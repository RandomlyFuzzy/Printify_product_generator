using System.Globalization;
using System.Text.Json;

public static partial class PhaseFactory
{
	private sealed class Phase7PublishingDirectionGenerator : PhaseGeneratorBase
	{
		public Phase7PublishingDirectionGenerator() : base(7, "Publishing Direction", "id/phase7.txt => PID,Store,NPID") { }

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

				Product product;
				try
				{
					product = await printify.GetProductAsync(stagingShopId, pid);
				}
				catch (PrintifyApiException ex) when (ex.StatusCode == 404)
				{
					// Product no longer exists in staging — it was likely transferred in a prior run
					// that crashed before writing the output file. Do not count as a failure.
					Console.Error.WriteLine($"[phase7] Product {pid} not found in staging shop {stagingShopId} (404) - already gone, skipping.");
					continue;
				}

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
				List<PublishDirectionDecision>? decisions = null;
				for (var attempt = 1; attempt <= 4; attempt++)
				{
					var prompt = attempt == 1
						? basePrompt
						: basePrompt + "\n\nIMPORTANT: Previous response was not valid JSON. Return only one valid JSON object and nothing else.";

					var response = await CollectStreamAsync(
						ollama.GenerateStreamAsync(runtime.Settings.SuitabilityModel, prompt, cancellationToken),
						cancellationToken, endOn: "]");

					decisions = ParseDecisions(response);
					if (decisions.Count > 0)
					{
						break;
					}
				}

				decisions ??= new List<PublishDirectionDecision>();
				var selectedStores = decisions.Select(d => d.store).ToList();

				var targets = await runtime.ResolveShopsByNamesAsync(selectedStores);
				var unresolvedStores = selectedStores
					.Where(store => targets.All(target =>
						!string.Equals(target.Title, store, StringComparison.OrdinalIgnoreCase)
						&& !target.Title.Contains(store, StringComparison.OrdinalIgnoreCase)
						&& !store.Contains(target.Title, StringComparison.OrdinalIgnoreCase)))
					.Distinct(StringComparer.OrdinalIgnoreCase)
					.ToList();

				if (unresolvedStores.Count > 0)
				{
					transferFailures += unresolvedStores.Count;
					Console.Error.WriteLine($"[phase7] Could not resolve target shop(s) for product {pid}: {string.Join(", ", unresolvedStores)}");
					continue;
				}
				var sids = targets.Select(t => t.Id).Distinct().ToList();

				var targetLines = new List<string>();
				foreach (var targetId in sids)
				{
					var targetTitle = targets.Where(t => t.Id == targetId).Select(t => t.Title).FirstOrDefault() ?? string.Empty;
					try
					{
						var transfer = await printify.TransferProductAsync(
							sourceShopId: stagingShopId,
							sourceProductId: pid,
							targetShopId: targetId,
							deleteSourceProduct: false,
							publishTargetProduct: false);
						movedCount++;
						targetLines.Add($"{pid},{targetTitle},{transfer.TargetProductId}");

						var matchingDecision = decisions.FirstOrDefault(d =>
							d.store.Equals(targetTitle, StringComparison.OrdinalIgnoreCase)
							|| targetTitle.Contains(d.store, StringComparison.OrdinalIgnoreCase)
							|| d.store.Contains(targetTitle, StringComparison.OrdinalIgnoreCase));
						if (matchingDecision is not null)
						{
							var reasonPath = GetPhase7ReasonPath(bundle, transfer.TargetProductId, matchingDecision.store);
							var reasonRecord = new Phase7ReasonRecord { store = matchingDecision.store, reason = matchingDecision.reason, sourcePid = pid };
							File.WriteAllText(reasonPath, JsonSerializer.Serialize(reasonRecord, PrettyJson));
						}
					}
					catch (Exception ex)
					{
						transferFailures++;
						Console.Error.WriteLine($"[phase7] Failed to transfer product {pid} to shop '{targetTitle}' ({targetId}): {ex.Message}");
					}
				}

				if (targetLines.Count == 0)
				{
					transferFailures++;
					Console.Error.WriteLine($"[phase7] No transferable target product was created for source product {pid}.");
					continue;
				}
				lines.AddRange(targetLines);
			}

			Console.WriteLine($"Phase7 generated publishing directions for {movedCount} product(s) for bundle {bundle.Id}.");
			Console.WriteLine($"Output written to {Path.GetFileName(outputFile)} with lines: {string.Join("; ", lines)}");
			File.WriteAllLines(outputFile, lines);

			if (transferFailures > 0)
			{
				// Write the output file first so the phase is marked complete and won't retry,
				// then surface the failure count in the result message.
				return PhaseExecutionResult.Done($"Created {Path.GetFileName(outputFile)} with {movedCount} transfer(s) and {transferFailures} failure(s).");
			}

			return PhaseExecutionResult.Done($"Created {Path.GetFileName(outputFile)} and transferred {movedCount} draft copy/copies.");
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

		private static List<PublishDirectionDecision> ParseDecisions(string response)
		{
			if (!TryExtractJsonArray(response, out var jsonArray))
			{
				return new List<PublishDirectionDecision>();
			}

			List<PublishDirectionDecision>? decision;
			try
			{
				decision = JsonSerializer.Deserialize<List<PublishDirectionDecision>>(jsonArray, PrettyJson);
			}
			catch
			{
				return new List<PublishDirectionDecision>();
			}

			if (decision is null || decision.Count == 0)
			{
				return new List<PublishDirectionDecision>();
			}

			return decision
				.Where(d => !string.IsNullOrWhiteSpace(d.store))
				.Select(d => new PublishDirectionDecision { store = d.store.Trim(), reason = d.reason?.Trim() ?? string.Empty })
				.Where(d => d.store.Equals("Etsy", StringComparison.OrdinalIgnoreCase) || d.store.Equals("Ebay", StringComparison.OrdinalIgnoreCase))
				.GroupBy(d => d.store, StringComparer.OrdinalIgnoreCase)
				.Select(g => g.First())
				.ToList();
		}
	}
}
