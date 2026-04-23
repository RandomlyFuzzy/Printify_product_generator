using System.Globalization;
using System.Text.Json;

public static partial class PhaseFactory
{
	private sealed class Phase7PublishingDirectionGenerator : PhaseGeneratorBase
	{
		public Phase7PublishingDirectionGenerator() : base(7, "Publishing Direction", "id/phase7.txt => {\"pid\":\"...\",\"targets\":[{\"shopTitle\":\"Ebay\",\"shopId\":123,\"productId\":\"...\"}]}") { }

		protected override bool IsCompleteCore(PhaseBundle bundle)
			=> File.Exists(bundle.ResolvePhaseFile(7, "txt"));

		protected override bool CanRunCore(PhaseBundle bundle)
			=> Phase6Complete(bundle);

		protected override async Task<PhaseExecutionResult> ExecuteCoreAsync(PhaseBundle bundle, CancellationToken cancellationToken)
		{
			var pids = bundle.ReadPhase4ProductIds();
			if (pids.Count == 0)
			{
				var emptyOutput = bundle.ResolvePhaseFile(7, "txt");
				File.WriteAllLines(emptyOutput, Array.Empty<string>());
				return PhaseExecutionResult.Done("No phase4 products available for publishing direction.");
			}

			var runtime = CombinedRuntime.Current;
			var printify = runtime.GetPrintifyClient();
			var stagingShopId = await runtime.ResolveShopIdAsync("Staging");
			using var ollama = runtime.CreateOllamaClient();

			var outputFile = bundle.ResolvePhaseFile(7, "txt");
			var lines = new List<string>();
			var movedCount = 0;
			var transferFailures = 0;

			foreach (var pid in pids)
			{
				cancellationToken.ThrowIfCancellationRequested();

				var product = await printify.GetProductAsync(stagingShopId, pid);
				var suitability = ReadImageSuitability(bundle);
				var assessmentPath = GetPhase6Path(bundle, pid);
				var assessment = File.Exists(assessmentPath)
					? JsonSerializer.Deserialize<ProductAssessment>(File.ReadAllText(assessmentPath))
					: null;

				var metaPath = GetPhase5Path(bundle, pid);
				var meta = File.Exists(metaPath)
					? JsonSerializer.Deserialize<ProductMeta>(File.ReadAllText(metaPath))
					: null;

				var context = BuildPublishingContext(product, suitability, assessment, meta);
				var basePrompt = PublishDirectionPromptTemplate.Replace("{0}", context, StringComparison.Ordinal);
				List<string>? selectedStores = null;
				for (var attempt = 1; attempt <= 4; attempt++)
				{
					var prompt = attempt == 1
						? basePrompt
						: basePrompt + "\n\nIMPORTANT: Previous response was not valid JSON. Return only one valid JSON object and nothing else.";

					var response = await CollectStreamAsync(
						ollama.GenerateStreamAsync(runtime.Settings.SuitabilityModel, prompt, cancellationToken),
						cancellationToken);

					selectedStores = ParseStores(response);
					if (selectedStores.Count > 0)
					{
						break;
					}
				}

				selectedStores ??= new List<string>();
				if (selectedStores.Count == 0)
				{
					selectedStores = new List<string> { "Etsy" };
				}

				var targets = await runtime.ResolveShopsByNamesAsync(selectedStores);
				var targetEntries = new List<object>();
				foreach (var target in targets)
				{
					try
					{
						var transfer = await printify.TransferProductAsync(
							sourceShopId: stagingShopId,
							sourceProductId: pid,
							targetShopId: target.Id,
							deleteSourceProduct: false,
							publishTargetProduct: false);
						movedCount++;
						targetEntries.Add(new
						{
							shopTitle = target.Title,
							shopId = target.Id,
							productId = transfer.TargetProductId,
						});
					}
					catch (Exception ex)
					{
						transferFailures++;
						targetEntries.Add(new
						{
							shopTitle = target.Title,
							shopId = target.Id,
							productId = (string?)null,
						});
						Console.Error.WriteLine($"[phase7] Failed to transfer product {pid} to shop '{target.Title}' ({target.Id}): {ex.Message}");
					}
				}

				if (targetEntries.Count == 0)
				{
					targetEntries.AddRange(selectedStores
						.Where(store => !string.IsNullOrWhiteSpace(store))
						.Select(store => new
						{
							shopTitle = store,
							shopId = (int?)null,
							productId = (string?)null,
						}));
				}

				lines.Add(JsonSerializer.Serialize(new
				{
					pid,
					targets = targetEntries,
				}));
			}

			File.WriteAllLines(outputFile, lines);
			return PhaseExecutionResult.Done($"Created {Path.GetFileName(outputFile)} and transferred {movedCount} draft copy/copies with {transferFailures} transfer failure(s).");
		}

		private static string BuildPublishingContext(
			Product product,
			ImageSuitability? suitability,
			ProductAssessment? assessment,
			ProductMeta? meta)
		{
			var title = string.IsNullOrWhiteSpace(meta?.Title) ? product.Title : meta!.Title;
			var tags = (meta?.Tags?.Length ?? 0) > 0 ? string.Join(", ", meta!.Tags) : string.Join(", ", product.Tags);
			var fit = assessment?.FitForPrintify == true ? "yes" : "no";
			var continueFlag = assessment?.shouldContinue == true ? "yes" : "no";
			var score = suitability?.OverallScore().ToString("0.00", CultureInfo.InvariantCulture) ?? "unknown";

			return $"Title: {title}\nTags: {tags}\nImageScore: {score}\nFitForPrintify: {fit}\nShouldContinue: {continueFlag}";
		}

		private static List<string> ParseStores(string response)
		{
			if (!TryExtractJsonObject(response, out var jsonObject))
			{
				return new List<string>();
			}

			PublishDirectionDecision? decision;
			try
			{
				decision = JsonSerializer.Deserialize<PublishDirectionDecision>(jsonObject, PrettyJson);
			}
			catch
			{
				return new List<string>();
			}

			if (decision is null || decision.stores is null || decision.stores.Count == 0)
			{
				return new List<string>();
			}

			return decision.stores
				.Where(store => !string.IsNullOrWhiteSpace(store))
				.Select(store => store.Trim())
				.Where(store => store.Equals("Etsy", StringComparison.OrdinalIgnoreCase) || store.Equals("Ebay", StringComparison.OrdinalIgnoreCase))
				.Distinct(StringComparer.OrdinalIgnoreCase)
				.ToList();
		}
	}
}
