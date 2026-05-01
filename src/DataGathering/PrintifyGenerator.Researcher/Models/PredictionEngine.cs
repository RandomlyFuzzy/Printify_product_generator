using System;
using System.Collections.Generic;
using System.Linq;

namespace PrintifyGenerator.Researcher.Models
{
    public class PredictionScore
    {
        public string Asin { get; set; } = "";
        public string Title { get; set; } = "";
        public string Category { get; set; } = "";
        public double OverallScore { get; set; }
        public double SentimentPrediction { get; set; }
        public double SalesPrediction { get; set; }
        public double CtrPrediction { get; set; }
        public double PriceCompetitivenessScore { get; set; }
        public string TopPositiveWords { get; set; } = "";
        public string TopNegativeWords { get; set; } = "";
        public string RecommendedColors { get; set; } = "";
        public string RecommendedMaterials { get; set; } = "";
        public string RecommendedPhrases { get; set; } = "";
        public List<string> Warnings { get; set; } = new();
    }

    public class ProductDefinition
    {
        public string Title { get; set; } = "";
        public string Description { get; set; } = "";
        public string Category { get; set; } = "";
        public string SubCategory { get; set; } = "";
        public string Audience { get; set; } = "";
        public string PrimaryColor { get; set; } = "";
        public string Material { get; set; } = "";
        public string UseCase { get; set; } = "";
        public List<string> Keywords { get; set; } = new();
    }

    public class PredictionEngine
    {
        private readonly Dictionary<string, WordStatProfile> _wordProfiles = new(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, PhraseStatProfile> _phraseProfiles = new(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, CategoryBenchmarks> _categoryBenchmarks = new(StringComparer.OrdinalIgnoreCase);
        private Dictionary<string, (int Count, double AvgSentiment, long TotalSales)> _colorStats = new(StringComparer.OrdinalIgnoreCase);
        private Dictionary<string, (int Count, double AvgSentiment, long TotalSales, decimal AvgPrice)> _materialStats = new(StringComparer.OrdinalIgnoreCase);
        private Dictionary<string, (int Count, double AvgSentiment, long TotalSales, int TopCount, int BottomCount)> _brandStats = new(StringComparer.OrdinalIgnoreCase);

        public void Train(
            Dictionary<string, SentimentStatResult> sentimentResults,
            Dictionary<string, SalesStatResult> salesResults,
            Dictionary<string, CtrStatResult> ctrResults,
            Dictionary<string, PriceStatResult> priceResults,
            Dictionary<string, ImageStatResult> imageResults,
            Dictionary<string, SeasonalityStatResult> seasonalityResults,
            Dictionary<string, ColorStatResult> colorResults,
            Dictionary<string, MaterialStatResult> materialResults,
            Dictionary<string, BrandStatResult> brandResults,
            Dictionary<string, List<(string asin, int sales)>> categoryProducts)
        {
            Console.WriteLine("Training prediction engine...");

            // Get the first (only) category's results
            var sentimentResult = sentimentResults.Values.FirstOrDefault() ?? new SentimentStatResult();
            var salesResult = salesResults.Values.FirstOrDefault() ?? new SalesStatResult();
            var ctrResult = ctrResults.Values.FirstOrDefault() ?? new CtrStatResult();
            var priceResult = priceResults.Values.FirstOrDefault() ?? new PriceStatResult();
            var imageResult = imageResults.Values.FirstOrDefault() ?? new ImageStatResult();

            // Build word profiles
            foreach (var (word, sentiment) in sentimentResult.WordSentiment)
            {
                var profile = GetOrCreateWordProfile(word);
                profile.SentimentPct = sentiment.Total > 0 ? (double)sentiment.Positive / sentiment.Total : 0;
                profile.SentimentCount = sentiment.Total;
            }

            foreach (var (word, sales) in salesResult.WordSales)
            {
                var profile = GetOrCreateWordProfile(word);
                profile.AvgSales = sales.Count > 0 ? (double)sales.Sales / sales.Count : 0;
                profile.SalesCount = sales.Count;
            }

            foreach (var (word, ctr) in ctrResult.WordCtr)
            {
                var profile = GetOrCreateWordProfile(word);
                profile.CtrViews = ctr.Views;
                profile.CtrCarts = ctr.Carts;
                profile.CtrPurchases = ctr.Purchases;
                profile.CtrProductCount = ctr.ProductCount;
            }

            foreach (var (word, price) in priceResult.WordPriceCompetitiveness)
            {
                var profile = GetOrCreateWordProfile(word);
                profile.AvgPrice = price.AvgPrice;
                profile.PriceVsCategory = price.PriceVsCategoryAvg;
            }

            foreach (var (word, images) in imageResult.WordImages)
            {
                var profile = GetOrCreateWordProfile(word);
                profile.AvgImages = images.TotalImages > 0 ? (double)images.TotalImages / images.ProductCount : 0;
            }

            // Build phrase profiles
            foreach (var (phrase, sentiment) in sentimentResult.PhraseSentiment)
            {
                var profile = GetOrCreatePhraseProfile(phrase);
                profile.SentimentPct = sentiment.Total > 0 ? (double)sentiment.Positive / sentiment.Total : 0;
                profile.SentimentCount = sentiment.Total;
            }

            foreach (var (phrase, sales) in salesResult.PhraseSales)
            {
                var profile = GetOrCreatePhraseProfile(phrase);
                profile.AvgSales = sales.Count > 0 ? (double)sales.Sales / sales.Count : 0;
                profile.SalesCount = sales.Count;
            }

            // Store color/material/brand stats
            _colorStats = colorResults?.Values.FirstOrDefault()?.ColorStats?.ToDictionary(k => k.Key, v => (v.Value.Count, v.Value.AvgSentiment, v.Value.TotalSales)) ?? new();
            _materialStats = materialResults?.Values.FirstOrDefault()?.MaterialStats?.ToDictionary(k => k.Key, v => (v.Value.Count, v.Value.AvgSentiment, v.Value.TotalSales, v.Value.AvgPrice)) ?? new();
            _brandStats = brandResults?.Values.FirstOrDefault()?.BrandStats?.ToDictionary(k => k.Key, v => (v.Value.Count, v.Value.AvgSentiment, v.Value.TotalSales, v.Value.TopCount, v.Value.BottomCount)) ?? new();

            // Build category benchmarks
            foreach (var kv in categoryProducts)
            {
                var category = kv.Key;
                var products = kv.Value;
                var benchmark = new CategoryBenchmarks
                {
                    Category = category,
                    AvgSales = products.Count > 0 ? products.Average(p => (double)p.sales) : 0,
                    MedianSales = products.Count > 0 ? GetMedian(products.Select(p => (double)p.sales).OrderBy(x => x).ToList()) : 0,
                    TotalProducts = products.Count
                };
                _categoryBenchmarks[category] = benchmark;
            }

            Console.WriteLine($"Trained on {_wordProfiles.Count} words, {_phraseProfiles.Count} phrases, {_categoryBenchmarks.Count} categories");
        }

        public PredictionScore Predict(string title, string? description = null, string? category = null)
        {
            var result = new PredictionScore { Title = title, Category = category ?? "Unknown" };

            if (string.IsNullOrEmpty(title)) return result;

            var words = ExtractWords(title + " " + (description ?? ""));
            var phrases = ExtractPhrases(title, words);

            // Score words
            double sentimentScore = 0, salesScore = 0, ctrScore = 0;
            int wordCount = 0;
            var positiveWords = new List<(string word, double score)>();
            var negativeWords = new List<(string word, double score)>();

            foreach (var word in words.Distinct())
            {
                if (!_wordProfiles.TryGetValue(word, out var profile)) continue;
                if (profile.SentimentCount < 10) continue; // Skip low-confidence words

                wordCount++;
                sentimentScore += profile.SentimentPct / 100.0;
                salesScore += NormalizeLog(profile.AvgSales, 10000);
                ctrScore += profile.CtrViews > 0 ? (double)profile.CtrCarts / profile.CtrViews : 0;

                var wordScore = (profile.SentimentPct / 100.0) * 0.4 +
                              NormalizeLog(profile.AvgSales, 10000) * 0.4 +
                              (profile.CtrViews > 0 ? (double)profile.CtrCarts / profile.CtrViews : 0) * 0.2;

                if (wordScore > 0.6)
                    positiveWords.Add((word, wordScore));
                else if (wordScore < 0.3)
                    negativeWords.Add((word, wordScore));
            }

            // Score phrases
            double phraseScore = 0;
            int phraseCount = 0;
            var goodPhrases = new List<string>();

            foreach (var phrase in phrases.Distinct())
            {
                if (!_phraseProfiles.TryGetValue(phrase, out var profile)) continue;
                if (profile.SentimentCount < 5) continue;

                phraseCount++;
                phraseScore += (profile.SentimentPct / 100.0) * 0.5 + NormalizeLog(profile.AvgSales, 10000) * 0.5;

                if (profile.AvgSales > 5000 && profile.SentimentPct > 70)
                    goodPhrases.Add(phrase);
            }

            // Calculate overall score
            result.SentimentPrediction = wordCount > 0 ? sentimentScore / wordCount : 0;
            result.SalesPrediction = wordCount > 0 ? salesScore / wordCount : 0;
            result.CtrPrediction = wordCount > 0 ? ctrScore / wordCount : 0;

            result.OverallScore = (result.SentimentPrediction * 0.30) +
                                 (result.SalesPrediction * 0.25) +
                                 (result.CtrPrediction * 0.20) +
                                 (wordCount > 0 ? (double)wordCount / 20.0 * 0.15 : 0) + // Feature richness
                                 (phraseCount > 0 ? Math.Min(1.0, (double)phraseCount / 10.0) * 0.10 : 0);

            // Get top recommendations
            result.TopPositiveWords = string.Join(", ", positiveWords.OrderByDescending(w => w.score).Take(5).Select(w => w.word));
            result.TopNegativeWords = string.Join(", ", negativeWords.OrderBy(w => w.score).Take(5).Select(w => w.word));
            result.RecommendedPhrases = string.Join("; ", goodPhrases.Take(5));

            // Recommend colors/materials from stats
            var topColors = _colorStats.Where(k => k.Value.Count >= 10)
                .OrderByDescending(k => k.Value.AvgSentiment)
                .Take(3)
                .Select(k => k.Key);
            result.RecommendedColors = string.Join(", ", topColors);

            var topMaterials = _materialStats.Where(k => k.Value.Count >= 10)
                .OrderByDescending(k => k.Value.AvgSentiment)
                .Take(3)
                .Select(k => k.Key);
            result.RecommendedMaterials = string.Join(", ", topMaterials);

            // Warnings
            if (result.SentimentPrediction < 0.5)
                result.Warnings.Add("Low sentiment prediction - consider different words");
            if (result.SalesPrediction < 0.3)
                result.Warnings.Add("Low sales prediction - words may not drive sales");
            if (wordCount < 3)
                result.Warnings.Add("Few recognizable words - add more descriptive terms");

            return result;
        }

        public PredictionScore Predict(ProductDefinition definition)
        {
            if (definition is null)
            {
                return new PredictionScore();
            }

            var title = string.IsNullOrWhiteSpace(definition.Title)
                ? string.Join(" ", definition.Keywords.Take(4))
                : definition.Title;

            var contextParts = new List<string>
            {
                definition.Description ?? "",
                definition.SubCategory ?? "",
                definition.Audience ?? "",
                definition.UseCase ?? "",
                string.Join(" ", definition.Keywords ?? new List<string>()),
            };

            var context = string.Join(" ", contextParts.Where(p => !string.IsNullOrWhiteSpace(p)));
            var category = string.IsNullOrWhiteSpace(definition.Category) ? "Unknown" : definition.Category;

            var result = Predict(title, context, category);

            if (!string.IsNullOrWhiteSpace(definition.PrimaryColor)
                && _colorStats.TryGetValue(definition.PrimaryColor, out var color))
            {
                var colorBoost = Math.Clamp((color.AvgSentiment / 5.0) * 0.08, 0, 0.08);
                result.OverallScore = Math.Clamp(result.OverallScore + colorBoost, 0, 1);
                result.RecommendedColors = string.IsNullOrWhiteSpace(result.RecommendedColors)
                    ? definition.PrimaryColor
                    : result.RecommendedColors;
            }

            if (!string.IsNullOrWhiteSpace(definition.Material)
                && _materialStats.TryGetValue(definition.Material, out var material))
            {
                var materialBoost = Math.Clamp((material.AvgSentiment / 5.0) * 0.08, 0, 0.08);
                result.OverallScore = Math.Clamp(result.OverallScore + materialBoost, 0, 1);
                result.RecommendedMaterials = string.IsNullOrWhiteSpace(result.RecommendedMaterials)
                    ? definition.Material
                    : result.RecommendedMaterials;
            }

            if (definition.Keywords is { Count: > 0 })
            {
                var matched = definition.Keywords
                    .Select(keyword => keyword?.Trim().ToLowerInvariant())
                    .Where(keyword => !string.IsNullOrWhiteSpace(keyword))
                    .Distinct()
                    .Count(keyword => _wordProfiles.ContainsKey(keyword!));

                var keywordBoost = Math.Clamp(matched / 10.0 * 0.10, 0, 0.10);
                result.OverallScore = Math.Clamp(result.OverallScore + keywordBoost, 0, 1);

                if (matched < 2)
                {
                    result.Warnings.Add("Few definition keywords matched known high-signal words");
                }
            }

            return result;
        }

        private WordStatProfile GetOrCreateWordProfile(string word)
        {
            if (!_wordProfiles.TryGetValue(word, out var profile))
                _wordProfiles[word] = profile = new WordStatProfile();
            return profile;
        }

        private PhraseStatProfile GetOrCreatePhraseProfile(string phrase)
        {
            if (!_phraseProfiles.TryGetValue(phrase, out var profile))
                _phraseProfiles[phrase] = profile = new PhraseStatProfile();
            return profile;
        }

        private static HashSet<string> ExtractWords(string text)
        {
            if (string.IsNullOrEmpty(text)) return new();
            return text.ToLower()
                .Split(new[] { ' ', '\t', '-', '_', ',', '.', ';', ':', '(', ')', '[', ']', '"', '\'' },
                       StringSplitOptions.RemoveEmptyEntries)
                .Where(w => w.Length > 2 && !int.TryParse(w, out _))
                .ToHashSet();
        }

        private static HashSet<string> ExtractPhrases(string title, HashSet<string> words)
        {
            var phrases = new HashSet<string>();
            var wordList = words.ToList();
            for (int len = 2; len <= Math.Min(4, wordList.Count); len++)
            {
                for (int i = 0; i <= wordList.Count - len; i++)
                {
                    var phrase = string.Join(" ", wordList.Skip(i).Take(len));
                    if (phrase.Split(' ').Length == len)
                        phrases.Add(phrase);
                }
            }
            return phrases;
        }

        private static double NormalizeLog(double value, double max)
        {
            if (value <= 0) return 0;
            return Math.Min(1.0, Math.Log(value + 1) / Math.Log(max + 1));
        }

        private static double GetMedian(List<double> sortedValues)
        {
            if (sortedValues.Count == 0) return 0;
            int mid = sortedValues.Count / 2;
            return sortedValues.Count % 2 == 0
                ? (sortedValues[mid - 1] + sortedValues[mid]) / 2.0
                : sortedValues[mid];
        }
    }

    public class WordStatProfile
    {
        public double SentimentPct { get; set; }
        public int SentimentCount { get; set; }
        public double AvgSales { get; set; }
        public int SalesCount { get; set; }
        public long CtrViews { get; set; }
        public long CtrCarts { get; set; }
        public long CtrPurchases { get; set; }
        public int CtrProductCount { get; set; }
        public decimal AvgPrice { get; set; }
        public decimal PriceVsCategory { get; set; }
        public double AvgImages { get; set; }
    }

    public class PhraseStatProfile
    {
        public double SentimentPct { get; set; }
        public int SentimentCount { get; set; }
        public double AvgSales { get; set; }
        public int SalesCount { get; set; }
    }

    public class CategoryBenchmarks
    {
        public string Category { get; set; } = "";
        public double AvgSales { get; set; }
        public double MedianSales { get; set; }
        public int TotalProducts { get; set; }
    }
}
