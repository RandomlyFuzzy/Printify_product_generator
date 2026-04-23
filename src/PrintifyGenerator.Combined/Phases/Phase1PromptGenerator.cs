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
			using var ollama = runtime.CreateOllamaClient();

			List<Prompt>? prompts = null;
			for (var attempt = 1; attempt <= 4; attempt++)
			{
				var promptText = attempt == 1
					? PromptGenerationInstruction
					: PromptGenerationInstruction + "\n\nIMPORTANT: Previous response was not valid JSON. Return only a valid JSON array and nothing else.";

				var response = new System.Text.StringBuilder();
				await foreach (var token in ollama.GenerateStreamAsync(runtime.Settings.PromptModel, promptText, cancellationToken))
				{
					response.Append(token);
				}

				if (!TryExtractJsonArray(response.ToString(), out var jsonArray))
				{
					continue;
				}

				try
				{
					prompts = JsonSerializer.Deserialize<List<Prompt>>(jsonArray, PrettyJson);
					if (prompts?.Count > 0)
					{
						break;
					}
				}
				catch
				{
					// Retry with stricter JSON instruction.
				}
			}

			if (prompts is null || prompts.Count == 0)
			{
				return PhaseExecutionResult.Failed("Phase1 model did not return valid JSON array after retries.");
			}

			var prompt = prompts?.FirstOrDefault();
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
			return PhaseExecutionResult.Done($"Created {Path.GetFileName(outputFile)} using Ollama.");
		}
	}
}
