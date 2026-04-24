using System.Text.Json;

public static partial class PhaseFactory
{
	private sealed class Phase4ProductsGenerator : PhaseGeneratorBase
	{
		public Phase4ProductsGenerator() : base(4, "Products Generated", "id/phase4.txt => PID[]") { }

		protected override bool IsCompleteCore(PhaseBundle bundle)
			=> File.Exists(GetPhase4Path(bundle));

		protected override bool CanRunCore(PhaseBundle bundle)
			=> File.Exists(bundle.ResolvePhaseFile(3, "json", underscoreLegacy: true)) && bundle.FindImagePath() is not null;

		protected override async Task<PhaseExecutionResult> ExecuteCoreAsync(PhaseBundle bundle, CancellationToken cancellationToken)
		{
			var runtime = CombinedRuntime.Current;
			var suitabilityFile = bundle.ResolvePhaseFile(3, "json", underscoreLegacy: true);
			var suitability = JsonSerializer.Deserialize<ImageSuitability>(File.ReadAllText(suitabilityFile));
			var imagePath = bundle.FindImagePath();
			if (imagePath is null)
			{
				return PhaseExecutionResult.Failed("Phase4 requires phase2 image.");
			}

			var outputFile = GetPhase4Path(bundle);
			if (suitability is null || !suitability.IsSuitableForPrint())
			{
				File.WriteAllLines(outputFile, Array.Empty<string>());
				return PhaseExecutionResult.Done("Phase4 skipped product generation due to unsuitable image.");
			}

			using var ollama = runtime.CreateOllamaClient();
			var printify = runtime.GetPrintifyClient();
			var stagingShopId = await runtime.ResolveShopIdAsync("Staging");
			var generator = new MockupGenerator(
				printify: printify,
				ollama: ollama,
				shopId: stagingShopId,
				dataBasePath: runtime.DataRoot,
				visionModel: runtime.Settings.MockupVisionModel);

			var ids = new List<string>();
			await foreach (var result in generator.ProcessImageAsync(imagePath))
			{
				if (result.Success && !string.IsNullOrWhiteSpace(result.Draft?.ProductId))
				{
					ids.Add(result.Draft.ProductId);
				}
			}

			ids = ids.Distinct(StringComparer.OrdinalIgnoreCase).ToList();
			Console.WriteLine($"Phase4 generated {ids.Count} product(s) for bundle {bundle.Id}.");
			Console.WriteLine($"Products: {string.Join(", ", ids)} outputed to {(outputFile)}.");
			File.WriteAllLines(outputFile, ids);
			Environment.Exit(0);
			return PhaseExecutionResult.Done($"Created {Path.GetFileName(outputFile)} with {ids.Count} product(s) via PrintifyGenerator flow.");
		}
	}
}
