using System.Text.Json;
using System.Text.RegularExpressions;

public static partial class PhaseFactory
{
	private sealed class Phase5MetadataGenerator : PhaseGeneratorBase
	{
		private const int MaxTagCount = 15;
		private const int MaxTagLength = 32;
		private static readonly Regex CollapseWhitespaceRegex = new(@"\s+", RegexOptions.Compiled);
		private static readonly Regex AllowedTagCharsRegex = new(@"[^\p{L}\p{N}\s\-]", RegexOptions.Compiled);

		public Phase5MetadataGenerator() : base(5, "Metadata Generated", "id/phase5.PID.Meta.json => ProductMeta") { }

		protected override bool IsCompleteCore(PhaseBundle bundle) => Phase5Complete(bundle);

		protected override bool CanRunCore(PhaseBundle bundle)
			=> File.Exists(GetPhase4Path(bundle));

		protected override async Task<PhaseExecutionResult> ExecuteCoreAsync(PhaseBundle bundle, CancellationToken cancellationToken)
		{
			var pids = bundle.ReadPhase4ProductIds();
			if (pids.Count == 0)
			{
				return PhaseExecutionResult.Done("No phase4 products to generate metadata for.");
			}

			var runtime = CombinedRuntime.Current;
			var printify = runtime.GetPrintifyClient();
			var stagingShopId = await runtime.ResolveShopIdAsync("Staging");
			using var ollama = runtime.CreateOllamaClient();

			var changed = 0;
			var updated = 0;
			foreach (var pid in pids)
			{
				var outputFile = GetPhase5Path(bundle, pid);
				var product = await printify.GetProductAsync(stagingShopId, pid);
				ProductMeta? meta = null;

				if (File.Exists(outputFile))
				{
					try
					{
						meta = JsonSerializer.Deserialize<ProductMeta>(File.ReadAllText(outputFile), PrettyJson);
					}
					catch
					{
						meta = null;
					}
				}

				if (meta is null)
				{
					var imagePaths = await runtime.DownloadProductImagesAsync(product.Images?.Select(i => i.Src) ?? Enumerable.Empty<string>(), 6, cancellationToken);
					try
					{
						if (imagePaths.Count == 0)
						{
							meta = new ProductMeta
						{
								Title = product.Title,
								Description = product.Description,
								Tags = product.Tags.ToArray(),
							};
						}
						else
						{
							for (var attempt = 1; attempt <= 4; attempt++)
							{
								var prompt = attempt == 1
									? ProductMetaPrompt
									: ProductMetaPrompt + "\n\nIMPORTANT: Previous response was not valid JSON. Return only one valid JSON object and nothing else.";

								var response = await CollectStreamAsync(
									ollama.GenerateWithImagesStreamAsync(runtime.Settings.MockupVisionModel, prompt, imagePaths.ToArray(), cancellationToken),
									cancellationToken);

								if (!TryExtractJsonObject(response, out var jsonObject))
								{
									continue;
								}

								try
								{
									meta = JsonSerializer.Deserialize<ProductMeta>(jsonObject, PrettyJson);
								}
								catch
								{
									meta = null;
								}

								if (meta is not null)
								{
									break;
								}
							}
						}

						if (meta is null)
						{
							throw new InvalidDataException("Invalid ProductMeta response JSON after retries.");
						}

						File.WriteAllText(outputFile, JsonSerializer.Serialize(meta, PrettyJson));
						changed++;
					}
					finally
					{
						runtime.TryDeleteFiles(imagePaths);
					}
				}

				if (meta is null)
				{
					continue;
				}

				var nextTitle = NormalizeTitle(meta.Title, product.Title);
				var nextDescription = NormalizeDescription(meta.Description, product.Description);
				var nextTags = NormalizeTags(meta.Tags, product.Tags);

				await UpdateMetadataWithFallbackAsync(printify, stagingShopId, pid, nextTitle, nextDescription, nextTags, cancellationToken);

				updated++;
			}

			return PhaseExecutionResult.Done($"Created phase5 metadata ({changed}) and updated products ({updated}).");
		}

		private static string NormalizeTitle(string? candidate, string fallback)
		{
			var normalized = NormalizeText(string.IsNullOrWhiteSpace(candidate) ? fallback : candidate);
			if (normalized.Length >= 80)
			{
				normalized = normalized[..79].TrimEnd();
			}

			if (string.IsNullOrWhiteSpace(normalized))
			{
				normalized = NormalizeText(fallback);
			}

			return normalized;
		}

		private static string NormalizeDescription(string? candidate, string fallback)
		{
			var normalized = NormalizeText(string.IsNullOrWhiteSpace(candidate) ? fallback : candidate);
			if (normalized.Length >= 3000)
			{
				normalized = normalized[..2999].TrimEnd();
			}

			if (string.IsNullOrWhiteSpace(normalized))
			{
				normalized = NormalizeText(fallback);
			}

			return normalized;
		}

		private static List<string> NormalizeTags(IEnumerable<string?>? candidates, IEnumerable<string?> fallback)
		{
			var sanitized = (candidates ?? Array.Empty<string?>())
				.Select(NormalizeTag)
				.Where(tag => !string.IsNullOrWhiteSpace(tag))
				.Select(tag => tag!)
				.Distinct(StringComparer.OrdinalIgnoreCase)
				.Take(MaxTagCount)
				.ToList();

			if (sanitized.Count > 0)
			{
				return sanitized;
			}

			return (fallback ?? Array.Empty<string?>())
				.Select(NormalizeTag)
				.Where(tag => !string.IsNullOrWhiteSpace(tag))
				.Select(tag => tag!)
				.Distinct(StringComparer.OrdinalIgnoreCase)
				.Take(MaxTagCount)
				.ToList();
		}

		private static string NormalizeText(string value)
		{
			if (string.IsNullOrWhiteSpace(value))
			{
				return string.Empty;
			}

			var withoutControls = new string(value.Where(ch => !char.IsControl(ch) || ch is '\n' or '\r' or '\t').ToArray());
			return CollapseWhitespaceRegex.Replace(withoutControls, " ").Trim();
		}

		private static string? NormalizeTag(string? value)
		{
			if (string.IsNullOrWhiteSpace(value))
			{
				return null;
			}

			var normalized = NormalizeText(value);
			normalized = AllowedTagCharsRegex.Replace(normalized, string.Empty);
			normalized = CollapseWhitespaceRegex.Replace(normalized, " ").Trim();

			if (normalized.Length == 0)
			{
				return null;
			}

			if (normalized.Length > MaxTagLength)
			{
				normalized = normalized[..MaxTagLength].TrimEnd();
			}

			return normalized;
		}

		private static async Task UpdateMetadataWithFallbackAsync(
			PrintifyClient printify,
			int shopId,
			string productId,
			string title,
			string description,
			List<string> tags,
			CancellationToken cancellationToken)
		{
			cancellationToken.ThrowIfCancellationRequested();

			try
			{
				await printify.UpdateProductAsync(shopId, productId, new UpdateProductRequest
				{
					Title = title,
					Description = description,
					Tags = tags,
				});
				return;
			}
			catch (PrintifyApiException ex) when (ex.StatusCode >= 500)
			{
				Console.WriteLine($"Phase 5 full metadata update failed for product {productId} with status {ex.StatusCode}. Retrying without tags.");
			}

			try
			{
				await printify.UpdateProductAsync(shopId, productId, new UpdateProductRequest
				{
					Title = title,
					Description = description,
				});
				return;
			}
			catch (PrintifyApiException ex) when (ex.StatusCode >= 500)
			{
				Console.WriteLine($"Phase 5 title+description update failed for product {productId} with status {ex.StatusCode}. Retrying with title only.");
			}

			await printify.UpdateProductAsync(shopId, productId, new UpdateProductRequest
			{
				Title = title,
			});
		}
	}
}
