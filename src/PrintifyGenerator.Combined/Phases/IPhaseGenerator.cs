public interface IPhaseGenerator
{
	int PhaseNumber { get; }
	string PhaseName { get; }
	string OutputHint { get; }

	bool IsComplete(PhaseBundle bundle);
	bool CanRun(PhaseBundle bundle);
	Task<PhaseExecutionResult> RunAsync(PhaseBundle bundle, CancellationToken cancellationToken);
}
