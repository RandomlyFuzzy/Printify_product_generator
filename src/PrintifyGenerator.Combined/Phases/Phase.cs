public sealed record PhaseExecutionResult(bool Success, bool Changed, string Message)
{
    public static PhaseExecutionResult Skipped(string message) => new(true, false, message);
    public static PhaseExecutionResult Done(string message) => new(true, true, message);
    public static PhaseExecutionResult Failed(string message) => new(false, false, message);
}

public abstract class PhaseGeneratorBase : IPhaseGenerator
{
    public int PhaseNumber { get; }
    public string PhaseName { get; }
    public string OutputHint { get; }

    protected PhaseGeneratorBase(int phaseNumber, string phaseName, string outputHint)
    {
        PhaseNumber = phaseNumber;
        PhaseName = phaseName;
        OutputHint = outputHint;
    }

    public bool IsComplete(PhaseBundle bundle) => IsCompleteCore(bundle);

    public bool CanRun(PhaseBundle bundle)
    {
        if (IsComplete(bundle))
        {
            return false;
        }

        return CanRunCore(bundle);
    }

    public async Task<PhaseExecutionResult> RunAsync(PhaseBundle bundle, CancellationToken cancellationToken)
    {
        if (IsComplete(bundle))
        {
            return PhaseExecutionResult.Skipped($"phase{PhaseNumber} already complete.");
        }

        if (!CanRunCore(bundle))
        {
            return PhaseExecutionResult.Skipped($"phase{PhaseNumber} prerequisites missing.");
        }

        return await ExecuteCoreAsync(bundle, cancellationToken);
    }

    protected abstract bool IsCompleteCore(PhaseBundle bundle);
    protected abstract bool CanRunCore(PhaseBundle bundle);
    protected abstract Task<PhaseExecutionResult> ExecuteCoreAsync(PhaseBundle bundle, CancellationToken cancellationToken);
}