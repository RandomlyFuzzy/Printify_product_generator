using System;
using System.Collections.Generic;
using System.Linq;
using PrintifyGenerator.Researcher.Interfaces;
using PrintifyGenerator.Researcher.Models;

namespace PrintifyGenerator.Researcher.StatCalculators
{
    public class CtrStatCalculator : IStatCalculator
    {
        public string Name => "CTR";
        public ISet<string> RequiredDatasetTypes => new HashSet<string> { "CTR" };

        private readonly Dictionary<string, CtrStatResult> _categoryResults = new();

        public void InitializeForCategory(string category)
        {
            _categoryResults[category] = new CtrStatResult();
        }

        public void ProcessWord(string word, ProductContext context)
        {
            if (!_categoryResults.TryGetValue(context.Category, out var result)) return;
            if (!context.DatasetData.TryGetValue("CTR", out var ctrObj)) return;
            
            var ctr = (ValueTuple<long, long, long>)ctrObj;
            
            if (!result.WordCtr.TryGetValue(word, out var entry))
                result.WordCtr[word] = (ctr.Item1, ctr.Item2, ctr.Item3, 1);
            else
                result.WordCtr[word] = (entry.Views + ctr.Item1, entry.Carts + ctr.Item2, 
                    entry.Purchases + ctr.Item3, entry.ProductCount + 1);
        }

        public void ProcessPhrase(string phrase, ProductContext context)
        {
            if (!_categoryResults.TryGetValue(context.Category, out var result)) return;
            if (!context.DatasetData.TryGetValue("CTR", out var ctrObj)) return;
            
            var ctr = (ValueTuple<long, long, long>)ctrObj;
            
            if (!result.PhraseCtr.TryGetValue(phrase, out var entry))
                result.PhraseCtr[phrase] = (ctr.Item1, ctr.Item2, ctr.Item3, 1);
            else
                result.PhraseCtr[phrase] = (entry.Views + ctr.Item1, entry.Carts + ctr.Item2, 
                    entry.Purchases + ctr.Item3, entry.ProductCount + 1);
        }

        public object GetResults() => _categoryResults;

        public void WriteResults(StreamWriter writer, string category, Dictionary<string, object> additionalData)
        {
            if (!_categoryResults.TryGetValue(category, out var result)) return;

            writer.WriteLine("\n--- REAL CTR FROM EVENT DATA (view → cart → purchase) ---");
            
            var realCtrResults = new List<(string word, double viewToCartRate, double cartToPurchaseRate, double overallCtr, int productCount)>();
            foreach (var kv in result.WordCtr)
            {
                if (kv.Value.ProductCount < 10) continue;
                var ctr = kv.Value;
                var viewToCart = ctr.Views > 0 ? (double)ctr.Carts / ctr.Views * 100 : 0;
                var cartToPurchase = ctr.Carts > 0 ? (double)ctr.Purchases / ctr.Carts * 100 : 0;
                var overallCtr = ctr.Views > 0 ? (double)ctr.Purchases / ctr.Views * 100 : 0;
                realCtrResults.Add((kv.Key, viewToCart, cartToPurchase, overallCtr, ctr.ProductCount));
            }

            var topRealCtr = realCtrResults.Where(x => x.overallCtr > 0)
                .OrderByDescending(x => x.overallCtr).Take(30).ToList();
            
            writer.WriteLine($"  Found {topRealCtr.Count} words with real CTR data");
            foreach (var r in topRealCtr)
                writer.WriteLine($"  {r.word}: {r.overallCtr:F3}% overall CTR | View→Cart: {r.viewToCartRate:F2}% | Cart→Buy: {r.cartToPurchaseRate:F2}% ({r.productCount} products)");

            writer.WriteLine("\n--- PHRASE REAL CTR FROM EVENT DATA ---");
            var phraseRealCtr = new List<(string phrase, double viewToCartRate, double cartToPurchaseRate, double overallCtr, int productCount)>();
            foreach (var kv in result.PhraseCtr)
            {
                if (kv.Value.ProductCount < 10) continue;
                var ctr = kv.Value;
                var viewToCart = ctr.Views > 0 ? (double)ctr.Carts / ctr.Views * 100 : 0;
                var cartToPurchase = ctr.Carts > 0 ? (double)ctr.Purchases / ctr.Carts * 100 : 0;
                var overallCtr = ctr.Views > 0 ? (double)ctr.Purchases / ctr.Views * 100 : 0;
                phraseRealCtr.Add((kv.Key, viewToCart, cartToPurchase, overallCtr, ctr.ProductCount));
            }

            var topPhraseRealCtr = phraseRealCtr.Where(x => x.overallCtr > 0).OrderByDescending(x => x.overallCtr).Take(30).ToList();
            writer.WriteLine($"  Found {topPhraseRealCtr.Count} phrases with real CTR data");
            foreach (var r in topPhraseRealCtr)
                writer.WriteLine($"  {r.phrase}: {r.overallCtr:F3}% overall CTR | View→Cart: {r.viewToCartRate:F2}% | Cart→Buy: {r.cartToPurchaseRate:F2}% ({r.productCount} products)");
        }
    }
}
