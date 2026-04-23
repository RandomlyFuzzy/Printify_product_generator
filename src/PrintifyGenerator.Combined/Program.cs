using System.Diagnostics;

var options = RunnerOptions.Parse(args);
var cancellationSource = new CancellationTokenSource();

Console.CancelKeyPress += (_, eventArgs) =>
{
    eventArgs.Cancel = true;
    cancellationSource.Cancel();
};

var pipeline = PhaseFactory.CreatePipeline();
var bundles = DiscoverBundles(options.CheckingRoot).ToList();

Console.WriteLine($"Pipeline phases: {pipeline.Count}");
Console.WriteLine($"Discovered bundles: {bundles.Count}");
Console.WriteLine("Priority: process latest phases first, then create earlier phases when needed.");

try
{
    var changes = await RunSchedulerAsync(pipeline, bundles, options, cancellationSource.Token);
    Console.WriteLine($"Done. Applied {changes} phase update(s).");
}
catch (OperationCanceledException)
{
    Console.WriteLine("Cancelled. Exiting cleanly.");
}

static async Task<int> RunSchedulerAsync(
    IReadOnlyList<IPhaseGenerator> pipeline,
    List<PhaseBundle> bundles,
    RunnerOptions options,
    CancellationToken cancellationToken)
{
    var updated = 0;
    var startTimeUtc = DateTime.UtcNow;
    var measuredSteps = 0;
    var measuredDuration = TimeSpan.Zero;

    for (var i = 0; i < options.MaxSteps; i++)
    {
        if (cancellationToken.IsCancellationRequested)
        {
            Console.WriteLine("[CANCEL] Cancellation requested. Stopping scheduler.");
            break;
        }

        var remainingBefore = CountRemainingPhaseUnits(pipeline, bundles);

        var next = PickNextWork(pipeline, bundles);
        if (next.bundle is null || next.phase is null)
        {
            if (options.AllowSeeding)
            {
                var seeded = SeedNewBundle(options.CheckingRoot);
                bundles.Add(seeded);
                Console.WriteLine($"[SEED] {seeded.Id} | bundles={bundles.Count} | remaining={CountRemainingPhaseUnits(pipeline, bundles)}");
                continue;
            }

            Console.WriteLine($"[IDLE] No runnable work. Remaining units={remainingBefore}.");
            break;
        }

        var remainingByPhase = BuildRemainingByPhaseMap(pipeline, bundles);
        var currentPhaseBacklog = remainingByPhase.TryGetValue(next.phase.PhaseNumber, out var backlog) ? backlog : 0;
        var eta = EstimateEta(startTimeUtc, measuredSteps, measuredDuration, remainingBefore);
        Console.WriteLine(
            $"[PROGRESS] step {i + 1}/{options.MaxSteps} | bundle={next.bundle.Id} | phase{next.phase.PhaseNumber} {next.phase.PhaseName} | " +
            $"phase-backlog={currentPhaseBacklog} | remaining={remainingBefore} | eta={eta}");

        var stepTimer = Stopwatch.StartNew();
        PhaseExecutionResult result;
        try
        {
            result = await next.phase.RunAsync(next.bundle, cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            Console.WriteLine("[CANCEL] Cancellation requested while running a phase. Stopping scheduler.");
            break;
        }
        stepTimer.Stop();
        measuredSteps++;
        measuredDuration += stepTimer.Elapsed;

        var status = result.Success ? (result.Changed ? "OK" : "SKIP") : "ERR";
        var remainingAfter = CountRemainingPhaseUnits(pipeline, bundles);
        var avgStepSeconds = measuredDuration.TotalSeconds / Math.Max(1, measuredSteps);
        Console.WriteLine(
            $"[{status}] {next.bundle.Id} phase{next.phase.PhaseNumber} {next.phase.PhaseName}: {result.Message} | " +
            $"elapsed={stepTimer.Elapsed:mm\\:ss} | avg={avgStepSeconds:F1}s/phase | remaining={remainingAfter}");
        if (result.Changed)
        {
            updated++;
        }
    }

    return updated;
}

static int CountRemainingPhaseUnits(IReadOnlyList<IPhaseGenerator> pipeline, IEnumerable<PhaseBundle> bundles)
{
    var remaining = 0;
    foreach (var bundle in bundles)
    {
        var completed = bundle.GetHighestCompletedPhase(pipeline);
        if (completed < pipeline.Count)
        {
            remaining += pipeline.Count - completed;
        }
    }

    return remaining;
}

static Dictionary<int, int> BuildRemainingByPhaseMap(IReadOnlyList<IPhaseGenerator> pipeline, IEnumerable<PhaseBundle> bundles)
{
    var map = Enumerable.Range(1, pipeline.Count).ToDictionary(phase => phase, _ => 0);
    foreach (var bundle in bundles)
    {
        var completed = bundle.GetHighestCompletedPhase(pipeline);
        for (var phase = completed + 1; phase <= pipeline.Count; phase++)
        {
            map[phase]++;
        }
    }

    return map;
}

static string EstimateEta(DateTime startTimeUtc, int measuredSteps, TimeSpan measuredDuration, int remainingBefore)
{
    if (measuredSteps <= 0 || remainingBefore <= 0)
    {
        return "n/a";
    }

    var avgSeconds = measuredDuration.TotalSeconds / measuredSteps;
    var etaUtc = DateTime.UtcNow.AddSeconds(avgSeconds * remainingBefore);
    var elapsed = DateTime.UtcNow - startTimeUtc;
    return $"{etaUtc:HH:mm:ss}Z (~{elapsed:hh\\:mm\\:ss} elapsed)";
}

static (PhaseBundle? bundle, IPhaseGenerator? phase) PickNextWork(
    IReadOnlyList<IPhaseGenerator> pipeline,
    IEnumerable<PhaseBundle> bundles)
{
    var candidates = bundles
        .Select(bundle =>
        {
            var completed = bundle.GetHighestCompletedPhase(pipeline);
            var nextPhase = completed + 1;
            if (nextPhase > pipeline.Count)
            {
                return (bundle: (PhaseBundle?)null, phase: (IPhaseGenerator?)null, completed: -1);
            }

            var phase = pipeline[nextPhase - 1];
            var canRun = phase.CanRun(bundle);
            return (bundle: canRun ? bundle : null, phase: canRun ? phase : null, completed);
        })
        .Where(item => item.bundle is not null && item.phase is not null)
        .OrderByDescending(item => item.completed)
        .ThenBy(item => item.bundle!.DirectoryPath, StringComparer.OrdinalIgnoreCase)
        .FirstOrDefault();

    return (candidates.bundle, candidates.phase);
}

static IEnumerable<PhaseBundle> DiscoverBundles(string checkingRoot)
{
    if (!Directory.Exists(checkingRoot))
    {
        yield break;
    }

    foreach (var directory in Directory.EnumerateDirectories(checkingRoot, "*", SearchOption.AllDirectories))
    {
        if (PhaseBundle.TryCreate(directory, out var bundle) && bundle is not null)
        {
            yield return bundle;
        }
    }
}

static PhaseBundle SeedNewBundle(string checkingRoot)
{
    var id = Guid.NewGuid();
    var path = Path.Combine(checkingRoot, DateTime.UtcNow.ToString("yyyy-MM"), DateTime.UtcNow.ToString("dd"), id.ToString());
    Directory.CreateDirectory(path);
    return new PhaseBundle(id, path);
}

sealed record RunnerOptions(string CheckingRoot, int MaxSteps, bool AllowSeeding)
{
    public static RunnerOptions Parse(string[] args)
    {
        var checkingRoot = "./src/data/Checking";
        var maxSteps = int.MaxValue;
        var allowSeeding = false;

        for (var i = 0; i < args.Length; i++)
        {
            switch (args[i])
            {
                case "--root" when i + 1 < args.Length:
                    checkingRoot = args[++i];
                    break;
                case "--max-steps" when i + 1 < args.Length && int.TryParse(args[i + 1], out var parsed):
                    maxSteps = Math.Max(1, parsed);
                    i++;
                    break;
                case "--seed":
                    allowSeeding = true;
                    break;
            }
        }

        return new RunnerOptions(
            CheckingRoot: Path.GetFullPath(checkingRoot),
            MaxSteps: maxSteps,
            AllowSeeding: allowSeeding);
    }
}