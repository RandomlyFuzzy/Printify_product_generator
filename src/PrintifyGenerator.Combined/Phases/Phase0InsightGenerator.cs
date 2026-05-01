using System.Text.Json;

public static partial class PhaseFactory
{
	private sealed class Phase0InsightGenerator : PhaseGeneratorBase
	{
		public Phase0InsightGenerator()
			: base(0, "Insight Built", "id/phase0.insight.json + id/phase0.blueprint-query.json => Starter intelligence") { }

		protected override bool IsCompleteCore(PhaseBundle bundle)
			=> File.Exists(GetPhase0InsightPath(bundle));

		protected override bool CanRunCore(PhaseBundle bundle) => true;

		protected override Task<PhaseExecutionResult> ExecuteCoreAsync(PhaseBundle bundle, CancellationToken cancellationToken)
		{
			var runtime = CombinedRuntime.Current;
			var intelligence = runtime.FeatureIntelligence;

			var definition = ReadProductDefinition(bundle) ?? intelligence.GenerateRandomDefinition(bundle.Id);
			var insight = intelligence.PredictSellability(definition);
			var blueprintName = !string.IsNullOrWhiteSpace(definition.BlueprintName)
				? definition.BlueprintName
				: (!string.IsNullOrWhiteSpace(definition.Title) ? definition.Title : bundle.Id.ToString());
			var starter = intelligence.QueryBlueprintStarter(blueprintName);

			var outputPath = GetPhase0InsightPath(bundle);
			var starterPath = GetPhase0BlueprintQueryPath(bundle);
			File.WriteAllText(outputPath, JsonSerializer.Serialize(insight, PrettyJson));
			File.WriteAllText(starterPath, JsonSerializer.Serialize(starter, PrettyJson));

			return Task.FromResult(PhaseExecutionResult.Done($"Created {Path.GetFileName(outputPath)} and {Path.GetFileName(starterPath)}."));
		}
	}
}
