using System.Text.Json;

public static partial class PhaseFactory
{
	private sealed class Phase6ProductAssessmentGenerator : PhaseGeneratorBase
	{
		public Phase6ProductAssessmentGenerator() : base(6, "Products Assessed", "id/phase6.PID.json => ProductAssessment") { }

		protected override bool IsCompleteCore(PhaseBundle bundle) => Phase6Complete(bundle);

		protected override bool CanRunCore(PhaseBundle bundle)
			=> Phase5Complete(bundle);

		protected override async Task<PhaseExecutionResult> ExecuteCoreAsync(PhaseBundle bundle, CancellationToken cancellationToken)
		{
			var pids = bundle.ReadPhase4ProductIds();
			if (pids.Count == 0)
			{
				return PhaseExecutionResult.Done("No phase4 products to assess.");
			}

			var runtime = CombinedRuntime.Current;
			var printify = runtime.GetPrintifyClient();
			var stagingShopId = await runtime.ResolveShopIdAsync("Staging");
			using var ollama = runtime.CreateOllamaClient();

			var changed = 0;
			var deleted = 0;
			var surviving = new List<string>();
			foreach (var pid in pids)
			{
				var outputFile = GetPhase6Path(bundle, pid);
				ProductAssessment? assessment = null;
				var createdAssessment = false;

				if (File.Exists(outputFile))
				{
					assessment = JsonSerializer.Deserialize<ProductAssessment>(File.ReadAllText(outputFile));
				}
				else
				{
					var imagePaths = new List<string>();
					try
					{
						var product = await printify.GetProductAsync(stagingShopId, pid);
						var metaPath = GetPhase5Path(bundle, pid);
						var meta = File.Exists(metaPath)
							? JsonSerializer.Deserialize<ProductMeta>(File.ReadAllText(metaPath), PrettyJson)
							: null;
						imagePaths = await runtime.DownloadProductImagesAsync(product.Images?.Select(i => i.Src) ?? Enumerable.Empty<string>(), 4, cancellationToken);

						if (imagePaths.Count == 0)
						{
							assessment = new ProductAssessment
							{
								FitForPrintify = false,
								Issues = new[] { "No product mockup images found." },
								shouldContinue = false,
							};
						}
						else
						{
							for (var attempt = 1; attempt <= 4; attempt++)
							{
								var prompt = attempt == 1
									? BuildProductAssessmentPrompt(product, meta)
									: BuildProductAssessmentPrompt(product, meta) + "\n\nIMPORTANT: Previous response was not valid JSON. Return only one valid JSON object and nothing else.";

								var response = await CollectStreamAsync(
									ollama.GenerateWithImagesStreamAsync(runtime.Settings.SuitabilityModel, prompt, imagePaths.ToArray(), cancellationToken),
									cancellationToken);

								if (!TryExtractJsonObject(response, out var jsonObject))
								{
									continue;
								}

								try
								{
									assessment = JsonSerializer.Deserialize<ProductAssessment>(jsonObject, PrettyJson);
								}
								catch
								{
									assessment = null;
								}

								if (assessment is not null)
								{
									break;
								}
							}

							if (assessment is null)
							{
								throw new InvalidDataException("Invalid ProductAssessment response JSON after retries.");
							}
						}
					}
					catch (Exception ex)
					{
						assessment = new ProductAssessment
						{
							FitForPrintify = false,
							Issues = new[] { $"Assessment failed: {ex.Message}" },
							shouldContinue = false,
						};
					}
					finally
					{
						runtime.TryDeleteFiles(imagePaths);
					}

					File.WriteAllText(outputFile, JsonSerializer.Serialize(assessment, PrettyJson));
					createdAssessment = true;
				}

				if (createdAssessment)
				{
					changed++;
				}

				if (assessment?.FitForPrintify == true && assessment.shouldContinue)
				{
					surviving.Add(pid);
					continue;
				}

				try
				{
					await printify.DeleteProductAsync(stagingShopId, pid);
					deleted++;
				}
				catch
				{
					// Best effort: product may already be deleted.
				}
			}

			var phase4Path = GetPhase4Path(bundle);
			File.WriteAllLines(phase4Path, surviving.Distinct(StringComparer.OrdinalIgnoreCase));

			return PhaseExecutionResult.Done($"Created phase6 assessments ({changed}), deleted {deleted} failed product(s), kept {surviving.Count}.");
		}

		private static string BuildProductAssessmentPrompt(Product product, ProductMeta? meta)
		{
			var title = string.IsNullOrWhiteSpace(meta?.Title) ? product.Title : meta!.Title.Trim();
			var description = string.IsNullOrWhiteSpace(meta?.Description) ? product.Description : meta!.Description.Trim();

			return $"{ProductAssessmentPrompt}\n\nAssess the product mockup images together with this listing copy:\nTitle: {title}\nDescription:\n{description}";
		}
	}
}
