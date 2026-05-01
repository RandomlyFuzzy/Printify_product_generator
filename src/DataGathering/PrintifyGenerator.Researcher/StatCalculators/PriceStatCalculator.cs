using System;
using System.Collections.Generic;
using System.Linq;
using PrintifyGenerator.Researcher.Interfaces;
using PrintifyGenerator.Researcher.Models;

namespace PrintifyGenerator.Researcher.StatCalculators
{
    public class PriceStatCalculator : IStatCalculator
    {
        public string Name => "Price";
        public ISet<string> RequiredDatasetTypes => new HashSet<string>();

        private readonly Dictionary<string, PriceStatResult> _categoryResults = new();
        private readonly Dictionary<string, (decimal TotalPrice, int Count)> _categoryPriceStats = new();

        public void InitializeForCategory(string category)
        {
            _categoryResults[category] = new PriceStatResult();
        }

        public void ProcessWord(string word, ProductContext context)
        {
            if (!_categoryResults.TryGetValue(context.Category, out var result)) return;
            if (context.Price <= 0) return;

            var priceResult = result.WordPrice;
            if (!priceResult.TryGetValue(word, out var entry))
                priceResult[word] = (context.Price, 1, context.Price, context.Price);
            else
                priceResult[word] = (entry.TotalPrice + context.Price, entry.Count + 1, 
                    Math.Min(entry.MinPrice, context.Price), Math.Max(entry.MaxPrice, context.Price));

            if (!_categoryPriceStats.TryGetValue(context.Category, out var catPrice))
                _categoryPriceStats[context.Category] = (context.Price, 1);
            else
                _categoryPriceStats[context.Category] = (catPrice.TotalPrice + context.Price, catPrice.Count + 1);
        }

        public void ProcessPhrase(string phrase, ProductContext context)
        {
            if (!_categoryResults.TryGetValue(context.Category, out var result)) return;
            if (context.Price <= 0) return;

            var priceResult = result.PhrasePrice;
            if (!priceResult.TryGetValue(phrase, out var entry))
                priceResult[phrase] = (context.Price, 1, context.Price, context.Price);
            else
                priceResult[phrase] = (entry.TotalPrice + context.Price, entry.Count + 1, 
                    Math.Min(entry.MinPrice, context.Price), Math.Max(entry.MaxPrice, context.Price));
        }

        public void CalculateCompetitiveness()
        {
            foreach (var kvp in _categoryResults)
            {
                var category = kvp.Key;
                var stats = kvp.Value;

                if (!_categoryPriceStats.TryGetValue(category, out var catPrice) || catPrice.Count == 0)
                    continue;

                var avgCategoryPrice = catPrice.TotalPrice / catPrice.Count;

                foreach (var word in stats.WordPrice.Keys.ToList())
                {
                    var wp = stats.WordPrice[word];
                    var avgPrice = wp.TotalPrice / wp.Count;
                    var priceRatio = avgCategoryPrice > 0 ? avgPrice / avgCategoryPrice : 1.0m;
                    stats.WordPriceCompetitiveness[word] = (avgPrice, priceRatio, wp.Count);
                }

                foreach (var phrase in stats.PhrasePrice.Keys.ToList())
                {
                    var pp = stats.PhrasePrice[phrase];
                    var avgPrice = pp.TotalPrice / pp.Count;
                    var priceRatio = avgCategoryPrice > 0 ? avgPrice / avgCategoryPrice : 1.0m;
                    stats.PhrasePriceCompetitiveness[phrase] = (avgPrice, priceRatio, pp.Count);
                }
            }
        }

        public object GetResults() => _categoryResults;

        public void WriteResults(StreamWriter writer, string category, Dictionary<string, object> additionalData)
        {
            if (!_categoryResults.TryGetValue(category, out var result)) return;

            writer.WriteLine("\n--- PRICE COMPETITIVENESS (Lower = More Competitive) ---");
            
            var priceResults = result.WordPriceCompetitiveness
                .Where(kv => kv.Value.Count >= 10)
                .Select(kv => (kv.Key, kv.Value.AvgPrice, kv.Value.PriceVsCategoryAvg, kv.Value.Count))
                .OrderBy(x => x.PriceVsCategoryAvg)
                .Take(30)
                .ToList();
            
            writer.WriteLine($"  Found {priceResults.Count} words with price data");
            foreach (var r in priceResults)
            {
                var competitiveness = r.PriceVsCategoryAvg < 1.0m ? "CHEAPER" : "EXPENSIVE";
                writer.WriteLine($"  {r.Key}: ${r.AvgPrice:F2} avg (ratio: {r.PriceVsCategoryAvg:F3}) - {competitiveness} ({r.Count} products)");
            }

            writer.WriteLine("\n--- PHRASE PRICE COMPETITIVENESS ---");
            var phrasePriceResults = result.PhrasePriceCompetitiveness
                .Where(kv => kv.Value.Count >= 10)
                .Select(kv => (kv.Key, kv.Value.AvgPrice, kv.Value.PriceVsCategoryAvg, kv.Value.Count))
                .OrderBy(x => x.PriceVsCategoryAvg)
                .Take(30)
                .ToList();
            
            writer.WriteLine($"  Found {phrasePriceResults.Count} phrases with price data");
            foreach (var r in phrasePriceResults)
            {
                var competitiveness = r.PriceVsCategoryAvg < 1.0m ? "CHEAPER" : "EXPENSIVE";
                writer.WriteLine($"  {r.Key}: ${r.AvgPrice:F2} avg (ratio: {r.PriceVsCategoryAvg:F3}) - {competitiveness} ({r.Count} products)");
            }
        }
    }
}
