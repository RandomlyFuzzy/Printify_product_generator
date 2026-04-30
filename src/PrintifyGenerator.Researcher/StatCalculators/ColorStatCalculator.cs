using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using PrintifyGenerator.Researcher.Interfaces;
using PrintifyGenerator.Researcher.Models;

namespace PrintifyGenerator.Researcher.StatCalculators
{
    public class ColorStatCalculator : IStatCalculator
    {
        public string Name => "Color";
        public ISet<string> RequiredDatasetTypes => new HashSet<string>();

        private readonly Dictionary<string, ColorStatResult> _categoryResults = new();
        private static readonly HashSet<string> CommonColors = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "red", "blue", "green", "yellow", "orange", "purple", "pink", "black", "white", "gray", "grey",
            "brown", "navy", "teal", "cyan", "magenta", "maroon", "olive", "lime", "aqua",
            "gold", "silver", "bronze", "copper", "beige", "tan", "ivory", "cream", "khaki",
            "coral", "salmon", "peach", "plum", "lavender", "mint", "rose", "wine", "burgundy",
            "turquoise", "indigo", "violet", "fuchsia", "crimson", "scarlet", "azure", "coral"
        };

        public void InitializeForCategory(string category)
        {
            _categoryResults[category] = new ColorStatResult();
        }

        public void ProcessWord(string word, ProductContext context)
        {
            if (!_categoryResults.TryGetValue(context.Category, out var result)) return;
        }

        public void ProcessPhrase(string phrase, ProductContext context)
        {
            if (!_categoryResults.TryGetValue(context.Category, out var result)) return;

            var words = phrase.Split(' ');
            foreach (var word in words)
            {
                if (!CommonColors.Contains(word)) continue;

                var color = word.ToLower();
                if (!result.ColorStats.TryGetValue(color, out var entry))
                    result.ColorStats[color] = (1, context.Sentiment, context.Sales);
                else
                    result.ColorStats[color] = (entry.Count + 1, 
                        (entry.AvgSentiment * entry.Count + context.Sentiment) / (entry.Count + 1),
                        entry.TotalSales + context.Sales);

                if (!result.ColorWordSentiment.TryGetValue(color, out var sentEntry))
                    result.ColorWordSentiment[color] = (1, context.Sentiment);
                else
                    result.ColorWordSentiment[color] = (sentEntry.Count + 1,
                        (sentEntry.AvgSentiment * sentEntry.Count + context.Sentiment) / (sentEntry.Count + 1));
            }
        }

        public object GetResults() => _categoryResults;

        public void WriteResults(StreamWriter writer, string category, Dictionary<string, object> additionalData)
        {
            if (!_categoryResults.TryGetValue(category, out var result)) return;

            writer.WriteLine("\n--- COLOR ANALYSIS ---");

            var colorResults = result.ColorStats
                .Where(kv => kv.Value.Count >= 10)
                .Select(kv => (kv.Key, kv.Value.Count, kv.Value.AvgSentiment, kv.Value.TotalSales))
                .OrderByDescending(x => x.AvgSentiment)
                .Take(30)
                .ToList();

            writer.WriteLine($"  Found {colorResults.Count} colors with sufficient data");
            foreach (var r in colorResults)
                writer.WriteLine($"  {r.Key}: {r.Count} products, {r.AvgSentiment:F1} avg sentiment, {r.TotalSales:N0} total sales");

            writer.WriteLine("\n--- COLOR SENTIMENT (Words containing colors) ---");
            var colorSentiment = result.ColorWordSentiment
                .Where(kv => kv.Value.Count >= 10)
                .Select(kv => (kv.Key, kv.Value.Count, kv.Value.AvgSentiment))
                .OrderByDescending(x => x.AvgSentiment)
                .Take(20)
                .ToList();

            foreach (var r in colorSentiment)
                writer.WriteLine($"  {r.Key}: {r.Count} occurrences, {r.AvgSentiment:F1} avg sentiment");
        }
    }
}
