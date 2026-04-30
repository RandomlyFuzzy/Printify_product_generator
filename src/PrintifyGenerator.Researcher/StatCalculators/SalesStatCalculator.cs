using System;
using System.Collections.Generic;
using System.Linq;
using PrintifyGenerator.Researcher.Interfaces;
using PrintifyGenerator.Researcher.Models;

namespace PrintifyGenerator.Researcher.StatCalculators
{
    public class SalesStatCalculator : IStatCalculator
    {
        public string Name => "Sales";
        public ISet<string> RequiredDatasetTypes => new HashSet<string>();

        private readonly Dictionary<string, SalesStatResult> _categoryResults = new();
        private readonly Dictionary<string, (int TopCount, int BottomCount)> _topBottomThresholds = new();

        public void InitializeForCategory(string category)
        {
            _categoryResults[category] = new SalesStatResult();
        }

        public void SetThresholds(string category, int topCount, int bottomCount)
        {
            _topBottomThresholds[category] = (topCount, bottomCount);
        }

        public void ProcessWord(string word, ProductContext context)
        {
            if (!_categoryResults.TryGetValue(context.Category, out var result)) return;

            if (context.IsTopSeller)
            {
                if (!result.WordSales.TryGetValue(word, out var entry))
                    result.WordSales[word] = (context.Sales, 1);
                else
                    result.WordSales[word] = (entry.Sales + context.Sales, entry.Count + 1);

                result.WordTopFreq.TryGetValue(word, out var tf);
                result.WordTopFreq[word] = tf + 1;
            }

            if (context.IsBottomSeller)
            {
                result.WordBottomFreq.TryGetValue(word, out var bf);
                result.WordBottomFreq[word] = bf + 1;
            }
        }

        public void ProcessPhrase(string phrase, ProductContext context)
        {
            if (!_categoryResults.TryGetValue(context.Category, out var result)) return;

            if (context.IsTopSeller)
            {
                if (!result.PhraseSales.TryGetValue(phrase, out var entry))
                    result.PhraseSales[phrase] = (context.Sales, 1);
                else
                    result.PhraseSales[phrase] = (entry.Sales + context.Sales, entry.Count + 1);

                result.PhraseTopFreq.TryGetValue(phrase, out var tf);
                result.PhraseTopFreq[phrase] = tf + 1;
            }

            if (context.IsBottomSeller)
            {
                result.PhraseBottomFreq.TryGetValue(phrase, out var bf);
                result.PhraseBottomFreq[phrase] = bf + 1;
            }
        }

        public object GetResults() => _categoryResults;

        public void WriteResults(StreamWriter writer, string category, Dictionary<string, object> additionalData)
        {
            if (!_categoryResults.TryGetValue(category, out var result)) return;

            const int MIN_COUNT = 50;
            var topCounts = additionalData["topCounts"] as Dictionary<string, int>;
            var bottomCounts = additionalData["bottomCounts"] as Dictionary<string, int>;
            var topCount = topCounts?.GetValueOrDefault(category, 1) ?? 1;
            var bottomCount = bottomCounts?.GetValueOrDefault(category, 1) ?? 1;

            writer.WriteLine("\n--- TOP OVERLAP (SENTIMENT + SALES) ---");
            
            var sentimentCalc = additionalData.TryGetValue("SentimentCalc", out var sc) ? sc as SentimentStatCalculator : null;
            var sentimentResults = sentimentCalc?.GetResults() as Dictionary<string, SentimentStatResult>;

            var overlap = new List<(string word, double sentimentPct, long salesVolume, int total)>();
            foreach (var kv in result.WordSales)
            {
                if (kv.Value.Count < MIN_COUNT) continue;
                
                if (sentimentResults != null && sentimentResults.TryGetValue(category, out var sentResult))
                {
                    if (sentResult.WordSentiment.TryGetValue(kv.Key, out var sentData))
                    {
                        var sentimentPct = (double)sentData.Positive / sentData.Total * 100;
                        if (sentimentPct >= 70)
                            overlap.Add((kv.Key, sentimentPct, kv.Value.Sales, kv.Value.Count));
                    }
                }
            }

            var topOverlap = overlap.OrderByDescending(x => x.salesVolume)
                .ThenByDescending(x => x.sentimentPct).Take(30).ToList();
            
            writer.WriteLine($"  Found {topOverlap.Count} high-sentiment words in top sales products");
            foreach (var r in topOverlap)
                writer.WriteLine($"  {r.word}: {r.sentimentPct:F1}% positive, {r.salesVolume:N0} sales ({r.total:N0} products)");

            writer.WriteLine("\n--- CTR INDICATORS (TOP 10% vs BOTTOM 10% SALES) ---");
            
            var ctrResults = new List<(string word, double ctrLift, int topFreq, int bottomFreq, int total)>();
            foreach (var kv in result.WordSales)
            {
                if (kv.Value.Count < MIN_COUNT) continue;
                var tf = result.WordTopFreq.GetValueOrDefault(kv.Key);
                var bf = result.WordBottomFreq.GetValueOrDefault(kv.Key);
                
                if (tf >= MIN_COUNT && bf >= MIN_COUNT)
                {
                    var topRate = (double)tf / topCount;
                    var bottomRate = (double)bf / bottomCount;
                    var ctrLift = bottomRate > 0 ? (topRate - bottomRate) / bottomRate * 100 : 0;
                    ctrResults.Add((kv.Key, ctrLift, tf, bf, kv.Value.Count));
                }
            }

            var topCtr = ctrResults.Where(x => x.ctrLift > 0).OrderByDescending(x => x.ctrLift).Take(30).ToList();
            writer.WriteLine($"  Found {topCtr.Count} words with CTR signal");
            foreach (var r in topCtr)
                writer.WriteLine($"  {r.word}: +{r.ctrLift:F1}% CTR lift ({r.topFreq} in top / {r.bottomFreq} in bottom)");
        }
    }
}
