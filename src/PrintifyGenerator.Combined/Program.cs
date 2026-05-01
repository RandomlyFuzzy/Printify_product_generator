using System.Collections.Concurrent;
using System.Diagnostics;
using System.Threading.Channels;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

if (TryRunBlueprintQuery(args))
{
    return;
}

var options = RunnerOptions.Parse(args);
var cancellationSource = new CancellationTokenSource();

int workerCount = Environment.ProcessorCount;

// =======================================================
// CHANNEL (single unified queue)
// =======================================================
var bundleChannel = Channel.CreateUnbounded<PhaseBundle>();

var seenBundles = new ConcurrentDictionary<Guid, byte>();

// =======================================================
// PIPELINE
// =======================================================
var pipeline = PhaseFactory.CreatePipeline();

// =======================================================
// STATE
// =======================================================
var activeWork = new ConcurrentDictionary<int, ActiveWorkItem>();
var completedWork = new ConcurrentQueue<CompletedWorkItem>();

int activeCount = 0;

// =======================================================
// INITIAL DISCOVERY
// =======================================================
var initialBundles = DiscoverBundles(options.CheckingRoot).ToList();

Console.WriteLine($"Pipeline phases: {pipeline.Count}");
Console.WriteLine($"Initial bundles: {initialBundles.Count}");

foreach (var bundle in initialBundles)
{
    if (seenBundles.TryAdd(bundle.Id, 0))
        bundleChannel.Writer.TryWrite(bundle);
}

// =======================================================
// CANCEL
// =======================================================
Console.CancelKeyPress += (_, e) =>
{
    e.Cancel = true;
    cancellationSource.Cancel();
    bundleChannel.Writer.TryComplete();
};

// =======================================================
// UI LOOP
// =======================================================
// _ = Task.Run(async () =>
// {
//     while (!cancellationSource.IsCancellationRequested)
//     {
//         Console.Clear();

//         Console.WriteLine("=== COMPLETED WORK ===");
//         Console.WriteLine($"Total completed: {completedWork.Count} Left: {bundleChannel.Reader.Count}");
//         Console.WriteLine();

//         Console.WriteLine("=== ACTIVE WORK ===");

//         if (activeWork.IsEmpty)
//             Console.WriteLine("Idle");
//         else
//         {
//             foreach (var kv in activeWork)
//             {
//                 var w = kv.Key;
//                 var a = kv.Value;
//                 var dur = DateTime.UtcNow - a.StartedUtc;

//                 Console.WriteLine(
//                     $"W{w}: {a.BundleId} | P{a.PhaseNumber} {a.PhaseName} | {dur:mm\\:ss}");
//             }
//         }

//         await Task.Delay(500);
//     }
// });

// =======================================================
// EXTERNAL PRODUCER (filesystem)
// =======================================================
_ = Task.Run(async () =>
{
    while (!cancellationSource.IsCancellationRequested)
    {
        try
        {
            var discovered = DiscoverBundles(options.CheckingRoot);

            foreach (var bundle in discovered)
            {
                if (seenBundles.TryAdd(bundle.Id, 0))
                {
                    await bundleChannel.Writer.WriteAsync(bundle, cancellationSource.Token);
                    Console.WriteLine($"[EXT] queued {bundle.Id}");
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[EXT ERROR] {ex.Message}");
        }

        await Task.Delay(2000);
    }
});

// =======================================================
// INTERNAL PRODUCER (idle generator)
// =======================================================
_ = Task.Run(async () =>
{
    int counter = 0;

    while (!cancellationSource.IsCancellationRequested)
    {
        try
        {
            bool idle =
                bundleChannel.Reader.Count <= workerCount &&
                Volatile.Read(ref activeCount) <= workerCount;

            if (idle)
            {
                var bundle = CreateInternalBundle(counter++);

                if (seenBundles.TryAdd(bundle.Id, 0))
                {
                    await bundleChannel.Writer.WriteAsync(bundle, cancellationSource.Token);
                    Console.WriteLine($"[INT] generated {bundle.Id}");
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[INT ERROR] {ex.Message}");
        }

        await Task.Delay(10);
    }
});

// =======================================================
// WORKERS
// =======================================================
var workers = Enumerable.Range(0, workerCount)
    .Select(i => WorkerLoop(i))
    .ToArray();

await Task.WhenAll(workers);

// =======================================================
// WORKER LOOP
// =======================================================
async Task WorkerLoop(int workerId)
{
    await foreach (var bundle in bundleChannel.Reader.ReadAllAsync(cancellationSource.Token))
    {
        Interlocked.Increment(ref activeCount);

        try
        {
            await RunBundle(workerId, bundle);
        }
        finally
        {
            Interlocked.Decrement(ref activeCount);
        }
    }
}

// =======================================================
// RUN BUNDLE
// =======================================================
async Task RunBundle(int workerId, PhaseBundle bundle)
{
    for (int i = 0; i < pipeline.Count; i++)
    {
        if (cancellationSource.IsCancellationRequested)
            return;

        var phase = pipeline[i];

        if (!phase.CanRun(bundle))
            continue;

        var item = new WorkItem(bundle, phase);
        await Execute(item, workerId);
    }
}

// =======================================================
// EXECUTE
// =======================================================
async Task Execute(WorkItem item, int workerId)
{
    var started = DateTime.UtcNow;

    activeWork[workerId] = new ActiveWorkItem(
        item.Bundle.Id,
        item.Phase.PhaseNumber,
        item.Phase.PhaseName,
        started);

    var sw = Stopwatch.StartNew();

    try
    {
        var result = await item.Phase.RunAsync(item.Bundle, cancellationSource.Token);
        sw.Stop();

        activeWork.TryRemove(workerId, out _);

        completedWork.Enqueue(new CompletedWorkItem(
            item.Bundle.Id,
            item.Phase.PhaseNumber,
            item.Phase.PhaseName,
            result.Changed ? "OK" : "SKIP",
            sw.Elapsed,
            DateTime.UtcNow));

        Console.WriteLine($"[W{workerId}] P{item.Phase.PhaseNumber} {(result.Changed ? "OK" : "SKIP")} {item.Bundle.Id}");
    }
    catch (Exception ex)
    {
        activeWork.TryRemove(workerId, out _);

        completedWork.Enqueue(new CompletedWorkItem(
            item.Bundle.Id,
            item.Phase.PhaseNumber,
            item.Phase.PhaseName,
            "ERR",
            sw.Elapsed,
            DateTime.UtcNow));
        var pagelinenumer = "";
        //get ther first none system stack frame
        var st = new StackTrace(ex, true);
        var frame = st.GetFrames()?.FirstOrDefault(f =>
        {
            var fileName = f.GetFileName();
            return fileName is not null && !fileName.Contains("System", StringComparison.Ordinal);
        });
        if (frame != null)
        {
            var frameFileName = frame.GetFileName() ?? string.Empty;
            pagelinenumer = $"{Path.GetFileName(frameFileName)}:{frame.GetFileLineNumber()}";
        }
        var extype = ex.GetType().Name;
        Console.WriteLine($"[W{workerId}] ERROR {ex.Message} (Line: {pagelinenumer}) (Type: {extype}) {item.Bundle.Id}");
    }
}

// =======================================================
// INTERNAL BUNDLE GENERATOR
// =======================================================
static PhaseBundle CreateInternalBundle(int i)
{
    return PhaseBundle.CreateSynthetic(Guid.NewGuid());
}

// =======================================================
// DISCOVERY
// =======================================================
static IEnumerable<PhaseBundle> DiscoverBundles(string root)
{
    if (!Directory.Exists(root))
        yield break;

    foreach (var dir in Directory.EnumerateDirectories(root, "*", SearchOption.AllDirectories))
    {
        if (PhaseBundle.TryCreate(dir, out var bundle) && bundle is not null)
            yield return bundle;
    }
}

bool TryRunBlueprintQuery(string[] cliArgs)
{
    var blueprintName = GetArgValue(cliArgs, "--query-blueprint");
    if (string.IsNullOrWhiteSpace(blueprintName))
    {
        return false;
    }

    var seoCountRaw = GetArgValue(cliArgs, "--seo-keyword-count");
    var seoKeywordCount = int.TryParse(seoCountRaw, out var parsedSeoCount)
        ? Math.Clamp(parsedSeoCount, 6, 24)
        : 12;

    var repositoryRoot = ResolveRepositoryRootForQuery() ?? Directory.GetCurrentDirectory();
    var featuresDir = Path.Combine(repositoryRoot, "category_features");
    var historyPath = Path.Combine(repositoryRoot, "src", "data", "staging", "shop-market-history.json");

    if (!Directory.Exists(featuresDir))
    {
        Console.Error.WriteLine($"category_features directory not found: {featuresDir}");
        return true;
    }

    var intelligence = CategoryFeatureIntelligence.Load(featuresDir, historyPath);
    var response = intelligence.QueryBlueprintStarter(blueprintName, seoKeywordCount);

    var outputPath = GetArgValue(cliArgs, "--query-output");
    var json = JsonSerializer.Serialize(response, new JsonSerializerOptions { WriteIndented = true });
    if (!string.IsNullOrWhiteSpace(outputPath))
    {
        var fullOutputPath = Path.GetFullPath(outputPath);
        var outputDirectory = Path.GetDirectoryName(fullOutputPath);
        if (!string.IsNullOrWhiteSpace(outputDirectory))
        {
            Directory.CreateDirectory(outputDirectory);
        }

        File.WriteAllText(fullOutputPath, json);
        Console.WriteLine($"Blueprint query insight saved to: {fullOutputPath}");
    }
    else
    {
        Console.WriteLine(json);
    }

    return true;
}

static string? GetArgValue(string[] cliArgs, string key)
{
    for (var i = 0; i < cliArgs.Length; i++)
    {
        var arg = cliArgs[i];
        if (string.Equals(arg, key, StringComparison.OrdinalIgnoreCase) && i + 1 < cliArgs.Length)
        {
            return cliArgs[i + 1];
        }

        var prefix = key + "=";
        if (arg.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
        {
            return arg.Substring(prefix.Length);
        }
    }

    return null;
}

static string? ResolveRepositoryRootForQuery()
{
    var probeRoots = new[]
    {
        Directory.GetCurrentDirectory(),
        AppContext.BaseDirectory,
    }
    .Where(path => !string.IsNullOrWhiteSpace(path))
    .Distinct(StringComparer.OrdinalIgnoreCase);

    foreach (var probeRoot in probeRoots)
    {
        var current = new DirectoryInfo(Path.GetFullPath(probeRoot));
        while (current is not null)
        {
            if (File.Exists(Path.Combine(current.FullName, "PrintifyGenerator.sln")))
            {
                return current.FullName;
            }

            current = current.Parent;
        }
    }

    return null;
}

// =======================================================
// OPTIONS
// =======================================================
sealed record RunnerOptions(string CheckingRoot, int MaxSteps, bool AllowSeeding)
{
    public static RunnerOptions Parse(string[] args)
    {
        var root = "./src/data/Checking";
        var maxSteps = int.MaxValue;
        var seed = false;

        for (int i = 0; i < args.Length; i++)
        {
            switch (args[i])
            {
                case "--root" when i + 1 < args.Length:
                    root = args[++i];
                    break;

                case "--max-steps" when i + 1 < args.Length && int.TryParse(args[i + 1], out var v):
                    maxSteps = Math.Max(1, v);
                    i++;
                    break;

                case "--seed":
                    seed = true;
                    break;
            }
        }

        return new RunnerOptions(Path.GetFullPath(root), maxSteps, seed);
    }
}

// =======================================================
// MODELS
// =======================================================
sealed record WorkItem(PhaseBundle Bundle, IPhaseGenerator Phase);

sealed record ActiveWorkItem(
    Guid BundleId,
    int PhaseNumber,
    string PhaseName,
    DateTime StartedUtc);

sealed record CompletedWorkItem(
    Guid BundleId,
    int PhaseNumber,
    string PhaseName,
    string Status,
    TimeSpan Duration,
    DateTime FinishedUtc);