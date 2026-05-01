using System;
using System.Collections.Generic;
using System.Linq;
using PrintifyGenerator.Researcher.Interfaces;
using PrintifyGenerator.Researcher.Models;

namespace PrintifyGenerator.Researcher.StatCalculators
{
    public class SentimentStatCalculator : IStatCalculator
    {
        public string Name => "Sentiment";
        public ISet<string> RequiredDatasetTypes => new HashSet<string>();

        private readonly Dictionary<string, SentimentStatResult> _categoryResults = new();

        public void InitializeForCategory(string category)
        {
            _categoryResults[category] = new SentimentStatResult();
        }

        public void ProcessWord(string word, ProductContext context)
        {
            if (!_categoryResults.TryGetValue(context.Category, out var result)) return;
            
            var isPositive = context.Sentiment >= 4.0;
            var sentiment = result.WordSentiment;
            
            if (!sentiment.TryGetValue(word, out var entry))
                sentiment[word] = (isPositive ? 1 : 0, 1);
            else
                sentiment[word] = (entry.Positive + (isPositive ? 1 : 0), entry.Total + 1);
        }

        public void ProcessPhrase(string phrase, ProductContext context)
        {
            if (!_categoryResults.TryGetValue(context.Category, out var result)) return;
            
            var isPositive = context.Sentiment >= 4.0;
            var sentiment = result.PhraseSentiment;
            
            if (!sentiment.TryGetValue(phrase, out var entry))
                sentiment[phrase] = (isPositive ? 1 : 0, 1);
            else
                sentiment[phrase] = (entry.Positive + (isPositive ? 1 : 0), entry.Total + 1);
        }

        public object GetResults() => _categoryResults;

        public void WriteResults(StreamWriter writer, string category, Dictionary<string, object> additionalData)
        {
            if (!_categoryResults.TryGetValue(category, out var result)) return;

            const int MIN_COUNT = 50;

            writer.WriteLine("--- SENTIMENT (Words & Phrases) ---");

            var wordResults = result.WordSentiment
                .Where(kv => kv.Value.Total >= MIN_COUNT)
                .Select(kv => new
                {
                    Word = kv.Key,
                    Pct = (double)kv.Value.Positive / kv.Value.Total * 100,
                    Total = kv.Value.Total
                })
                .Where(x => x.Pct >= 70)
                .OrderByDescending(x => x.Pct)
                .Take(30)
                .ToList();

            writer.WriteLine($"  Found {wordResults.Count} high-sentiment words (>=70% positive, >=50 occurrences)");
            foreach (var r in wordResults)
                writer.WriteLine($"  {r.Word}: {r.Pct:F1}% positive ({r.Total} products)");

            var phraseResults = result.PhraseSentiment
                .Where(kv => kv.Value.Total >= MIN_COUNT)
                .Select(kv => new
                {
                    Phrase = kv.Key,
                    Pct = (double)kv.Value.Positive / kv.Value.Total * 100,
                    Total = kv.Value.Total
                })
                .Where(x => x.Pct >= 70)
                .OrderByDescending(x => x.Pct)
                .Take(30)
                .ToList();

            writer.WriteLine($"  Found {phraseResults.Count} high-sentiment phrases (>=70% positive, >=50 occurrences)");
            foreach (var r in phraseResults)
                writer.WriteLine($"  {r.Phrase}: {r.Pct:F1}% positive ({r.Total} products)");
        }
    }
}
