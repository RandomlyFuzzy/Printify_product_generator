using System;
using System.Collections.Generic;
using System.Linq;
using PrintifyGenerator.Researcher.Interfaces;
using PrintifyGenerator.Researcher.Models;

namespace PrintifyGenerator.Researcher.StatCalculators
{
    public class MaterialStatCalculator : IStatCalculator
    {
        public string Name => "Material";
        public ISet<string> RequiredDatasetTypes => new HashSet<string>();

        private readonly Dictionary<string, MaterialStatResult> _categoryResults = new();
        private static readonly HashSet<string> CommonMaterials = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "cotton", "polyester", "silk", "wool", "linen", "denim", "leather", "suede", "velvet",
            "satin", "chiffon", "nylon", "spandex", "elastane", "lycra", "rayon", "viscose",
            "canvas", "jersey", "fleece", "flannel", "tweed", "cashmere", "mohair", "alpaca",
            "synthetic", "blend", "mesh", "lace", "knit", "crochet", "ribbed", "quilted"
        };

        public void InitializeForCategory(string category)
        {
            _categoryResults[category] = new MaterialStatResult();
        }

        public void ProcessWord(string word, ProductContext context)
        {
            if (!_categoryResults.TryGetValue(context.Category, out var result)) return;

            if (!CommonMaterials.Contains(word)) return;

            TrackMaterial(result, word.ToLowerInvariant(), context);
        }

        public void ProcessMaterial(string materialValue, ProductContext context)
        {
            if (!_categoryResults.TryGetValue(context.Category, out var result)) return;

            foreach (var material in ExtractKnownMaterials(materialValue))
                TrackMaterial(result, material, context);
        }

        public void ProcessPhrase(string phrase, ProductContext context)
        {
        }

        private static IEnumerable<string> ExtractKnownMaterials(string materialValue)
        {
            if (string.IsNullOrWhiteSpace(materialValue))
                yield break;

            foreach (var token in materialValue
                .ToLowerInvariant()
                .Split(new[] { ' ', ',', '/', ';', '|', '&' }, StringSplitOptions.RemoveEmptyEntries))
            {
                if (CommonMaterials.Contains(token))
                    yield return token;
            }
        }

        private static void TrackMaterial(MaterialStatResult result, string material, ProductContext context)
        {
            if (!result.MaterialStats.TryGetValue(material, out var entry))
                result.MaterialStats[material] = (1, context.Sentiment, context.Sales, context.Price);
            else
            {
                var newCount = entry.Count + 1;
                result.MaterialStats[material] = (newCount,
                    (entry.AvgSentiment * entry.Count + context.Sentiment) / newCount,
                    entry.TotalSales + context.Sales,
                    (entry.AvgPrice * entry.Count + context.Price) / newCount);
            }
        }

        public object GetResults() => _categoryResults;

        public void WriteResults(StreamWriter writer, string category, Dictionary<string, object> additionalData)
        {
            if (!_categoryResults.TryGetValue(category, out var result)) return;

            writer.WriteLine("\n--- MATERIAL ANALYSIS ---");

            var materialResults = result.MaterialStats
                .Where(kv => kv.Value.Count >= 10)
                .Select(kv => (kv.Key, kv.Value.Count, kv.Value.AvgSentiment, kv.Value.TotalSales, kv.Value.AvgPrice))
                .OrderByDescending(x => x.AvgSentiment)
                .Take(30)
                .ToList();

            writer.WriteLine($"  Found {materialResults.Count} materials with sufficient data");
            foreach (var r in materialResults)
                writer.WriteLine($"  {r.Key}: {r.Count} products, {r.AvgSentiment:F1} avg sentiment, {r.TotalSales:N0} sales, ${r.AvgPrice:F2} avg price");
        }
    }
}
