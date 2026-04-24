using System.Collections.Concurrent;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using System;

var options = RunnerOptions.Parse(args);
var cancellationSource = new CancellationTokenSource();

// 🔥 declare worker count FIRST
int workerCount = Environment.ProcessorCount * 2;

// 🔥 declare signal BEFORE it's used anywhere
var signal = new SemaphoreSlim(0);

Console.CancelKeyPress += (_, e) =>
{
    e.Cancel = true;

    cancellationSource.Cancel();

    // wake all workers immediately
    signal.Release(workerCount);

    Environment.Exit(0);
};

var pipeline = PhaseFactory.CreatePipeline();
var bundles = DiscoverBundles(options.CheckingRoot).ToList();

Console.WriteLine($"Pipeline phases: {pipeline.Count}");
Console.WriteLine($"Bundles: {bundles.Count}");

var workQueue = new ConcurrentQueue<WorkItem>();
var remaining = new ConcurrentDictionary<Guid, int>();

var activeWork = new ConcurrentDictionary<int, ActiveWorkItem>();
var completedWork = new ConcurrentQueue<CompletedWorkItem>();

// -------------------------
// Init
// -------------------------
var activeBundles = new ConcurrentDictionary<Guid, byte>();

foreach (var bundle in bundles)
{
    activeBundles[bundle.Id] = 0;

    var completed = bundle.GetHighestCompletedPhase(pipeline);
    remaining[bundle.Id] = pipeline.Count - completed;

    EnqueueNext(bundle, completed);
}

// -------------------------
// UI LOOP
// -------------------------
_ = Task.Run(async () =>
{
    while (!cancellationSource.IsCancellationRequested)
    {
        Console.Clear();

        var snapshot = completedWork.ToArray();

        Console.WriteLine("=== COMPLETED WORK ===");
        Console.WriteLine();
        Console.WriteLine($"Total completed: {snapshot.Length}");
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

        Console.WriteLine();

        await Task.Delay(500);
    }
});

// -------------------------
// Worker pool
// -------------------------
var workers = new Task[workerCount];

for (int i = 0; i < workerCount; i++)
{
    int id = i;
    workers[i] = WorkerLoop(id);
}

await Task.WhenAll(workers);

// =======================================================
// WORKER LOOP (NO POLLING)
// =======================================================
async Task WorkerLoop(int workerId)
{
    while (true)
    {
        if (cancellationSource.IsCancellationRequested)
            return;

        try
        {
            await signal.WaitAsync(cancellationSource.Token);
        }
        catch (OperationCanceledException)
        {
            return;
        }

        if (cancellationSource.IsCancellationRequested)
            return;

        if (!workQueue.TryDequeue(out var item))
            continue;

        await Execute(item, workerId);
    }
}

// =======================================================
// EXECUTION
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
        $"[W{workerId}] START {item.Bundle.Id} P{item.Phase.PhaseNumber}");

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
            $"[W{workerId}] {status} {item.Bundle.Id} P{item.Phase.PhaseNumber} " +
            $"| {sw.Elapsed:mm\\:ss} | {result.Message}");

        // ✅ ALWAYS advance to next phase (core fix)
        EnqueueNext(item.Bundle, item.Phase.PhaseNumber);
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

        Console.WriteLine($"[W{workerId}] ERROR {ex.Message}");

        // ✅ still continue pipeline even on error
        EnqueueNext(item.Bundle, item.Phase.PhaseNumber);
    }
}

// =======================================================
// SCHEDULING (lock-free enqueue)
// =======================================================
void EnqueueNext(PhaseBundle bundle, int completedPhase)
{
    var nextPhaseNumber = completedPhase + 1;

    if (nextPhaseNumber > pipeline.Count)
    {
        activeBundles.TryRemove(bundle.Id, out _);
        return;
    }

    var phase = pipeline[nextPhaseNumber - 1];

    // ✅ skip non-runnable phases instead of killing bundle
    if (!phase.CanRun(bundle))
    {
        EnqueueNext(bundle, nextPhaseNumber);
        return;
    }

    workQueue.Enqueue(new WorkItem(bundle, phase));

    // wake one worker
    signal.Release();
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