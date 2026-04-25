using System.Globalization;
using System.Net.Http;
using System.Text.Json;

public static partial class PhaseFactory
{
	public static IReadOnlyList<IPhaseGenerator> CreatePipeline() =>
		new IPhaseGenerator[]
		{
			new Phase1PromptGenerator(),
			new Phase2ImageGenerator(),
			new Phase3SuitabilityGenerator(),
			new Phase4ProductsGenerator(),
			new Phase5MetadataGenerator(),
			new Phase6ProductAssessmentGenerator(),
			new Phase7PublishingDirectionGenerator(),
			new Phase8PricingGenerator(),
			new Phase9ManualPublishingMarker(),
		};

	private static readonly JsonSerializerOptions PrettyJson = new()
	{
		WriteIndented = true,
	};

	private const string PromptGenerationInstruction = @"You are an expert prompt engineer for commercial print-on-demand artwork.
Create one high-converting image-generation prompt for a broad audience.

Output requirements:
- Return ONLY valid JSON.
- No markdown, no code fences, no extra text.
- Return exactly one array item with exactly these keys.

JSON format:
[
	{
		""positive"": ""detailed scene prompt with style, composition, lighting, color direction, and print-friendly details"",
		""negative"": ""artifact and quality blockers as comma-separated tags"",
		""width"": 768,
		""height"": 768,
		""steps"": 9,
		""cfg"": 2.5
	}
]

Quality rules:
- Positive prompt should be specific, visually rich, and include a clear main subject.
- Prefer clean silhouettes, strong contrast, and central composition suitable for apparel prints.
- Avoid references to logos, trademarks, celebrity likenesses, copyrighted characters, or artist names.
- Negative prompt should suppress blur, artifacts, text, watermarks, extra limbs, bad anatomy, noise, and low detail.
- Keep width, height, steps, and cfg exactly as shown above.";

	private const string ImageSuitabilityPrompt = @"Assess this image for safe, legal, and commercially viable print-on-demand use.

Output requirements:
- Return ONLY valid JSON.
- No markdown, no code fences, no extra keys, no extra text.
- Match the shape below exactly.

Scoring rules:
- All numeric scores are between 0.0 and 1.0.
- suitability is the overall confidence score.
- If any legal, IP, or NSFW risk is present, set the related boolean to true and include specific issue text.
- Issues should be short, concrete findings. Use an empty array when no issues are found.

Required JSON shape:
{
	""suitability"": 0.0,
	""DoesViolateLaw"": false,
	""DoesViolateIPRights"": false,
	""IsNSFW"": false,
	""Issues"": [""""],
	""Scoring"": {
		""commercialAppeal"": 0.0,
		""printQuality"": 0.0,
		""estimatedSalesViability"": 0.0,
		""uniqueness"": 0.0,
		""technicalSkill"": 0.0,
		""creativity"": 0.0,
		""composition"": 0.0,
		""technique"": 0.0,
		""originality"": 0.0
	}
}";

	private const string ProductAssessmentPrompt = @"Assess these product mockup images for marketplace readiness and technical quality.

Output requirements:
- Return ONLY valid JSON.
- No markdown, no code fences, no extra keys, no extra text.
- Match this shape exactly.

Decision rules:
- FitForPrintify is true only if the design appears printable, compliant i.e. does not violate any laws or IP rights, and visually acceptable for listing.
- shouldContinue is true only if generation should proceed to publishing decisions.
- Issues should contain concrete, short reasons (for example: low resolution, clipping, unreadable subject, risky IP elements).
- Use an empty Issues array when no issues are found.
- If the image is cut off or clipped in the mockup, but the original image is suitable, still return FitForPrintify as true and include an issue noting the clipping for later review.
- Dont allow images on breast pockets as they are embroidered and will be rejected by printify and cause listing issues, if the mockup contains a breast pocket with an image return FitForPrintify as false and include an issue noting this for later review.


Required JSON shape:
{
	""FitForPrintify"": true,
	""Issues"": [""""],
	""shouldContinue"": true
}";

	private const string ProductMetaPrompt = @"Generate conversion-focused product listing metadata from product mockup images.

Output requirements:
- Return ONLY valid JSON.
- No markdown, no code fences, no extra keys, no extra text.
- Match this shape exactly:
{
	""Title"": ""..."",
	""Description"": ""html description"",
	""Tags"": [""tag1"", ""tag2""]
}

Rules:
- Title must be less than 80 characters and should be concise dont fall for pictures of items on the printed item no tradmark terms or anything that could infringe IP rights.
- Description must be less than 3000 characters.
- Title should be specific, human-readable, and avoid keyword stuffing.
- Description should use simple HTML (p, ul, li, strong) and highlight benefits, material feel, and gift/use cases.
- Tags should be relevant search phrases, lowercase preferred, no duplicates, and no trademarked terms.
- Never include URLs, markdown, JSON comments, or text outside the JSON object.";

	private const string PublishDirectionPromptTemplate = @"Choose the best marketplace destination for this POD product.

Output requirements:
- Return ONLY valid JSON object.
- No markdown, no code fences, no extra keys, no extra text.
- Use exactly this shape:
[
{
	""store"": ""Etsy"" or ""Ebay"",
	""reason"": ""short reason""
}
]
Rules:
- Allowed stores are only Etsy and Ebay.
- Choose one or both stores based on product style, buyer intent, and marketplace fit.
- reason must be concise (one short sentence) and specific.

Product context:
{0}";

	private static string GetPhase4Path(PhaseBundle bundle)
	{
		var canonical = Path.Combine(bundle.DirectoryPath, "phase4.txt");
		if (File.Exists(canonical))
		{
			return canonical;
		}

		var legacy = Path.Combine(bundle.DirectoryPath, "phase_4.txt");
		if (File.Exists(legacy))
		{
			return legacy;
		}

		return canonical;
	}

	private static string GetPhase5Path(PhaseBundle bundle, string pid)
	{
		var upper = Path.Combine(bundle.DirectoryPath, $"phase5.{pid}.Meta.json");
		if (File.Exists(upper))
		{
			return upper;
		}

		var lower = Path.Combine(bundle.DirectoryPath, $"phase5.{pid}.meta.json");
		if (File.Exists(lower))
		{
			return lower;
		}

		return upper;
	}

	private static string GetPhase6Path(PhaseBundle bundle, string pid)
		=> Path.Combine(bundle.DirectoryPath, $"phase6.{pid}.json");

	private static bool Phase5Complete(PhaseBundle bundle)
	{
		var pids = bundle.ReadPhase4ProductIds();
		if (pids.Count == 0)
		{
			return File.Exists(GetPhase4Path(bundle));
		}

		return pids.All(pid => File.Exists(GetPhase5Path(bundle, pid)));
	}

	private static bool Phase6Complete(PhaseBundle bundle)
	{
		var pids = bundle.ReadPhase4ProductIds();
		if (pids.Count == 0)
		{
			return Phase5Complete(bundle);
		}

		return pids.All(pid => File.Exists(GetPhase6Path(bundle, pid)));
	}

	private static ImageSuitability? ReadImageSuitability(PhaseBundle bundle)
	{
		var path = bundle.ResolvePhaseFile(3, "json", underscoreLegacy: true);
		if (!File.Exists(path))
		{
			return null;
		}

		return JsonSerializer.Deserialize<ImageSuitability>(File.ReadAllText(path));
	}

	private sealed class PublishDirectionDecision
	{
		public string store { get; set; } = string.Empty;
		public string reason { get; set; } = string.Empty;
	}

	private static async Task<string> CollectStreamAsync(IAsyncEnumerable<string> source, CancellationToken cancellationToken)
	{
		var sb = new System.Text.StringBuilder();
		await foreach (var token in source.WithCancellation(cancellationToken))
		{
            // Console.Write(token);
			sb.Append(token);
		}

		return sb.ToString();
	}

	private static bool TryExtractJsonObject(string response, out string json)
	{
		return TryExtractJsonByRootKind(response, '{', '}', out json);
	}

	private static bool TryExtractJsonArray(string response, out string json)
	{
		return TryExtractJsonByRootKind(response, '[', ']', out json);
	}

	private static bool TryExtractJsonByRootKind(string response, char open, char close, out string json)
	{
		json = string.Empty;
		if (string.IsNullOrWhiteSpace(response))
		{
			return false;
		}

		var sanitized = NormalizeLlmResponse(response);
		if (TryValidateJsonRoot(sanitized, open, close, out json))
		{
			return true;
		}

		if (!TryExtractBalancedJson(sanitized, open, close, out var extracted))
		{
			return false;
		}

		return TryValidateJsonRoot(extracted, open, close, out json);
	}

	private static string NormalizeLlmResponse(string response)
	{
		var normalized = TryExtractOllamaResponseBody(response);
		normalized = StripMarkdownFence(normalized);

		if (normalized.StartsWith("json", StringComparison.OrdinalIgnoreCase))
		{
			normalized = normalized[4..].TrimStart();
		}

		return normalized.Trim();
	}

	private static string TryExtractOllamaResponseBody(string response)
	{
		var trimmed = response.Trim();
		if (string.IsNullOrWhiteSpace(trimmed) || !trimmed.StartsWith('{'))
		{
			return trimmed;
		}

		try
		{
			using var document = JsonDocument.Parse(trimmed);
			if (document.RootElement.ValueKind == JsonValueKind.Object
				&& document.RootElement.TryGetProperty("response", out var responseElement)
				&& responseElement.ValueKind == JsonValueKind.String)
			{
				return responseElement.GetString()?.Trim() ?? string.Empty;
			}
		}
		catch
		{
			// Keep original response when envelope parsing fails.
		}

		return trimmed;
	}

	private static string StripMarkdownFence(string response)
	{
		var trimmed = response.Trim();
		if (!trimmed.StartsWith("```", StringComparison.Ordinal))
		{
			return trimmed;
		}

		var withoutOpening = trimmed[3..].TrimStart();
		if (withoutOpening.StartsWith("json", StringComparison.OrdinalIgnoreCase))
		{
			withoutOpening = withoutOpening[4..].TrimStart();
		}

		var closingFence = withoutOpening.LastIndexOf("```", StringComparison.Ordinal);
		if (closingFence >= 0)
		{
			return withoutOpening[..closingFence].Trim();
		}

		var lines = trimmed.Split('\n');
		if (lines.Length < 2)
		{
			return withoutOpening.Trim();
		}

		var start = 1;
		var end = lines.Length;
		if (lines[^1].TrimStart().StartsWith("```", StringComparison.Ordinal))
		{
			end = lines.Length - 1;
		}

		return string.Join("\n", lines[start..end]).Trim();
	}

	private static bool TryValidateJsonRoot(string text, char open, char close, out string json)
	{
		json = string.Empty;
		if (string.IsNullOrWhiteSpace(text))
		{
			return false;
		}

		var trimmed = text.Trim();
		if (trimmed[0] != open || trimmed[^1] != close)
		{
			return false;
		}

		try
		{
			using var doc = JsonDocument.Parse(trimmed);
			if ((open == '{' && doc.RootElement.ValueKind != JsonValueKind.Object)
				|| (open == '[' && doc.RootElement.ValueKind != JsonValueKind.Array))
			{
				return false;
			}

			json = trimmed;
			return true;
		}
		catch
		{
			return false;
		}
	}

	private static bool TryExtractBalancedJson(string text, char open, char close, out string json)
	{
		json = string.Empty;
		var start = text.IndexOf(open);
		if (start < 0)
		{
			return false;
		}

		var depth = 0;
		var inString = false;
		var escaped = false;
		for (var i = start; i < text.Length; i++)
		{
			var ch = text[i];
			if (inString)
			{
				if (escaped)
				{
					escaped = false;
					continue;
				}

				if (ch == '\\')
				{
					escaped = true;
					continue;
				}

				if (ch == '"')
				{
					inString = false;
				}

				continue;
			}

			if (ch == '"')
			{
				inString = true;
				continue;
			}

			if (ch == open)
			{
				depth++;
			}
			else if (ch == close)
			{
				depth--;
				if (depth == 0)
				{
					json = text[start..(i + 1)];
					return true;
				}
			}
		}

		return false;
	}

	private sealed class CombinedRuntime
	{
		private static readonly Lazy<CombinedRuntime> InstanceFactory = new(() => new CombinedRuntime());
		public static CombinedRuntime Current => InstanceFactory.Value;

		private readonly RoundRobinSelector<OrchestrationNode> _ollamaSelector;
		private readonly RoundRobinSelector<OrchestrationNode> _comfySelector;
		private readonly HttpClient _http = new();
		private readonly Lazy<PrintifyClient> _printifyClient;
		private List<Shop>? _shops;

		private CombinedRuntime()
		{
			RepositoryRoot = ResolveRepositoryRoot() ?? Directory.GetCurrentDirectory();
			DataRoot = Path.Combine(RepositoryRoot, "src", "data");
			Settings = OrchestrationSettingsStore.Load(DataRoot);

			var ollamaNodes = Settings.Ollama
				.Where(node => node.Enabled && !string.IsNullOrWhiteSpace(node.BaseUrl))
				.ToList();
			if (ollamaNodes.Count == 0)
			{
				throw new InvalidOperationException("No enabled Ollama nodes found in orchestration settings.");
			}

			var comfyNodes = Settings.ComfyUi
				.Where(node => node.Enabled && !string.IsNullOrWhiteSpace(node.BaseUrl))
				.ToList();
			if (comfyNodes.Count == 0)
			{
				throw new InvalidOperationException("No enabled ComfyUI nodes found in orchestration settings.");
			}

			_ollamaSelector = new RoundRobinSelector<OrchestrationNode>(ollamaNodes);
			_comfySelector = new RoundRobinSelector<OrchestrationNode>(comfyNodes);

			Token = ReadToken(RepositoryRoot)
				?? throw new InvalidOperationException("TOKEN is missing from main.env; Printify phases require it.");

			_printifyClient = new Lazy<PrintifyClient>(() => new PrintifyClient(Token));
		}

		public string RepositoryRoot { get; }
		public string DataRoot { get; }
		public string Token { get; }
		public OrchestrationSettings Settings { get; }

		public OllamaClient CreateOllamaClient() => new(_ollamaSelector.Next().BaseUrl);

		public string NextComfyBaseUrl() => _comfySelector.Next().BaseUrl;

		public PrintifyClient GetPrintifyClient() => _printifyClient.Value;

		public async Task<int> ResolveShopIdAsync(string preferredTitle)
		{
			if (_shops is null)
			{
				_shops = await GetPrintifyClient().GetShopsAsync();
			}

			var preferred = _shops.FirstOrDefault(shop => string.Equals(shop.Title, preferredTitle, StringComparison.OrdinalIgnoreCase));
			if (preferred is not null)
			{
				return preferred.Id;
			}

			if (_shops.Count == 0)
			{
				throw new InvalidOperationException("No Printify shops are available for this token.");
			}

			return _shops[0].Id;
		}

		public async Task<List<Shop>> ResolveShopsByNamesAsync(IEnumerable<string> requestedNames)
		{
			if (_shops is null)
			{
				_shops = await GetPrintifyClient().GetShopsAsync();
			}

			var shops = _shops ?? new List<Shop>();
			var resolved = new List<Shop>();

			foreach (var requestedName in requestedNames.Where(name => !string.IsNullOrWhiteSpace(name)))
			{
				var normalized = requestedName.Trim();
				var match = shops.FirstOrDefault(shop =>
					string.Equals(shop.Title, normalized, StringComparison.OrdinalIgnoreCase)
					|| shop.Title.Contains(normalized, StringComparison.OrdinalIgnoreCase)
					|| normalized.Contains(shop.Title, StringComparison.OrdinalIgnoreCase));

				if (match is not null && resolved.All(existing => existing.Id != match.Id))
				{
					resolved.Add(match);
				}
			}

			return resolved;
		}

		public async Task<List<string>> DownloadProductImagesAsync(IEnumerable<string> urls, int maxCount, CancellationToken cancellationToken)
		{
			var files = new List<string>();
			foreach (var url in urls.Where(url => !string.IsNullOrWhiteSpace(url)).Take(Math.Max(1, maxCount)))
			{
				cancellationToken.ThrowIfCancellationRequested();
				var bytes = await _http.GetByteArrayAsync(url, cancellationToken);
				var ext = Path.GetExtension(url.Split('?', 2)[0]);
				if (string.IsNullOrWhiteSpace(ext))
				{
					ext = ".png";
				}

				var tempPath = Path.Combine(Path.GetTempPath(), $"pg-combined-{Guid.NewGuid():N}{ext}");
				await File.WriteAllBytesAsync(tempPath, bytes, cancellationToken);
				files.Add(tempPath);
			}

			return files;
		}

		public void TryDeleteFiles(IEnumerable<string> files)
		{
			foreach (var file in files)
			{
				try
				{
					if (File.Exists(file))
					{
						File.Delete(file);
					}
				}
				catch
				{
					// best effort temp-file cleanup
				}
			}
		}

		private static string? ResolveRepositoryRoot()
		{
			var probeRoots = new[]
			{
				Directory.GetCurrentDirectory(),
				AppContext.BaseDirectory,
			}
			.Where(path => !string.IsNullOrWhiteSpace(path))
			.Distinct(StringComparer.OrdinalIgnoreCase);

			foreach (var probeRoot in probeRoots)
			{
				var current = new DirectoryInfo(Path.GetFullPath(probeRoot));
				while (current is not null)
				{
					if (File.Exists(Path.Combine(current.FullName, "PrintifyGenerator.sln")))
					{
						return current.FullName;
					}

					current = current.Parent;
				}
			}

			return null;
		}

		private static string? ReadToken(string repositoryRoot)
		{
			var envPath = Path.Combine(repositoryRoot, "main.env");
			if (!File.Exists(envPath))
			{
				return null;
			}

			foreach (var rawLine in File.ReadLines(envPath))
			{
				var line = rawLine.Trim();
				if (line.StartsWith("TOKEN=", StringComparison.OrdinalIgnoreCase))
				{
					return line["TOKEN=".Length..].Trim();
				}
			}

			return null;
		}
	}
}
