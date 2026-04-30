using System;
using System.Collections.Generic;
using System.Linq;
using PrintifyGenerator.Researcher.Interfaces;
using PrintifyGenerator.Researcher.Models;

namespace PrintifyGenerator.Researcher.StatCalculators
{
    public class BrandStatCalculator : IStatCalculator
    {
        public string Name => "Brand";
        public ISet<string> RequiredDatasetTypes => new HashSet<string>();

        private readonly Dictionary<string, BrandStatResult> _categoryResults = new();

        public void InitializeForCategory(string category)
        {
            _categoryResults[category] = new BrandStatResult();
        }

        public void ProcessWord(string word, ProductContext context)
        {
            if (!_categoryResults.TryGetValue(context.Category, out var result)) return;
        }

        public void ProcessPhrase(string phrase, ProductContext context)
        {
            if (!_categoryResults.TryGetValue(context.Category, out var result)) return;
        }

        public void ProcessBrand(string brand, ProductContext context)
        {
            if (!_categoryResults.TryGetValue(context.Category, out var result)) return;
            if (string.IsNullOrEmpty(brand)) return;

            var normalizedBrand = brand.ToLower();
            if (!result.BrandStats.TryGetValue(normalizedBrand, out var entry))
                result.BrandStats[normalizedBrand] = (1, context.Sentiment, context.Sales, 
                    context.IsTopSeller ? 1 : 0, context.IsBottomSeller ? 1 : 0);
            else
                result.BrandStats[normalizedBrand] = (entry.Count + 1,
                    (entry.AvgSentiment * entry.Count + context.Sentiment) / (entry.Count + 1),
                    entry.TotalSales + context.Sales,
                    entry.TopCount + (context.IsTopSeller ? 1 : 0),
                    entry.BottomCount + (context.IsBottomSeller ? 1 : 0));
        }

        public object GetResults() => _categoryResults;

        public void WriteResults(StreamWriter writer, string category, Dictionary<string, object> additionalData)
        {
            if (!_categoryResults.TryGetValue(category, out var result)) return;

            writer.WriteLine("\n--- BRAND ANALYSIS ---");

            var brandResults = result.BrandStats
                .Where(kv => kv.Value.Count >= 10)
                .Select(kv => (kv.Key, kv.Value.Count, kv.Value.AvgSentiment, kv.Value.TotalSales, 
                    kv.Value.TopCount, kv.Value.BottomCount))
                .OrderByDescending(x => x.AvgSentiment)
                .Take(30)
                .ToList();

            writer.WriteLine($"  Found {brandResults.Count} brands with sufficient data");
            foreach (var r in brandResults)
            {
                var topPct = r.Count > 0 ? (double)r.TopCount / r.Count * 100 : 0;
                writer.WriteLine($"  {r.Key}: {r.Count} products, {r.AvgSentiment:F1} avg sentiment, {r.TotalSales:N0} sales, {topPct:F1}% in top sellers");
            }
        }
    }
}
