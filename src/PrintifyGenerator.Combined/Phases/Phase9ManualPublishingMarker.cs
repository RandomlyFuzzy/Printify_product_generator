public static partial class PhaseFactory
{
	private sealed class Phase9ManualPublishingMarker : PhaseGeneratorBase
	{
		public Phase9ManualPublishingMarker() : base(9, "Human Assessment + Publishing", "id/phase9.manual.txt") { }

		protected override bool IsCompleteCore(PhaseBundle bundle)
			=> File.Exists(bundle.ResolvePhaseFile(9, "manual.txt"));

		protected override bool CanRunCore(PhaseBundle bundle)
			=> File.Exists(bundle.ResolvePhaseFile(8, "txt"));

		protected override Task<PhaseExecutionResult> ExecuteCoreAsync(PhaseBundle bundle, CancellationToken cancellationToken)
		{
			var outputFile = bundle.ResolvePhaseFile(9, "manual.txt");
			File.WriteAllText(outputFile, $"Manual publish gate reached at {DateTime.UtcNow:O}");
			return Task.FromResult(PhaseExecutionResult.Done($"Created {Path.GetFileName(outputFile)}."));
		}
	}
}
