using System.Globalization;
using System.Text.Json;
using System.Text.RegularExpressions;

internal sealed record ProductDefinitionInput
{
	public string BlueprintName { get; init; } = string.Empty;
	public string Title { get; init; } = string.Empty;
	public string Description { get; init; } = string.Empty;
	public string MainCategory { get; init; } = string.Empty;
	public string SubCategory { get; init; } = string.Empty;
	public string Audience { get; init; } = string.Empty;
	public string PrimaryColor { get; init; } = string.Empty;
	public string Material { get; init; } = string.Empty;
	public string UseCase { get; init; } = string.Empty;
	public string[] Keywords { get; init; } = Array.Empty<string>();
}

internal sealed record BlueprintQueryInsight
{
	public string BlueprintName { get; init; } = string.Empty;
	public ProductDefinitionInput Definition { get; init; } = new();
	public SellabilityInsight Sellability { get; init; } = new();
	public SeoHintBundle SeoHints { get; init; } = new();
	public ShopRecommendationBundle ShopRecommendations { get; init; } = new();
	public MarketPriceGuidance PriceGuidance { get; init; } = new();
	public CategoryColorPairing[] CategoryColorPairs { get; init; } = Array.Empty<CategoryColorPairing>();
	public string[] PromptAnchors { get; init; } = Array.Empty<string>();
}

internal sealed record SellabilityInsight
{
	public ProductDefinitionInput Definition { get; init; } = new();
	public double Score { get; init; }
	public string ScoreBand { get; init; } = "Unknown";
	public string[] RecommendedKeywords { get; init; } = Array.Empty<string>();
	public string[] RecommendedColors { get; init; } = Array.Empty<string>();
	public string[] RecommendedMaterials { get; init; } = Array.Empty<string>();
	public string[] RecommendedCategoryColorPairs { get; init; } = Array.Empty<string>();
	public string[] Notes { get; init; } = Array.Empty<string>();
}

internal sealed record SeoHintBundle
{
	public string[] HighIntentKeywords { get; init; } = Array.Empty<string>();
	public string[] SecondaryKeywords { get; init; } = Array.Empty<string>();
	public string[] SuggestedAngles { get; init; } = Array.Empty<string>();
}

internal sealed record ShopRecommendation
{
	public string Store { get; init; } = string.Empty;
	public double Score { get; init; }
	public string Reason { get; init; } = string.Empty;
}

internal sealed record ShopRecommendationBundle
{
	public ShopRecommendation[] Recommendations { get; init; } = Array.Empty<ShopRecommendation>();
	public string[] Notes { get; init; } = Array.Empty<string>();
}

internal sealed record CategoryColorPairing
{
	public string Category { get; init; } = string.Empty;
	public string Color { get; init; } = string.Empty;
	public double Score { get; init; }
	public int ProductCount { get; init; }
	public long TotalSales { get; init; }
}

internal sealed record MarketPriceGuidance
{
	public double SuggestedMinPrice { get; init; }
	public double SuggestedMaxPrice { get; init; }
	public double MedianCompetitivePrice { get; init; }
	public string[] CompetitiveTerms { get; init; } = Array.Empty<string>();
	public string[] Notes { get; init; } = Array.Empty<string>();
}

internal sealed class CategoryFeatureIntelligence
{
	private static readonly Regex HeaderRegex = new(
		@"^===\s*(?<name>.+?)\s*\((?<count>[0-9,]+)\s+products,\s+Type:\s*(?<type>[^\)]+)\)\s*===",
		RegexOptions.Compiled | RegexOptions.CultureInvariant);

	private static readonly Regex SentimentRegex = new(
		@"^\s{2}(?<term>[^:]{2,}):\s*(?<pct>[0-9]+(?:\.[0-9]+)?)%\s+positive\s+\((?<count>[0-9,]+)\s+products\)",
		RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.IgnoreCase);

	private static readonly Regex CtrRegex = new(
		@"^\s{2}(?<term>[^:]{2,}):\s*\+?(?<lift>[0-9]+(?:\.[0-9]+)?)%\s+CTR\s+lift\s+\((?<top>[0-9,]+)\s+in\s+top\s*/\s*(?<bottom>[0-9,]+)\s+in\s+bottom\)",
		RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.IgnoreCase);

	private static readonly Regex ColorOrMaterialRegex = new(
		@"^\s{2}(?<name>[^:]{2,}):\s*(?<count>[0-9,]+)\s+products,\s*(?<sent>[0-9]+(?:\.[0-9]+)?)\s+avg\s+sentiment,\s*(?<sales>[0-9,]+)\s+(?:total\s+)?sales",
		RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.IgnoreCase);

	private static readonly Regex PriceTermRegex = new(
		@"^\s{2}(?<term>[^:]{2,}):\s*\$(?<price>[0-9]+(?:\.[0-9]+)?)\s+avg\s+\(ratio:\s*(?<ratio>[0-9]+(?:\.[0-9]+)?)\)\s*-\s*(?<tag>CHEAPER|PRICIER)\s+\((?<count>[0-9,]+)\s+products\)",
		RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.IgnoreCase);

	private static readonly Regex WordRegex = new("[a-z0-9][a-z0-9\\-']+", RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.IgnoreCase);

	private readonly Dictionary<string, CategorySnapshot> _categories;
	private readonly Dictionary<string, KeywordMetric> _keywords;
	private readonly Dictionary<string, ColorMetric> _colors;
	private readonly Dictionary<string, MaterialMetric> _materials;
	private readonly Dictionary<string, AggregatePriceTerm> _priceTerms;
	private readonly string _historyPath;
	private readonly object _historyLock = new();
	private StoreLearningHistory _history;

	private CategoryFeatureIntelligence(
		Dictionary<string, CategorySnapshot> categories,
		Dictionary<string, KeywordMetric> keywords,
		Dictionary<string, ColorMetric> colors,
		Dictionary<string, MaterialMetric> materials,
		Dictionary<string, AggregatePriceTerm> priceTerms,
		string historyPath,
		StoreLearningHistory history)
	{
		_categories = categories;
		_keywords = keywords;
		_colors = colors;
		_materials = materials;
		_priceTerms = priceTerms;
		_historyPath = historyPath;
		_history = history;
	}

	public static CategoryFeatureIntelligence Load(string categoryFeaturesDirectory, string historyPath)
	{
		var categories = new Dictionary<string, CategorySnapshot>(StringComparer.OrdinalIgnoreCase);
		if (Directory.Exists(categoryFeaturesDirectory))
		{
			foreach (var file in Directory.EnumerateFiles(categoryFeaturesDirectory, "*.txt", SearchOption.TopDirectoryOnly))
			{
				var snapshot = ParseCategoryFile(file);
				if (snapshot is null)
				{
					continue;
				}

				categories[snapshot.Name] = snapshot;
			}
		}

		var keywords = BuildKeywordIndex(categories.Values);
		var colors = BuildColorIndex(categories.Values);
		var materials = BuildMaterialIndex(categories.Values);
		var priceTerms = BuildPriceTermIndex(categories.Values);
		var history = ReadHistory(historyPath);

		return new CategoryFeatureIntelligence(categories, keywords, colors, materials, priceTerms, historyPath, history);
	}

	public ProductDefinitionInput GenerateRandomDefinition(Guid seed)
	{
		var random = new Random(seed.GetHashCode());
		var selectedCategory = _categories.Values
			.Where(category => category.ProductCount > 0 &&
				(category.Type.Equals("MainCategory", StringComparison.OrdinalIgnoreCase)
				 || category.Type.Equals("SubCategory", StringComparison.OrdinalIgnoreCase)
				 || category.Type.Equals("Gender", StringComparison.OrdinalIgnoreCase)
				 || category.Type.Equals("PriceRange", StringComparison.OrdinalIgnoreCase)))
			.OrderByDescending(category => category.ProductCount)
			.Take(20)
			.OrderBy(_ => random.Next())
			.FirstOrDefault();

		var color = _colors.Values
			.OrderByDescending(metric => metric.Score)
			.Take(20)
			.OrderBy(_ => random.Next())
			.Select(metric => metric.Name)
			.FirstOrDefault() ?? string.Empty;

		var material = _materials.Values
			.OrderByDescending(metric => metric.Score)
			.Take(20)
			.OrderBy(_ => random.Next())
			.Select(metric => metric.Name)
			.FirstOrDefault() ?? string.Empty;

		var keywords = _keywords.Values
			.OrderByDescending(metric => metric.Score)
			.Take(120)
			.OrderBy(_ => random.Next())
			.Take(6)
			.Select(metric => metric.Term)
			.ToArray();

		var titleParts = keywords.Take(3).Select(CultureInfo.InvariantCulture.TextInfo.ToTitleCase).ToArray();
		var generatedTitle = titleParts.Length > 0 ? string.Join(" ", titleParts) : "Commercial POD Design";

		return new ProductDefinitionInput
		{
			Title = generatedTitle,
			MainCategory = selectedCategory?.Name ?? string.Empty,
			Audience = InferAudience(selectedCategory?.Name ?? string.Empty),
			PrimaryColor = color,
			Material = material,
			UseCase = "everyday gifting and self-purchase",
			Keywords = keywords,
		};
	}

	public ProductDefinitionInput BuildDefinitionFromBlueprintName(string blueprintName)
	{
		var normalizedBlueprint = NormalizeTitle(blueprintName);
		var terms = ExtractWords(blueprintName)
			.Select(NormalizeToken)
			.Where(term => term.Length >= 3)
			.Distinct(StringComparer.OrdinalIgnoreCase)
			.ToArray();

		var categoryCandidates = _categories.Values
			.Select(category => new
			{
				Category = category,
				Score = ScoreCategoryMatch(category, terms, normalizedBlueprint),
			})
			.Where(candidate => candidate.Score > 0)
			.OrderByDescending(candidate => candidate.Score)
			.ThenByDescending(candidate => candidate.Category.ProductCount)
			.Take(8)
			.ToList();

		var primaryCategory = categoryCandidates.FirstOrDefault()?.Category;
		var subCategory = categoryCandidates
			.Select(candidate => candidate.Category)
			.FirstOrDefault(category => category.Type.Equals("SubCategory", StringComparison.OrdinalIgnoreCase));

		var keywordPool = new List<string>();
		keywordPool.AddRange(terms);

		foreach (var candidate in categoryCandidates.Take(4))
		{
			keywordPool.AddRange(candidate.Category.CtrTerms
				.OrderByDescending(term => term.Lift)
				.Take(4)
				.Select(term => term.Term));
			keywordPool.AddRange(candidate.Category.SentimentTerms
				.OrderByDescending(term => term.PositivePct)
				.Take(4)
				.Select(term => term.Term));
		}

		keywordPool.AddRange(_keywords.Values
			.OrderByDescending(metric => metric.Score)
			.Take(20)
			.Select(metric => metric.Term));

		var keywords = keywordPool
			.Select(NormalizeToken)
			.Where(value => value.Length >= 3)
			.Distinct(StringComparer.OrdinalIgnoreCase)
			.Take(10)
			.ToArray();

		var color = PickBlueprintColor(terms, categoryCandidates.Select(candidate => candidate.Category));
		var material = PickBlueprintMaterial(terms, categoryCandidates.Select(candidate => candidate.Category));

		var audience = InferAudience(blueprintName);
		if (string.Equals(audience, "unisex", StringComparison.OrdinalIgnoreCase) && primaryCategory is not null)
		{
			audience = InferAudience(primaryCategory.Name);
		}

		var inferredMainCategory = primaryCategory?.Type.Equals("MainCategory", StringComparison.OrdinalIgnoreCase) == true
			? NormalizeTitle(primaryCategory.Name)
			: NormalizeTitle(primaryCategory?.Name ?? string.Empty);

		var inferredSubCategory = NormalizeTitle(subCategory?.Name ?? string.Empty);
		var useCase = InferUseCaseFromBlueprint(blueprintName, terms, inferredMainCategory, inferredSubCategory);

		var definition = new ProductDefinitionInput
		{
			Title = string.IsNullOrWhiteSpace(normalizedBlueprint) ? "Blueprint Product" : normalizedBlueprint,
			Description = $"Commercial print design inspired by blueprint '{normalizedBlueprint}'.",
			MainCategory = inferredMainCategory,
			SubCategory = inferredSubCategory,
			Audience = audience,
			PrimaryColor = color,
			Material = material,
			UseCase = useCase,
			Keywords = keywords,
		};

		return NormalizeDefinition(definition);
	}

	public BlueprintQueryInsight QueryBlueprintStarter(string blueprintName, int seoKeywordCount = 12)
	{
		var definition = BuildDefinitionFromBlueprintName(blueprintName);
		var sellability = PredictSellability(definition);
		var seoHints = BuildSeoHints(definition, Math.Max(6, seoKeywordCount));
		var shopRecommendations = RecommendShops(definition, sellability.RecommendedKeywords, definition.Title);
		var priceGuidance = GetMarketPriceGuidance(definition);
		var colorPairs = GetColorCategoryPairings(10, definition).ToArray();

		var promptAnchors = new List<string>();
		promptAnchors.AddRange(sellability.RecommendedKeywords.Take(5));
		if (!string.IsNullOrWhiteSpace(definition.PrimaryColor))
		{
			promptAnchors.Add($"primary color: {definition.PrimaryColor}");
		}
		if (!string.IsNullOrWhiteSpace(definition.Material))
		{
			promptAnchors.Add($"material texture: {definition.Material}");
		}
		if (!string.IsNullOrWhiteSpace(definition.Audience))
		{
			promptAnchors.Add($"target audience: {definition.Audience}");
		}
		if (!string.IsNullOrWhiteSpace(definition.UseCase))
		{
			promptAnchors.Add($"use case: {definition.UseCase}");
		}

		return new BlueprintQueryInsight
		{
			BlueprintName = blueprintName,
			Definition = definition,
			Sellability = sellability,
			SeoHints = seoHints,
			ShopRecommendations = shopRecommendations,
			PriceGuidance = priceGuidance,
			CategoryColorPairs = colorPairs,
			PromptAnchors = promptAnchors
				.Where(anchor => !string.IsNullOrWhiteSpace(anchor))
				.Distinct(StringComparer.OrdinalIgnoreCase)
				.Take(16)
				.ToArray(),
		};
	}

	public SellabilityInsight PredictSellability(ProductDefinitionInput definition)
	{
		var normalizedDefinition = NormalizeDefinition(definition);
		var matchedCategories = FindMatchingCategories(normalizedDefinition).ToList();
		var keywordSignals = ExtractCandidateKeywords(normalizedDefinition)
			.Select(keyword => _keywords.TryGetValue(keyword, out var metric) ? metric : null)
			.Where(metric => metric is not null)
			.Select(metric => metric!)
			.OrderByDescending(metric => metric.Score)
			.ToList();

		var notes = new List<string>();
		double score = 40.0;

		if (matchedCategories.Count > 0)
		{
			var categorySignal = matchedCategories.Average(category => Math.Min(1.0, Math.Log10(category.ProductCount + 10) / 5.0));
			score += categorySignal * 20.0;
			notes.Add($"Matched {matchedCategories.Count} category feature groups.");
		}
		else
		{
			notes.Add("No direct category match found; using global feature priors.");
		}

		if (keywordSignals.Count > 0)
		{
			var keywordScore = keywordSignals.Take(8).Average(metric => metric.Score);
			score += keywordScore * 25.0;
			notes.Add($"Detected {keywordSignals.Count} high-intent keyword signals.");
		}
		else
		{
			notes.Add("Add 3-6 intent keywords for stronger ranking potential.");
		}

		if (!string.IsNullOrWhiteSpace(normalizedDefinition.PrimaryColor) &&
			_colors.TryGetValue(NormalizeToken(normalizedDefinition.PrimaryColor), out var colorMetric))
		{
			score += Math.Min(10.0, colorMetric.Score * 10.0);
			notes.Add($"Color signal found for '{colorMetric.Name}'.");
		}

		if (!string.IsNullOrWhiteSpace(normalizedDefinition.Material) &&
			_materials.TryGetValue(NormalizeToken(normalizedDefinition.Material), out var materialMetric))
		{
			score += Math.Min(10.0, materialMetric.Score * 10.0);
			notes.Add($"Material signal found for '{materialMetric.Name}'.");
		}

		// Price competitiveness signal: categories with many CHEAPER terms indicate an accessible price niche.
		var priceTermCount = matchedCategories.Sum(c => c.PriceTerms.Count(t => t.IsCheaper));
		if (priceTermCount > 0)
		{
			var priceBoost = Math.Min(5.0, Math.Log10(priceTermCount + 1) * 3.0);
			score += priceBoost;
			var samplePrices = matchedCategories
				.SelectMany(c => c.PriceTerms.Where(t => t.IsCheaper).Select(t => t.AvgPrice))
				.OrderBy(p => p)
				.ToList();
			if (samplePrices.Count > 0)
			{
				var medianPrice = samplePrices[samplePrices.Count / 2];
				notes.Add($"Price competitive data found: {priceTermCount} CHEAPER terms; median market entry ~${medianPrice:F2}.");
			}
		}

		score = Math.Clamp(score, 0.0, 100.0);

		var recommendedKeywords = keywordSignals
			.Take(8)
			.Select(metric => metric.Term)
			.Distinct(StringComparer.OrdinalIgnoreCase)
			.ToArray();

		if (recommendedKeywords.Length == 0)
		{
			recommendedKeywords = _keywords.Values
				.OrderByDescending(metric => metric.Score)
				.Take(8)
				.Select(metric => metric.Term)
				.ToArray();
		}

		var recommendedColors = SelectRecommendedColors(matchedCategories, 5);
		var recommendedMaterials = SelectRecommendedMaterials(matchedCategories, 5);
		var pairs = GetColorCategoryPairings(8, normalizedDefinition)
			.Select(pair => $"{pair.Category} + {pair.Color}")
			.ToArray();

		return new SellabilityInsight
		{
			Definition = normalizedDefinition,
			Score = score,
			ScoreBand = ScoreBand(score),
			RecommendedKeywords = recommendedKeywords,
			RecommendedColors = recommendedColors,
			RecommendedMaterials = recommendedMaterials,
			RecommendedCategoryColorPairs = pairs,
			Notes = notes.ToArray(),
		};
	}

	public IReadOnlyList<CategoryColorPairing> GetColorCategoryPairings(int limit, ProductDefinitionInput? definition = null)
	{
		var categories = definition is null
			? _categories.Values
			: FindMatchingCategories(definition);

		var pairings = new List<CategoryColorPairing>();
		foreach (var category in categories)
		{
			foreach (var color in category.Colors.OrderByDescending(metric => metric.Score).Take(8))
			{
				pairings.Add(new CategoryColorPairing
				{
					Category = category.Name,
					Color = color.Name,
					Score = color.Score,
					ProductCount = color.ProductCount,
					TotalSales = color.TotalSales,
				});
			}
		}

		if (pairings.Count == 0)
		{
			foreach (var category in _categories.Values.OrderByDescending(value => value.ProductCount).Take(12))
			{
				foreach (var color in _colors.Values.OrderByDescending(value => value.Score).Take(5))
				{
					pairings.Add(new CategoryColorPairing
					{
						Category = category.Name,
						Color = color.Name,
						Score = (category.ProductCount / 1000.0) * color.Score,
						ProductCount = color.ProductCount,
						TotalSales = color.TotalSales,
					});
				}
			}
		}

		return pairings
			.OrderByDescending(pair => pair.Score)
			.Take(Math.Max(1, limit))
			.ToArray();
	}

	public SeoHintBundle BuildSeoHints(ProductDefinitionInput? definition, int keywordCount)
	{
		var normalizedDefinition = definition is null ? null : NormalizeDefinition(definition);
		var matchedCategories = normalizedDefinition is null ? new List<CategorySnapshot>() : FindMatchingCategories(normalizedDefinition).ToList();

		var highIntent = new List<string>();
		foreach (var category in matchedCategories)
		{
			highIntent.AddRange(category.CtrTerms
				.OrderByDescending(term => term.Lift)
				.Take(8)
				.Select(term => term.Term));
		}

		highIntent.AddRange(_keywords.Values
			.OrderByDescending(metric => metric.Score)
			.Take(40)
			.Select(metric => metric.Term));

		highIntent = highIntent
			.Where(value => value.Length >= 3)
			.Distinct(StringComparer.OrdinalIgnoreCase)
			.Take(Math.Max(3, keywordCount))
			.ToList();

		var secondary = _keywords.Values
			.OrderByDescending(metric => metric.Confidence)
			.Select(metric => metric.Term)
			.Where(term => !highIntent.Contains(term, StringComparer.OrdinalIgnoreCase))
			.Take(Math.Max(3, keywordCount / 2))
			.ToArray();

		var angles = new List<string>
		{
			"Lead with buyer intent, then style descriptor, then practical use case.",
			"Keep the main keyword in the first 45 characters of the title.",
			"Use concrete benefits and avoid generic filler adjectives.",
		};

		if (normalizedDefinition is not null && !string.IsNullOrWhiteSpace(normalizedDefinition.Audience))
		{
			angles.Add($"Include audience cue '{normalizedDefinition.Audience}' in title or first sentence.");
		}

		if (normalizedDefinition is not null && !string.IsNullOrWhiteSpace(normalizedDefinition.UseCase))
		{
			angles.Add($"Call out use case '{normalizedDefinition.UseCase}' in the description intro.");
		}

		return new SeoHintBundle
		{
			HighIntentKeywords = highIntent.ToArray(),
			SecondaryKeywords = secondary,
			SuggestedAngles = angles.ToArray(),
		};
	}

	public ShopRecommendationBundle RecommendShops(ProductDefinitionInput? definition, IEnumerable<string> tags, string title)
	{
		var normalizedDefinition = definition is null ? null : NormalizeDefinition(definition);
		var evidenceTerms = new List<string>();
		evidenceTerms.AddRange(ExtractWords(title));
		evidenceTerms.AddRange(tags.SelectMany(ExtractWords));
		if (normalizedDefinition is not null)
		{
			evidenceTerms.AddRange(normalizedDefinition.Keywords.SelectMany(ExtractWords));
			evidenceTerms.AddRange(ExtractWords(normalizedDefinition.UseCase));
		}

		var etsyScore = 0.50;
		var ebayScore = 0.50;

		var etsySignals = new[] { "handmade", "artisan", "gift", "wedding", "boho", "vintage", "minimalist", "personalized" };
		var ebaySignals = new[] { "sport", "tactical", "outdoor", "tech", "gaming", "automotive", "fitness", "bundle" };

		foreach (var term in evidenceTerms)
		{
			if (etsySignals.Contains(term, StringComparer.OrdinalIgnoreCase))
			{
				etsyScore += 0.12;
			}

			if (ebaySignals.Contains(term, StringComparer.OrdinalIgnoreCase))
			{
				ebayScore += 0.12;
			}
		}

		lock (_historyLock)
		{
			ApplyHistoryBoost("Etsy", normalizedDefinition?.MainCategory, ref etsyScore);
			ApplyHistoryBoost("Ebay", normalizedDefinition?.MainCategory, ref ebayScore);
		}

		var recommendations = new List<ShopRecommendation>
		{
			new() { Store = "Etsy", Score = Math.Clamp(etsyScore, 0, 1), Reason = BuildStoreReason("Etsy", etsyScore, evidenceTerms) },
			new() { Store = "Ebay", Score = Math.Clamp(ebayScore, 0, 1), Reason = BuildStoreReason("Ebay", ebayScore, evidenceTerms) },
		}
		.OrderByDescending(item => item.Score)
		.ToList();

		return new ShopRecommendationBundle
		{
			Recommendations = recommendations.ToArray(),
			Notes = new[]
			{
				"Scores combine listing language signals with stored cross-shop placement history.",
				"As phase7 outcomes accumulate, shop preference weighting becomes more category-specific.",
			},
		};
	}

	public void RecordShopDecision(string store, ProductDefinitionInput? definition)
	{
		if (string.IsNullOrWhiteSpace(store))
		{
			return;
		}

		var category = NormalizeToken(definition?.MainCategory ?? string.Empty);
		var normalizedStore = NormalizeStoreName(store);

		lock (_historyLock)
		{
			if (!_history.Stores.TryGetValue(normalizedStore, out var stats))
			{
				stats = new StoreStats();
				_history.Stores[normalizedStore] = stats;
			}

			stats.TotalPlacements++;

			if (!string.IsNullOrWhiteSpace(category))
			{
				stats.CategoryPlacements.TryGetValue(category, out var categoryCount);
				stats.CategoryPlacements[category] = categoryCount + 1;
			}

			PersistHistory();
		}
	}

	public MarketPriceGuidance GetMarketPriceGuidance(ProductDefinitionInput? definition)
	{
		var normalizedDefinition = definition is null ? null : NormalizeDefinition(definition);
		var matchedCategories = normalizedDefinition is null
			? _categories.Values.OrderByDescending(c => c.ProductCount).Take(10).ToList()
			: FindMatchingCategories(normalizedDefinition).ToList();

		// Collect price terms from matched categories, falling back to global index
		var priceTerms = matchedCategories
			.SelectMany(cat => cat.PriceTerms)
			.Where(term => term.IsCheaper && term.AvgPrice > 0 && term.ProductCount >= 5)
			.ToList();

		if (priceTerms.Count == 0)
		{
			priceTerms = _priceTerms.Values
				.Where(agg => agg.IsCheaper && agg.AvgPrice > 0 && agg.TotalProducts >= 10)
				.Select(agg => new PriceTerm
				{
					Term = agg.Term,
					AvgPrice = agg.AvgPrice,
					Ratio = agg.AvgRatio,
					IsCheaper = agg.IsCheaper,
					ProductCount = agg.TotalProducts,
				})
				.ToList();
		}

		if (priceTerms.Count == 0)
		{
			return new MarketPriceGuidance
			{
				Notes = new[] { "No price competitiveness data available for this product definition." },
			};
		}

		var sortedPrices = priceTerms.Select(t => t.AvgPrice).OrderBy(p => p).ToList();
		var median = sortedPrices.Count % 2 == 0
			? (sortedPrices[sortedPrices.Count / 2 - 1] + sortedPrices[sortedPrices.Count / 2]) / 2.0
			: sortedPrices[sortedPrices.Count / 2];

		var competitiveTerms = priceTerms
			.OrderBy(t => t.Ratio)
			.Take(10)
			.Select(t => t.Term)
			.Distinct(StringComparer.OrdinalIgnoreCase)
			.ToArray();

		var notes = new List<string>
		{
			$"Derived from {priceTerms.Count} price-competitive terms across {matchedCategories.Count} matched category group(s).",
			$"Median competitive price: ${median:F2}. Listing near this range maximises category visibility.",
		};

		if (normalizedDefinition is not null && !string.IsNullOrWhiteSpace(normalizedDefinition.MainCategory))
		{
			notes.Add($"Category match: {normalizedDefinition.MainCategory}.");
		}

		return new MarketPriceGuidance
		{
			SuggestedMinPrice = sortedPrices.First(),
			SuggestedMaxPrice = sortedPrices.Last(),
			MedianCompetitivePrice = median,
			CompetitiveTerms = competitiveTerms,
			Notes = notes.ToArray(),
		};
	}

	private static CategorySnapshot? ParseCategoryFile(string path)
	{
		var lines = File.ReadAllLines(path);
		if (lines.Length == 0)
		{
			return null;
		}

		var header = HeaderRegex.Match(lines[0]);
		if (!header.Success)
		{
			return null;
		}

		var name = header.Groups["name"].Value.Trim();
		var type = header.Groups["type"].Value.Trim();
		var productCount = ParseInt(header.Groups["count"].Value);

		var snapshot = new CategorySnapshot(name, type, productCount);
		var section = string.Empty;

		for (var i = 1; i < lines.Length; i++)
		{
			var line = lines[i];
			if (line.StartsWith("--- ", StringComparison.Ordinal) && line.EndsWith(" ---", StringComparison.Ordinal))
			{
				section = line;
				continue;
			}

			if (string.IsNullOrWhiteSpace(section) || line.Contains("Found ", StringComparison.OrdinalIgnoreCase))
			{
				continue;
			}

			if (section.Contains("CTR INDICATORS", StringComparison.OrdinalIgnoreCase))
			{
				var ctr = CtrRegex.Match(line);
				if (ctr.Success)
				{
					snapshot.CtrTerms.Add(new CtrTerm
					{
						Term = NormalizeToken(ctr.Groups["term"].Value),
						Lift = ParseDouble(ctr.Groups["lift"].Value),
						TopCount = ParseInt(ctr.Groups["top"].Value),
						BottomCount = ParseInt(ctr.Groups["bottom"].Value),
					});
				}
				continue;
			}

			if (section.Contains("SENTIMENT", StringComparison.OrdinalIgnoreCase))
			{
				var sentiment = SentimentRegex.Match(line);
				if (sentiment.Success)
				{
					snapshot.SentimentTerms.Add(new SentimentTerm
					{
						Term = NormalizeToken(sentiment.Groups["term"].Value),
						PositivePct = ParseDouble(sentiment.Groups["pct"].Value),
						ProductCount = ParseInt(sentiment.Groups["count"].Value),
					});
				}
				continue;
			}

			if (section.Contains("COLOR ANALYSIS", StringComparison.OrdinalIgnoreCase))
			{
				var color = ColorOrMaterialRegex.Match(line);
				if (color.Success)
				{
					snapshot.Colors.Add(new ColorMetric
					{
						Name = NormalizeToken(color.Groups["name"].Value),
						ProductCount = ParseInt(color.Groups["count"].Value),
						Sentiment = ParseDouble(color.Groups["sent"].Value),
						TotalSales = ParseLong(color.Groups["sales"].Value),
					});
				}
				continue;
			}

			if (section.Contains("MATERIAL ANALYSIS", StringComparison.OrdinalIgnoreCase))
			{
				var material = ColorOrMaterialRegex.Match(line);
				if (material.Success)
				{
					snapshot.Materials.Add(new MaterialMetric
					{
						Name = NormalizeToken(material.Groups["name"].Value),
						ProductCount = ParseInt(material.Groups["count"].Value),
						Sentiment = ParseDouble(material.Groups["sent"].Value),
						TotalSales = ParseLong(material.Groups["sales"].Value),
					});
				}
			}

			if (section.Contains("PRICE COMPETITIVENESS", StringComparison.OrdinalIgnoreCase))
			{
				var priceTerm = PriceTermRegex.Match(line);
				if (priceTerm.Success)
				{
					snapshot.PriceTerms.Add(new PriceTerm
					{
						Term = NormalizeToken(priceTerm.Groups["term"].Value),
						AvgPrice = ParseDouble(priceTerm.Groups["price"].Value),
						Ratio = ParseDouble(priceTerm.Groups["ratio"].Value),
						IsCheaper = priceTerm.Groups["tag"].Value.Equals("CHEAPER", StringComparison.OrdinalIgnoreCase),
						ProductCount = ParseInt(priceTerm.Groups["count"].Value),
					});
				}
			}
		}

		foreach (var color in snapshot.Colors)
		{
			color.Score = MetricScore(color.ProductCount, color.TotalSales, color.Sentiment);
		}

		foreach (var material in snapshot.Materials)
		{
			material.Score = MetricScore(material.ProductCount, material.TotalSales, material.Sentiment);
		}

		return snapshot;
	}

	private static Dictionary<string, KeywordMetric> BuildKeywordIndex(IEnumerable<CategorySnapshot> snapshots)
	{
		var results = new Dictionary<string, KeywordMetric>(StringComparer.OrdinalIgnoreCase);
		foreach (var snapshot in snapshots)
		{
			foreach (var ctr in snapshot.CtrTerms)
			{
				if (!results.TryGetValue(ctr.Term, out var metric))
				{
					metric = new KeywordMetric { Term = ctr.Term };
					results[ctr.Term] = metric;
				}

				metric.AggregateLift += ctr.Lift;
				metric.SampleCount++;
				metric.TopHits += ctr.TopCount;
				metric.BottomHits += ctr.BottomCount;
			}

			foreach (var sentiment in snapshot.SentimentTerms)
			{
				if (!results.TryGetValue(sentiment.Term, out var metric))
				{
					metric = new KeywordMetric { Term = sentiment.Term };
					results[sentiment.Term] = metric;
				}

				metric.AggregateSentiment += sentiment.PositivePct;
				metric.SentimentSamples++;
			}
		}

		foreach (var metric in results.Values)
		{
			var avgLift = metric.SampleCount > 0 ? metric.AggregateLift / metric.SampleCount : 0;
			var avgSentiment = metric.SentimentSamples > 0 ? metric.AggregateSentiment / metric.SentimentSamples : 0;
			var liftScore = Math.Clamp(avgLift / 250.0, 0.0, 1.0);
			var sentimentScore = Math.Clamp(avgSentiment / 100.0, 0.0, 1.0);
			var confidence = Math.Clamp(Math.Log10(metric.TopHits + metric.BottomHits + 10) / 3.0, 0.0, 1.0);

			metric.Score = (liftScore * 0.55) + (sentimentScore * 0.35) + (confidence * 0.10);
			metric.Confidence = confidence;
		}

		return results;
	}

	private static Dictionary<string, ColorMetric> BuildColorIndex(IEnumerable<CategorySnapshot> snapshots)
	{
		var colors = new Dictionary<string, ColorMetric>(StringComparer.OrdinalIgnoreCase);
		foreach (var snapshot in snapshots)
		{
			foreach (var color in snapshot.Colors)
			{
				if (!colors.TryGetValue(color.Name, out var aggregate))
				{
					aggregate = new ColorMetric { Name = color.Name };
					colors[color.Name] = aggregate;
				}

				aggregate.ProductCount += color.ProductCount;
				aggregate.TotalSales += color.TotalSales;
				aggregate.Sentiment += color.Sentiment;
				aggregate.SampleCount++;
			}
		}

		foreach (var color in colors.Values)
		{
			var avgSentiment = color.SampleCount > 0 ? color.Sentiment / color.SampleCount : color.Sentiment;
			color.Sentiment = avgSentiment;
			color.Score = MetricScore(color.ProductCount, color.TotalSales, avgSentiment);
		}

		return colors;
	}

	private static Dictionary<string, MaterialMetric> BuildMaterialIndex(IEnumerable<CategorySnapshot> snapshots)
	{
		var materials = new Dictionary<string, MaterialMetric>(StringComparer.OrdinalIgnoreCase);
		foreach (var snapshot in snapshots)
		{
			foreach (var material in snapshot.Materials)
			{
				if (!materials.TryGetValue(material.Name, out var aggregate))
				{
					aggregate = new MaterialMetric { Name = material.Name };
					materials[material.Name] = aggregate;
				}

				aggregate.ProductCount += material.ProductCount;
				aggregate.TotalSales += material.TotalSales;
				aggregate.Sentiment += material.Sentiment;
				aggregate.SampleCount++;
			}
		}

		foreach (var material in materials.Values)
		{
			var avgSentiment = material.SampleCount > 0 ? material.Sentiment / material.SampleCount : material.Sentiment;
			material.Sentiment = avgSentiment;
			material.Score = MetricScore(material.ProductCount, material.TotalSales, avgSentiment);
		}

		return materials;
	}

	private static Dictionary<string, AggregatePriceTerm> BuildPriceTermIndex(IEnumerable<CategorySnapshot> snapshots)
	{
		var index = new Dictionary<string, AggregatePriceTerm>(StringComparer.OrdinalIgnoreCase);
		foreach (var snapshot in snapshots)
		{
			foreach (var term in snapshot.PriceTerms)
			{
				if (!index.TryGetValue(term.Term, out var aggregate))
				{
					aggregate = new AggregatePriceTerm { Term = term.Term };
					index[term.Term] = aggregate;
				}

				aggregate.TotalPrice += term.AvgPrice;
				aggregate.TotalRatio += term.Ratio;
				aggregate.SampleCount++;
				aggregate.TotalProducts += term.ProductCount;

				if (term.IsCheaper)
				{
					aggregate.CheaperCount++;
				}
			}
		}

		foreach (var agg in index.Values)
		{
			if (agg.SampleCount > 0)
			{
				agg.AvgPrice = agg.TotalPrice / agg.SampleCount;
				agg.AvgRatio = agg.TotalRatio / agg.SampleCount;
			}

			agg.IsCheaper = agg.CheaperCount > agg.SampleCount / 2;
		}

		return index;
	}

	private static StoreLearningHistory ReadHistory(string historyPath)
	{
		if (!File.Exists(historyPath))
		{
			return new StoreLearningHistory();
		}

		try
		{
			var raw = File.ReadAllText(historyPath);
			return JsonSerializer.Deserialize<StoreLearningHistory>(raw) ?? new StoreLearningHistory();
		}
		catch
		{
			return new StoreLearningHistory();
		}
	}

	private void PersistHistory()
	{
		var dir = Path.GetDirectoryName(_historyPath);
		if (!string.IsNullOrWhiteSpace(dir))
		{
			Directory.CreateDirectory(dir);
		}

		File.WriteAllText(_historyPath, JsonSerializer.Serialize(_history, new JsonSerializerOptions { WriteIndented = true }));
	}

	private IEnumerable<CategorySnapshot> FindMatchingCategories(ProductDefinitionInput definition)
	{
		var needles = new[]
		{
			definition.MainCategory,
			definition.SubCategory,
			definition.Audience,
			definition.UseCase,
		}
		.Where(value => !string.IsNullOrWhiteSpace(value))
		.Select(NormalizeToken)
		.Where(value => value.Length >= 3)
		.Distinct(StringComparer.OrdinalIgnoreCase)
		.ToArray();

		if (needles.Length == 0)
		{
			return _categories.Values.OrderByDescending(category => category.ProductCount).Take(15);
		}

		return _categories.Values
			.Where(category =>
			{
				var normalizedName = NormalizeToken(category.Name);
				return needles.Any(needle => normalizedName.Contains(needle, StringComparison.OrdinalIgnoreCase)
					|| needle.Contains(normalizedName, StringComparison.OrdinalIgnoreCase));
			})
			.OrderByDescending(category => category.ProductCount)
			.Take(15);
	}

	private static ProductDefinitionInput NormalizeDefinition(ProductDefinitionInput definition)
	{
		var normalizedKeywords = definition.Keywords
			.Where(keyword => !string.IsNullOrWhiteSpace(keyword))
			.Select(NormalizeToken)
			.Where(keyword => keyword.Length >= 3)
			.Distinct(StringComparer.OrdinalIgnoreCase)
			.Take(12)
			.ToArray();

		return definition with
		{
			Title = definition.Title.Trim(),
			Description = definition.Description.Trim(),
			MainCategory = NormalizeTitle(definition.MainCategory),
			SubCategory = NormalizeTitle(definition.SubCategory),
			Audience = NormalizeTitle(definition.Audience),
			PrimaryColor = NormalizeToken(definition.PrimaryColor),
			Material = NormalizeToken(definition.Material),
			UseCase = NormalizeTitle(definition.UseCase),
			Keywords = normalizedKeywords,
		};
	}

	private static IEnumerable<string> ExtractCandidateKeywords(ProductDefinitionInput definition)
	{
		foreach (var word in ExtractWords(definition.Title))
		{
			yield return word;
		}

		foreach (var word in ExtractWords(definition.Description))
		{
			yield return word;
		}

		foreach (var keyword in definition.Keywords)
		{
			yield return NormalizeToken(keyword);
		}
	}

	private static IEnumerable<string> ExtractWords(string text)
	{
		if (string.IsNullOrWhiteSpace(text))
		{
			yield break;
		}

		foreach (Match match in WordRegex.Matches(text.ToLowerInvariant()))
		{
			if (match.Success && match.Value.Length >= 3)
			{
				yield return match.Value;
			}
		}
	}

	private string[] SelectRecommendedColors(IEnumerable<CategorySnapshot> matchedCategories, int limit)
	{
		var preferred = matchedCategories
			.SelectMany(category => category.Colors)
			.OrderByDescending(color => color.Score)
			.Select(color => color.Name)
			.Distinct(StringComparer.OrdinalIgnoreCase)
			.Take(limit)
			.ToList();

		if (preferred.Count >= limit)
		{
			return preferred.ToArray();
		}

		preferred.AddRange(_colors.Values
			.OrderByDescending(color => color.Score)
			.Select(color => color.Name)
			.Where(color => !preferred.Contains(color, StringComparer.OrdinalIgnoreCase))
			.Take(limit - preferred.Count));

		return preferred.ToArray();
	}

	private string[] SelectRecommendedMaterials(IEnumerable<CategorySnapshot> matchedCategories, int limit)
	{
		var preferred = matchedCategories
			.SelectMany(category => category.Materials)
			.OrderByDescending(material => material.Score)
			.Select(material => material.Name)
			.Distinct(StringComparer.OrdinalIgnoreCase)
			.Take(limit)
			.ToList();

		if (preferred.Count >= limit)
		{
			return preferred.ToArray();
		}

		preferred.AddRange(_materials.Values
			.OrderByDescending(material => material.Score)
			.Select(material => material.Name)
			.Where(material => !preferred.Contains(material, StringComparer.OrdinalIgnoreCase))
			.Take(limit - preferred.Count));

		return preferred.ToArray();
	}

	private void ApplyHistoryBoost(string store, string? mainCategory, ref double score)
	{
		if (!_history.Stores.TryGetValue(store, out var stats) || stats.TotalPlacements <= 0)
		{
			return;
		}

		var normalizedCategory = NormalizeToken(mainCategory ?? string.Empty);
		var categoryPlacements = 0;
		if (!string.IsNullOrWhiteSpace(normalizedCategory))
		{
			stats.CategoryPlacements.TryGetValue(normalizedCategory, out categoryPlacements);
		}

		var globalBoost = Math.Clamp(Math.Log10(stats.TotalPlacements + 1) / 15.0, 0.0, 0.15);
		var categoryBoost = categoryPlacements > 0
			? Math.Clamp(Math.Log10(categoryPlacements + 1) / 10.0, 0.0, 0.10)
			: 0;

		score += globalBoost + categoryBoost;
	}

	private static string BuildStoreReason(string store, double score, IReadOnlyList<string> evidenceTerms)
	{
		var confidence = score >= 0.75 ? "high" : score >= 0.60 ? "medium" : "light";
		var signal = evidenceTerms
			.GroupBy(term => term)
			.OrderByDescending(group => group.Count())
			.Take(3)
			.Select(group => group.Key)
			.ToArray();

		if (signal.Length == 0)
		{
			return $"{confidence} confidence placement based on historical {store} performance.";
		}

		return $"{confidence} confidence from language fit ({string.Join(", ", signal)}).";
	}

	private static string NormalizeStoreName(string store)
	{
		if (store.Equals("etsy", StringComparison.OrdinalIgnoreCase))
		{
			return "Etsy";
		}

		if (store.Equals("ebay", StringComparison.OrdinalIgnoreCase))
		{
			return "Ebay";
		}

		return CultureInfo.InvariantCulture.TextInfo.ToTitleCase(store.Trim().ToLowerInvariant());
	}

	private static string ScoreBand(double score)
	{
		if (score >= 80)
		{
			return "High";
		}

		if (score >= 60)
		{
			return "Medium";
		}

		return "Low";
	}

	private static double MetricScore(int productCount, long totalSales, double sentiment)
	{
		var countScore = Math.Clamp(Math.Log10(productCount + 10) / 4.0, 0.0, 1.0);
		var salesScore = Math.Clamp(Math.Log10(totalSales + 10) / 7.0, 0.0, 1.0);
		var sentimentScore = Math.Clamp(sentiment / 5.0, 0.0, 1.0);
		return (countScore * 0.25) + (salesScore * 0.45) + (sentimentScore * 0.30);
	}

	private static string InferAudience(string categoryName)
	{
		var normalized = NormalizeToken(categoryName);
		if (normalized.Contains("women", StringComparison.OrdinalIgnoreCase)) return "women";
		if (normalized.Contains("men", StringComparison.OrdinalIgnoreCase)) return "men";
		if (normalized.Contains("kids", StringComparison.OrdinalIgnoreCase)) return "kids";
		return "unisex";
	}

	private static double ScoreCategoryMatch(CategorySnapshot category, string[] terms, string normalizedBlueprint)
	{
		var normalizedCategoryName = NormalizeToken(category.Name);
		var overlap = terms.Count(term => normalizedCategoryName.Contains(term, StringComparison.OrdinalIgnoreCase));
		if (overlap == 0 && !string.IsNullOrWhiteSpace(normalizedBlueprint))
		{
			overlap = normalizedCategoryName.Contains(NormalizeToken(normalizedBlueprint), StringComparison.OrdinalIgnoreCase) ? 1 : 0;
		}

		if (overlap == 0)
		{
			return 0;
		}

		var typeWeight = category.Type switch
		{
			"MainCategory" => 1.15,
			"SubCategory" => 1.20,
			"Gender" => 1.05,
			"MaterialBased" => 0.90,
			"ColorBased" => 0.90,
			_ => 1.0,
		};

		var supportWeight = Math.Log10(category.ProductCount + 10);
		return overlap * typeWeight * supportWeight;
	}

	private string PickBlueprintColor(IEnumerable<string> terms, IEnumerable<CategorySnapshot> matchedCategories)
	{
		foreach (var term in terms)
		{
			if (_colors.ContainsKey(term))
			{
				return term;
			}
		}

		var matchedColor = matchedCategories
			.SelectMany(category => category.Colors)
			.OrderByDescending(color => color.Score)
			.Select(color => color.Name)
			.FirstOrDefault();

		if (!string.IsNullOrWhiteSpace(matchedColor))
		{
			return matchedColor;
		}

		return _colors.Values
			.OrderByDescending(color => color.Score)
			.Select(color => color.Name)
			.FirstOrDefault() ?? string.Empty;
	}

	private string PickBlueprintMaterial(IEnumerable<string> terms, IEnumerable<CategorySnapshot> matchedCategories)
	{
		foreach (var term in terms)
		{
			if (_materials.ContainsKey(term))
			{
				return term;
			}
		}

		var matchedMaterial = matchedCategories
			.SelectMany(category => category.Materials)
			.OrderByDescending(material => material.Score)
			.Select(material => material.Name)
			.FirstOrDefault();

		if (!string.IsNullOrWhiteSpace(matchedMaterial))
		{
			return matchedMaterial;
		}

		return _materials.Values
			.OrderByDescending(material => material.Score)
			.Select(material => material.Name)
			.FirstOrDefault() ?? string.Empty;
	}

	private static string InferUseCaseFromBlueprint(string blueprintName, IEnumerable<string> terms, string mainCategory, string subCategory)
	{
		var allTerms = terms
			.Concat(ExtractWords(blueprintName))
			.Select(NormalizeToken)
			.Where(value => value.Length >= 3)
			.ToArray();

		if (allTerms.Any(term => term.Contains("gift", StringComparison.OrdinalIgnoreCase)
			|| term.Contains("birthday", StringComparison.OrdinalIgnoreCase)
			|| term.Contains("holiday", StringComparison.OrdinalIgnoreCase)))
		{
			return "gift-ready seasonal listing";
		}

		if (allTerms.Any(term => term.Contains("hiking", StringComparison.OrdinalIgnoreCase)
			|| term.Contains("outdoor", StringComparison.OrdinalIgnoreCase)
			|| term.Contains("trail", StringComparison.OrdinalIgnoreCase)
			|| term.Contains("camp", StringComparison.OrdinalIgnoreCase)))
		{
			return "outdoor activity and travel";
		}

		if (allTerms.Any(term => term.Contains("gym", StringComparison.OrdinalIgnoreCase)
			|| term.Contains("fitness", StringComparison.OrdinalIgnoreCase)
			|| term.Contains("workout", StringComparison.OrdinalIgnoreCase)
			|| term.Contains("running", StringComparison.OrdinalIgnoreCase)))
		{
			return "fitness and active lifestyle";
		}

		if (!string.IsNullOrWhiteSpace(subCategory))
		{
			return $"everyday {NormalizeToken(subCategory)} styling";
		}

		if (!string.IsNullOrWhiteSpace(mainCategory))
		{
			return $"everyday {NormalizeToken(mainCategory)} usage";
		}

		return "everyday gifting and self-purchase";
	}

	private static string NormalizeTitle(string value)
	{
		if (string.IsNullOrWhiteSpace(value))
		{
			return string.Empty;
		}

		return CultureInfo.InvariantCulture.TextInfo.ToTitleCase(NormalizeToken(value));
	}

	private static string NormalizeToken(string value)
	{
		if (string.IsNullOrWhiteSpace(value))
		{
			return string.Empty;
		}

		var token = value.Trim().ToLowerInvariant();
		token = token.Replace("_", " ", StringComparison.Ordinal);
		token = Regex.Replace(token, "\\s+", " ");
		return token.Trim();
	}

	private static int ParseInt(string value)
		=> int.TryParse(value.Replace(",", string.Empty, StringComparison.Ordinal), NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed)
			? parsed
			: 0;

	private static long ParseLong(string value)
		=> long.TryParse(value.Replace(",", string.Empty, StringComparison.Ordinal), NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed)
			? parsed
			: 0;

	private static double ParseDouble(string value)
		=> double.TryParse(value.Replace(",", string.Empty, StringComparison.Ordinal), NumberStyles.Float, CultureInfo.InvariantCulture, out var parsed)
			? parsed
			: 0;

	private sealed class CategorySnapshot
	{
		public CategorySnapshot(string name, string type, int productCount)
		{
			Name = name;
			Type = type;
			ProductCount = productCount;
		}

		public string Name { get; }
		public string Type { get; }
		public int ProductCount { get; }
		public List<CtrTerm> CtrTerms { get; } = new();
		public List<SentimentTerm> SentimentTerms { get; } = new();
		public List<ColorMetric> Colors { get; } = new();
		public List<MaterialMetric> Materials { get; } = new();
		public List<PriceTerm> PriceTerms { get; } = new();
	}

	private sealed class PriceTerm
	{
		public string Term { get; init; } = string.Empty;
		public double AvgPrice { get; init; }
		public double Ratio { get; init; }
		public bool IsCheaper { get; init; }
		public int ProductCount { get; init; }
	}

	private sealed class AggregatePriceTerm
	{
		public string Term { get; init; } = string.Empty;
		public double TotalPrice { get; set; }
		public double TotalRatio { get; set; }
		public int SampleCount { get; set; }
		public int TotalProducts { get; set; }
		public int CheaperCount { get; set; }
		public double AvgPrice { get; set; }
		public double AvgRatio { get; set; }
		public bool IsCheaper { get; set; }
	}

	private sealed class SentimentTerm
	{
		public string Term { get; init; } = string.Empty;
		public double PositivePct { get; init; }
		public int ProductCount { get; init; }
	}

	private sealed class CtrTerm
	{
		public string Term { get; init; } = string.Empty;
		public double Lift { get; init; }
		public int TopCount { get; init; }
		public int BottomCount { get; init; }
	}

	private sealed class KeywordMetric
	{
		public string Term { get; init; } = string.Empty;
		public double AggregateLift { get; set; }
		public double AggregateSentiment { get; set; }
		public int SampleCount { get; set; }
		public int SentimentSamples { get; set; }
		public int TopHits { get; set; }
		public int BottomHits { get; set; }
		public double Confidence { get; set; }
		public double Score { get; set; }
	}

	private sealed class ColorMetric
	{
		public string Name { get; init; } = string.Empty;
		public int ProductCount { get; set; }
		public long TotalSales { get; set; }
		public double Sentiment { get; set; }
		public int SampleCount { get; set; }
		public double Score { get; set; }
	}

	private sealed class MaterialMetric
	{
		public string Name { get; init; } = string.Empty;
		public int ProductCount { get; set; }
		public long TotalSales { get; set; }
		public double Sentiment { get; set; }
		public int SampleCount { get; set; }
		public double Score { get; set; }
	}

	private sealed class StoreLearningHistory
	{
		public Dictionary<string, StoreStats> Stores { get; set; } = new(StringComparer.OrdinalIgnoreCase);
	}

	private sealed class StoreStats
	{
		public int TotalPlacements { get; set; }
		public Dictionary<string, int> CategoryPlacements { get; set; } = new(StringComparer.OrdinalIgnoreCase);
	}
}