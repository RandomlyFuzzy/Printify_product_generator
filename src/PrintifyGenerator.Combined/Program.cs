using System.Collections.Concurrent;
using System.Diagnostics;
using System.Threading.Channels;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

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
_ = Task.Run(async () =>
{
    while (!cancellationSource.IsCancellationRequested)
    {
        Console.Clear();

        Console.WriteLine("=== COMPLETED WORK ===");
        Console.WriteLine($"Total completed: {completedWork.Count}");
        Console.WriteLine();

        Console.WriteLine("=== ACTIVE WORK ===");

        if (activeWork.IsEmpty)
            Console.WriteLine("Idle");
        else
        {
            foreach (var kv in activeWork)
            {
                var w = kv.Key;
                var a = kv.Value;
                var dur = DateTime.UtcNow - a.StartedUtc;

                Console.WriteLine(
                    $"W{w}: {a.BundleId} | P{a.PhaseNumber} {a.PhaseName} | {dur:mm\\:ss}");
            }
        }

        await Task.Delay(500);
    }
});

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
    int completed = bundle.GetHighestCompletedPhase(pipeline);

    for (int i = completed; i < pipeline.Count; i++)
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

        Console.WriteLine($"[W{workerId}] ERROR {ex.Message}");
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