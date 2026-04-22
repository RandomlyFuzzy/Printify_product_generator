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

var changes = await RunSchedulerAsync(pipeline, bundles, options, cancellationSource.Token);
Console.WriteLine($"Done. Applied {changes} phase update(s).");

static async Task<int> RunSchedulerAsync(
    IReadOnlyList<IPhaseGenerator> pipeline,
    List<PhaseBundle> bundles,
    RunnerOptions options,
    CancellationToken cancellationToken)
{
    var updated = 0;
    for (var i = 0; i < options.MaxSteps; i++)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var next = PickNextWork(pipeline, bundles);
        if (next.bundle is null || next.phase is null)
        {
            if (options.AllowSeeding)
            {
                var seeded = SeedNewBundle(options.CheckingRoot);
                bundles.Add(seeded);
                Console.WriteLine($"Seeded new bundle {seeded.Id}.");
                continue;
            }

            break;
        }

        var result = await next.phase.RunAsync(next.bundle, cancellationToken);
        var status = result.Success ? (result.Changed ? "OK" : "SKIP") : "ERR";
        Console.WriteLine($"[{status}] {next.bundle.Id} phase{next.phase.PhaseNumber}: {result.Message}");
        if (result.Changed)
        {
            updated++;
        }
    }

    return updated;
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
        var maxSteps = 150;
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