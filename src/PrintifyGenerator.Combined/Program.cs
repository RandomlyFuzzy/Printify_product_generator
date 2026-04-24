using System.Diagnostics;
using System.Collections.Concurrent;

var options = RunnerOptions.Parse(args);
using var cts = new CancellationTokenSource();

Console.CancelKeyPress += (_, e) =>
{
    e.Cancel = true;
    Console.WriteLine("Stopping...");
    cts.Cancel();
};

var pipeline = PhaseFactory.CreatePipeline();
var bundles = DiscoverBundles(options.CheckingRoot).ToList();

Console.WriteLine($"Pipeline phases: {pipeline.Count}");
Console.WriteLine($"Bundles: {bundles.Count}");

int maxParallelBundles = Environment.ProcessorCount; // tune this

var active = new ConcurrentDictionary<Guid, ActiveWorkItem>();
var completed = new ConcurrentQueue<CompletedWorkItem>();

// -------------------------
// EXECUTION
// -------------------------
await Parallel.ForEachAsync(
    bundles,
    new ParallelOptions
    {
        MaxDegreeOfParallelism = maxParallelBundles,
        CancellationToken = cts.Token
    },
    async (bundle, ct) =>
    {
        var bundleId = bundle.Id;

        try
        {
            var completedPhase = bundle.GetHighestCompletedPhase(pipeline);

            for (int i = completedPhase; i < pipeline.Count; i++)
            {
                ct.ThrowIfCancellationRequested();

                var phase = pipeline[i];

                if (!phase.CanRun(bundle))
                    continue;

                var started = DateTime.UtcNow;

                active[bundleId] = new ActiveWorkItem(
                    bundleId,
                    phase.PhaseNumber,
                    phase.PhaseName,
                    started);

                Console.WriteLine(
                    $"[{bundleId}] START P{phase.PhaseNumber} {phase.PhaseName}");

                var sw = Stopwatch.StartNew();

                try
                {
                    var result = await phase.RunAsync(bundle, ct);
                    sw.Stop();

                    var status = result.Changed ? "OK" : "SKIP";

                    completed.Enqueue(new CompletedWorkItem(
                        bundleId,
                        phase.PhaseNumber,
                        phase.PhaseName,
                        status,
                        sw.Elapsed,
                        DateTime.UtcNow));

                    Console.WriteLine(
                        $"[{bundleId}] {status} P{phase.PhaseNumber} | {sw.Elapsed:mm\\:ss}");

                    // optional: stop pipeline early if no change
                    if (!result.Changed)
                        continue;
                }
                catch (OperationCanceledException)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    sw.Stop();

                    completed.Enqueue(new CompletedWorkItem(
                        bundleId,
                        phase.PhaseNumber,
                        phase.PhaseName,
                        "ERR",
                        sw.Elapsed,
                        DateTime.UtcNow));

                    Console.WriteLine($"[{bundleId}] ERROR: {ex.Message} {ex.StackTrace.ToString()}");
                }
                finally
                {
                    active.TryRemove(bundleId, out _);
                }
            }

            Console.WriteLine($"[{bundleId}] ✅ COMPLETED ALL PHASES");
        }
        catch (OperationCanceledException)
        {
            Console.WriteLine($"[{bundleId}] ⛔ CANCELLED");
        }
    });

// -------------------------
// DONE
// -------------------------
Console.WriteLine();
Console.WriteLine("All work finished (or cancelled).");


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
// RECORDS
// =======================================================
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