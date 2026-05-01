using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using PrintifyGenerator.Researcher.Interfaces;
using PrintifyGenerator.Researcher.Models;

namespace PrintifyGenerator.Researcher.StatCalculators
{
    public class SeasonalityStatCalculator : IStatCalculator
    {
        public string Name => "Seasonality";
        public ISet<string> RequiredDatasetTypes => new HashSet<string>();

        private readonly Dictionary<string, SeasonalityStatResult> _categoryResults = new();
        private readonly Dictionary<string, List<(string Asin, DateTime Date)>> _productDates = new();

        public void InitializeForCategory(string category)
        {
            _categoryResults[category] = new SeasonalityStatResult();
        }

        public void RegisterProductDate(string asin, string category, DateTime date)
        {
            if (!_productDates.TryGetValue(category, out var list))
                _productDates[category] = list = new List<(string, DateTime)>();
            list.Add((asin, date));
        }

        public void ProcessWord(string word, ProductContext context)
        {
            if (!_categoryResults.TryGetValue(context.Category, out var result)) return;
            if (!context.DatasetData.TryGetValue("Date", out var dateObj)) return;

            var date = (DateTime)dateObj;
            var month = date.Month - 1; // 0-indexed

            if (!result.WordSeasonality.TryGetValue(word, out var entry))
                result.WordSeasonality[word] = (new int[12], 1);
            else
                entry.TotalCount++;

            var stats = result.WordSeasonality[word];
            stats.MonthlyCount[month]++;
            result.WordSeasonality[word] = stats;
        }

        public void ProcessPhrase(string phrase, ProductContext context)
        {
            if (!_categoryResults.TryGetValue(context.Category, out var result)) return;
            if (!context.DatasetData.TryGetValue("Date", out var dateObj)) return;

            var date = (DateTime)dateObj;
            var month = date.Month - 1;

            if (!result.PhraseSeasonality.TryGetValue(phrase, out var entry))
                result.PhraseSeasonality[phrase] = (new int[12], 1);
            else
                entry.TotalCount++;

            var stats = result.PhraseSeasonality[phrase];
            stats.MonthlyCount[month]++;
            result.PhraseSeasonality[phrase] = stats;
        }

        public object GetResults() => _categoryResults;

        public void WriteResults(StreamWriter writer, string category, Dictionary<string, object> additionalData)
        {
            if (!_categoryResults.TryGetValue(category, out var result)) return;

            writer.WriteLine("\n--- SEASONALITY (Top Words by Seasonal Variance) ---");

            var monthNames = new[] { "Jan", "Feb", "Mar", "Apr", "May", "Jun", "Jul", "Aug", "Sep", "Oct", "Nov", "Dec" };

            var seasonalityResults = result.WordSeasonality
                .Where(kv => kv.Value.TotalCount >= 50)
                .Select(kv =>
                {
                    var monthly = kv.Value.MonthlyCount;
                    var total = kv.Value.TotalCount;
                    var avg = total / 12.0;
                    var variance = monthly.Sum(m => Math.Pow(m - avg, 2)) / 12;
                    var maxMonth = Array.IndexOf(monthly, monthly.Max());
                    return (kv.Key, Variance: variance, MaxMonth: maxMonth, Monthly: monthly, Total: total);
                })
                .OrderByDescending(x => x.Variance)
                .Take(30)
                .ToList();

            writer.WriteLine($"  Found {seasonalityResults.Count} words with seasonal patterns");
            foreach (var r in seasonalityResults)
            {
                writer.WriteLine($"  {r.Key}: peaks in {monthNames[r.MaxMonth]} (variance: {r.Variance:F1})");
                writer.WriteLine($"    Monthly: {string.Join(", ", r.Monthly.Select((m, i) => $"{monthNames[i]}:{m}"))}");
            }
        }
    }
}
