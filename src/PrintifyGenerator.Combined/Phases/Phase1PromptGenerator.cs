using System.Text.Json;

public static partial class PhaseFactory
{
	private sealed class Phase1PromptGenerator : PhaseGeneratorBase
	{
		public Phase1PromptGenerator() : base(1, "Prompt Generated", "id/phase1.json => Prompt") { }

		protected override bool IsCompleteCore(PhaseBundle bundle)
			=> File.Exists(bundle.ResolvePhaseFile(1, "json", underscoreLegacy: true));

		protected override bool CanRunCore(PhaseBundle bundle) => true;

		protected override async Task<PhaseExecutionResult> ExecuteCoreAsync(PhaseBundle bundle, CancellationToken cancellationToken)
		{
			var runtime = CombinedRuntime.Current;
			var intelligence = runtime.FeatureIntelligence;

			var insight = ReadPhase0Insight(bundle);
			if (insight is null)
			{
				var definition = ReadProductDefinition(bundle) ?? intelligence.GenerateRandomDefinition(bundle.Id);
				insight = intelligence.PredictSellability(definition);
				File.WriteAllText(GetPhase0InsightPath(bundle), JsonSerializer.Serialize(insight, PrettyJson));
			}
			var starter = ReadPhase0BlueprintQuery(bundle);

			using var ollama = runtime.CreateOllamaClient();

			var basePrompt = PromptGenerationInstruction
				+ "\n\nProduct definition and sellability guidance (use this as the core commercial direction):\n"
				+ JsonSerializer.Serialize(insight, PrettyJson)
				+ "\n\nUse the recommended keywords/colors/materials naturally in the composition and style."
				+ (starter is not null && starter.PromptAnchors.Length > 0
					? "\n\nBlueprint starter anchors (prioritize these in visual concept and wording):\n- " + string.Join("\n- ", starter.PromptAnchors)
					: string.Empty)
				+ "\n\nIMPORTANT: Return only one valid JSON array item and no extra text.";

			Console.WriteLine("Phase 1 base prompt sent ");
			var prompts = await RetryOllamaJsonArrayAsync<Prompt>(
				prompt => ollama.GenerateStreamAsync(runtime.Settings.PromptModel, prompt, cancellationToken),
				basePrompt,
				cancellationToken);

			if (prompts is null || prompts.Count == 0)
			{
				return PhaseExecutionResult.Failed("Phase1 model did not return valid JSON array after retries.");
			}

			var prompt = prompts.FirstOrDefault();
			if (prompt is null || !prompt.isValid())
			{
				return PhaseExecutionResult.Failed("Phase1 prompt payload is invalid.");
			}

			prompt.width = Math.Clamp(prompt.width, 256, 1024);
			prompt.height = Math.Clamp(prompt.height, 256, 1024);
			prompt.steps = Math.Clamp(prompt.steps, 7, 12);
			prompt.cfg = Math.Clamp(prompt.cfg, 1.0f, 4.0f);

			var outputFile = bundle.ResolvePhaseFile(1, "json", underscoreLegacy: true);
			File.WriteAllText(outputFile, JsonSerializer.Serialize(prompt, PrettyJson));
			return PhaseExecutionResult.Done($"Created {Path.GetFileName(outputFile)} using Ollama with feature-driven sellability guidance.");
		}
	}
}
