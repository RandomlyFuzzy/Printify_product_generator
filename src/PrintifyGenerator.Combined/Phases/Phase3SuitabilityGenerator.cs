using System.Text.Json;

public static partial class PhaseFactory
{
	private sealed class Phase3SuitabilityGenerator : PhaseGeneratorBase
	{
		public Phase3SuitabilityGenerator() : base(3, "Image Assessed", "id/phase3.json => ImageSuitability") { }

		protected override bool IsCompleteCore(PhaseBundle bundle)
			=> File.Exists(bundle.ResolvePhaseFile(3, "json", underscoreLegacy: true));

		protected override bool CanRunCore(PhaseBundle bundle) => bundle.FindImagePath() is not null;

		protected override async Task<PhaseExecutionResult> ExecuteCoreAsync(PhaseBundle bundle, CancellationToken cancellationToken)
		{
			var imagePath = bundle.FindImagePath();
			if (imagePath is null)
			{
				return PhaseExecutionResult.Failed("Missing phase2 image.");
			}

			var runtime = CombinedRuntime.Current;
			using var ollama = runtime.CreateOllamaClient();

			var suitability = await RetryOllamaJsonObjectAsync<ImageSuitability>(
				prompt => ollama.GenerateWithImageStreamAsync(runtime.Settings.SuitabilityModel, prompt, imagePath, cancellationToken),
				ImageSuitabilityPrompt,
				cancellationToken,
				isValid: s => s.isValid());

			if (suitability is null || !suitability.isValid())
			{
				return PhaseExecutionResult.Failed("Phase3 suitability payload is invalid after retries.");
			}

			suitability.imageURL = new Uri(Path.GetFullPath(imagePath)).AbsoluteUri;
			var outputFile = bundle.ResolvePhaseFile(3, "json", underscoreLegacy: true);
			File.WriteAllText(outputFile, JsonSerializer.Serialize(suitability, PrettyJson));
			return PhaseExecutionResult.Done($"Created {Path.GetFileName(outputFile)} using Ollama.");
		}
	}
}
