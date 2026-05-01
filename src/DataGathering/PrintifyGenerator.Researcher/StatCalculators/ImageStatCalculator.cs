using System;
using System.Collections.Generic;
using System.Linq;
using PrintifyGenerator.Researcher.Interfaces;
using PrintifyGenerator.Researcher.Models;

namespace PrintifyGenerator.Researcher.StatCalculators
{
    public class ImageStatCalculator : IStatCalculator
    {
        public string Name => "Images";
        public ISet<string> RequiredDatasetTypes => new HashSet<string>();

        private readonly Dictionary<string, ImageStatResult> _categoryResults = new();

        public void InitializeForCategory(string category)
        {
            _categoryResults[category] = new ImageStatResult();
        }

        public void ProcessWord(string word, ProductContext context)
        {
            if (!_categoryResults.TryGetValue(context.Category, out var result)) return;
            if (context.ImageCount <= 0) return;

            if (!result.WordImages.TryGetValue(word, out var entry))
                result.WordImages[word] = (context.ImageCount, 1);
            else
                result.WordImages[word] = (entry.TotalImages + context.ImageCount, entry.ProductCount + 1);
        }

        public void ProcessPhrase(string phrase, ProductContext context)
        {
            if (!_categoryResults.TryGetValue(context.Category, out var result)) return;
            if (context.ImageCount <= 0) return;

            if (!result.PhraseImages.TryGetValue(phrase, out var entry))
                result.PhraseImages[phrase] = (context.ImageCount, 1);
            else
                result.PhraseImages[phrase] = (entry.TotalImages + context.ImageCount, entry.ProductCount + 1);
        }

        public object GetResults() => _categoryResults;

        public void WriteResults(StreamWriter writer, string category, Dictionary<string, object> additionalData)
        {
            if (!_categoryResults.TryGetValue(category, out var result)) return;

            writer.WriteLine("\n--- IMAGE COUNT ANALYSIS ---");
            
            var imageResults = result.WordImages
                .Where(kv => kv.Value.ProductCount >= 10)
                .Select(kv => (kv.Key, AvgImages: (double)kv.Value.TotalImages / kv.Value.ProductCount, kv.Value.ProductCount))
                .OrderByDescending(x => x.AvgImages)
                .Take(30)
                .ToList();
            
            writer.WriteLine($"  Found {imageResults.Count} words with image data");
            foreach (var r in imageResults)
                writer.WriteLine($"  {r.Key}: {r.AvgImages:F1} avg images per product ({r.ProductCount} products)");

            writer.WriteLine("\n--- PHRASE IMAGE COUNT ANALYSIS ---");
            var phraseImageResults = result.PhraseImages
                .Where(kv => kv.Value.ProductCount >= 10)
                .Select(kv => (kv.Key, AvgImages: (double)kv.Value.TotalImages / kv.Value.ProductCount, kv.Value.ProductCount))
                .OrderByDescending(x => x.AvgImages)
                .Take(30)
                .ToList();
            
            writer.WriteLine($"  Found {phraseImageResults.Count} phrases with image data");
            foreach (var r in phraseImageResults)
                writer.WriteLine($"  {r.Key}: {r.AvgImages:F1} avg images per product ({r.ProductCount} products)");
        }
    }
}
