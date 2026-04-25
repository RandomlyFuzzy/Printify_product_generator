using System.Collections.Concurrent;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

var options = RunnerOptions.Parse(args);
var cancellationSource = new CancellationTokenSource();

int workerCount =  Environment.ProcessorCount;

// 🔥 bundle queue (not work items)
var bundleQueue = new Queue<PhaseBundle>();
var queueLock = new object();

Console.CancelKeyPress += (_, e) =>
{
    e.Cancel = true;
    cancellationSource.Cancel();

    lock (queueLock)
    {
        Monitor.PulseAll(queueLock);
    }

    Environment.Exit(0);
};

var pipeline = PhaseFactory.CreatePipeline();
var bundles = DiscoverBundles(options.CheckingRoot).ToList();

Console.WriteLine($"Pipeline phases: {pipeline.Count}");
Console.WriteLine($"Bundles: {bundles.Count}");

var activeWork = new ConcurrentDictionary<int, ActiveWorkItem>();
var completedWork = new ConcurrentQueue<CompletedWorkItem>();
var activeBundles = new ConcurrentDictionary<Guid, byte>();

// -------------------------
// INIT (enqueue bundles)
// -------------------------
foreach (var bundle in bundles)
{
    bundleQueue.Enqueue(bundle);
}

// // -------------------------
// // UI LOOP
// // -------------------------
// _ = Task.Run(async () =>
// {
//     while (!cancellationSource.IsCancellationRequested)
//     {
//         Console.Clear();

//         var snapshot = completedWork.ToArray();

//         Console.WriteLine("=== COMPLETED WORK ===");
//         Console.WriteLine();
//         Console.WriteLine($"Total completed: {snapshot.Length}");
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

//         Console.WriteLine();
//         await Task.Delay(500);
//     }
// });

// -------------------------
// WORKERS
// -------------------------
var workers = new Task[workerCount];

for (int i = 0; i < workerCount; i++)
{
    int id = i;
    workers[i] = WorkerLoop(id);
}

await Task.WhenAll(workers);

// =======================================================
// WORKER LOOP (pulls bundles)
// =======================================================
async Task WorkerLoop(int workerId)
{
    while (true)
    {
        if (cancellationSource.IsCancellationRequested)
            return;

        PhaseBundle bundle;

        lock (queueLock)
        {
            while (bundleQueue.Count == 0)
            {
                Monitor.Wait(queueLock);

                if (cancellationSource.IsCancellationRequested)
                    return;
            }

            bundle = bundleQueue.Dequeue();
        }

        await RunBundle(workerId, bundle);
    }
}

// =======================================================
// RUN FULL BUNDLE (key change)
// =======================================================
async Task RunBundle(int workerId, PhaseBundle bundle)
{
    activeBundles[bundle.Id] = 0;

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

    activeBundles.TryRemove(bundle.Id, out _);
}

// =======================================================
// EXECUTION (unchanged except NO enqueue)
// =======================================================
async Task Execute(WorkItem item, int workerId)
{
    var started = DateTime.UtcNow;

    activeWork[workerId] = new ActiveWorkItem(
        item.Bundle.Id,
        item.Phase.PhaseNumber,
        item.Phase.PhaseName,
        started);

    Console.WriteLine(
        $"[W{workerId}] [{item.Phase.PhaseNumber}] START {item.Bundle.Id} P{item.Phase.PhaseNumber}");

    var sw = Stopwatch.StartNew();

    try
    {
        var result = await item.Phase.RunAsync(item.Bundle, cancellationSource.Token);
        sw.Stop();

        activeWork.TryRemove(workerId, out _);

        var status = result.Changed ? "OK" : "SKIP";

        completedWork.Enqueue(new CompletedWorkItem(
            item.Bundle.Id,
            item.Phase.PhaseNumber,
            item.Phase.PhaseName,
            status,
            sw.Elapsed,
            DateTime.UtcNow));

        Console.WriteLine(
            $"[W{workerId}] [{item.Phase.PhaseNumber}] {status} {item.Bundle.Id} P{item.Phase.PhaseNumber} " +
            $"| {sw.Elapsed:mm\\:ss} | {result.Message}");
    }
    catch (OperationCanceledException)
    {
        activeWork.TryRemove(workerId, out _);
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
        string fileandline = string.Empty;
        if (ex.StackTrace is not null)        {
            var lines = ex.StackTrace.Split(Environment.NewLine);
            if (lines.Length > 0)            {
                var firstLine = lines[0];
                var idx = firstLine.LastIndexOf(" in ");
                if (idx != -1)                {
                    fileandline = firstLine.Substring(idx + 4);
                }
            }
        }
        Console.WriteLine($"[W{workerId}] [{item.Phase.PhaseNumber}] ERROR {ex.Message} {fileandline}");
    }
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