using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using PrintifyGenerator.Researcher.Models;

namespace PrintifyGenerator.Researcher.StatCalculators
{
    public class PivotTableGenerator
    {
        public static void GenerateWordPivotTable(
            Dictionary<string, SentimentStatResult> sentimentResults,
            Dictionary<string, SalesStatResult> salesResults,
            Dictionary<string, CtrStatResult> ctrResults,
            Dictionary<string, PriceStatResult> priceResults,
            Dictionary<string, ImageStatResult> imageResults,
            Dictionary<string, SeasonalityStatResult> seasonalityResults,
            Dictionary<string, ColorStatResult> colorResults,
            Dictionary<string, MaterialStatResult> materialResults,
            Dictionary<string, BrandStatResult> brandResults,
            string outputDir,
            Dictionary<string, int> topCounts,
            Dictionary<string, int> bottomCounts,
            Dictionary<string, List<(string asin, int sales)>> categoryProducts)
        {
            var outputPath = Path.Combine(outputDir, "word_pivot_table.csv");
            using var writer = new StreamWriter(outputPath);

            // Write header with calculated fields
            writer.WriteLine("Category,Word,OverallScore," +
                "SentimentPct,SentimentPos,SentimentTotal," +
                "SalesVolume,SalesCount,SalesPerProduct," +
                "TopFreq,BottomFreq,TopBottomRatio,CtrLift," +
                "CtrViews,CtrCarts,CtrPurchases,ViewToCartPct,CartToPurchasePct,OverallCtrPct," +
                "PriceAvg,PriceVsCategory,PriceMin,PriceMax," +
                "ImageAvg,ImageCount," +
                "SeasonalityVariance,PeakMonth," +
                "IsColor,IsMaterial,IsBrand");

            var allWords = new HashSet<string>();
            foreach (var sr in sentimentResults.Values)
                foreach (var w in sr.WordSentiment.Keys) allWords.Add(w);
            foreach (var sr in salesResults.Values)
                foreach (var w in sr.WordSales.Keys) allWords.Add(w);

            var monthNames = new[] { "Jan", "Feb", "Mar", "Apr", "May", "Jun", "Jul", "Aug", "Sep", "Oct", "Nov", "Dec" };

            var wordWriterLock = new object();

            Parallel.ForEach(sentimentResults.Keys, category =>
            {
                if (!categoryProducts.TryGetValue(category, out var products) || products.Count == 0) return;

                var topCount = topCounts.GetValueOrDefault(category, 1);
                var bottomCount = bottomCounts.GetValueOrDefault(category, 1);
                var categoryAvgSales = products.Average(p => p.sales);

                var sentimentResult = sentimentResults.GetValueOrDefault(category);
                var salesResult = salesResults.GetValueOrDefault(category);
                var ctrResult = ctrResults.GetValueOrDefault(category);
                var priceResult = priceResults.GetValueOrDefault(category);
                var imageResult = imageResults.GetValueOrDefault(category);
                var seasonalityResult = seasonalityResults.GetValueOrDefault(category);

                var categoryRows = new List<(double score, string line)>();

                foreach (var word in allWords)
                {
                    // Sentiment data
                    var sentimentPct = 0.0;
                    var sentPos = 0;
                    var sentTotal = 0;
                    if (sentimentResult?.WordSentiment.TryGetValue(word, out var sentData) == true)
                    {
                        sentPos = sentData.Positive;
                        sentTotal = sentData.Total;
                        sentimentPct = sentTotal > 0 ? (double)sentPos / sentTotal * 100 : 0;
                    }

                    // Sales data
                    var salesVolume = 0L;
                    var salesCount = 0;
                    var topFreq = 0;
                    var bottomFreq = 0;
                    if (salesResult?.WordSales.TryGetValue(word, out var salesData) == true)
                    {
                        salesVolume = salesData.Sales;
                        salesCount = salesData.Count;
                    }
                    if (salesResult?.WordTopFreq.TryGetValue(word, out var tf) == true) topFreq = tf;
                    if (salesResult?.WordBottomFreq.TryGetValue(word, out var bf) == true) bottomFreq = bf;

                    // Skip words with no data in this category
                    if (sentTotal == 0 && salesVolume == 0 && topFreq == 0) continue;

                    var topBottomRatio = bottomFreq > 0 ? (double)topFreq / bottomFreq : (topFreq > 0 ? double.MaxValue : 0);
                    var ctrLift = (topFreq >= 50 && bottomFreq >= 50 && bottomCount > 0)
                        ? ((double)topFreq / topCount - (double)bottomFreq / bottomCount) / (bottomCount > 0 ? (double)bottomFreq / bottomCount : 1) * 100
                        : 0;

                    // CTR data
                    var ctrViews = 0L;
                    var ctrCarts = 0L;
                    var ctrPurchases = 0L;
                    var viewToCartPct = 0.0;
                    var cartToPurchasePct = 0.0;
                    var overallCtrPct = 0.0;
                    if (ctrResult?.WordCtr.TryGetValue(word, out var ctrData) == true)
                    {
                        ctrViews = ctrData.Views;
                        ctrCarts = ctrData.Carts;
                        ctrPurchases = ctrData.Purchases;
                        viewToCartPct = ctrViews > 0 ? (double)ctrCarts / ctrViews * 100 : 0;
                        cartToPurchasePct = ctrCarts > 0 ? (double)ctrPurchases / ctrCarts * 100 : 0;
                        overallCtrPct = ctrViews > 0 ? (double)ctrPurchases / ctrViews * 100 : 0;
                    }

                    // Price data
                    var priceAvg = 0m;
                    var priceVsCategory = 1.0m;
                    var priceMin = 0m;
                    var priceMax = 0m;
                    if (priceResult?.WordPriceCompetitiveness.TryGetValue(word, out var priceComp) == true)
                    {
                        priceAvg = priceComp.AvgPrice;
                        priceVsCategory = priceComp.PriceVsCategoryAvg;
                    }
                    if (priceResult?.WordPrice.TryGetValue(word, out var priceData) == true)
                    {
                        priceMin = priceData.MinPrice;
                        priceMax = priceData.MaxPrice;
                    }

                    // Image data
                    var imageAvg = 0.0;
                    var imageCount = 0;
                    if (imageResult?.WordImages.TryGetValue(word, out var imgData) == true)
                    {
                        imageAvg = imgData.ProductCount > 0 ? (double)imgData.TotalImages / imgData.ProductCount : 0;
                        imageCount = imgData.ProductCount;
                    }

                    // Seasonality data
                    var seasonalityVariance = 0.0;
                    var peakMonth = -1;
                    if (seasonalityResult?.WordSeasonality.TryGetValue(word, out var seasData) == true)
                    {
                        var monthly = seasData.MonthlyCount;
                        var total = seasData.TotalCount;
                        var avg = total / 12.0;
                        seasonalityVariance = monthly.Sum(m => Math.Pow(m - avg, 2)) / 12;
                        peakMonth = Array.IndexOf(monthly, monthly.Max());
                    }

                    // Color/Material/Brand flags
                    var isColor = IsColor(word);
                    var isMaterial = IsMaterial(word);
                    var isBrand = IsBrand(word);

                    // Overall score calculation (weighted average of normalized metrics)
                    var overallScore = CalculateOverallScore(sentimentPct, salesVolume, salesCount, ctrLift, overallCtrPct, priceVsCategory);

                    var line = $"\"{category}\",\"{word}\",{overallScore:F4}," +
                        $"{sentimentPct:F1},{sentPos},{sentTotal}," +
                        $"{salesVolume},{salesCount},{(salesCount > 0 ? salesVolume / (double)salesCount : 0):F1}," +
                        $"{topFreq},{bottomFreq},{topBottomRatio:F2},{ctrLift:F1}," +
                        $"{ctrViews},{ctrCarts},{ctrPurchases},{viewToCartPct:F2},{cartToPurchasePct:F2},{overallCtrPct:F3}," +
                        $"{priceAvg:F2},{priceVsCategory:F3},{priceMin:F2},{priceMax:F2}," +
                        $"{imageAvg:F1},{imageCount}," +
                        $"{seasonalityVariance:F1},{(peakMonth >= 0 ? monthNames[peakMonth] : "N/A")}," +
                        $"{isColor},{isMaterial},{isBrand}";

                    categoryRows.Add((overallScore, line));
                }

                // Emit only top 25% by OverallScore
                var top25Count = (int)Math.Ceiling(categoryRows.Count * 0.25);
                var top25Rows = categoryRows.OrderByDescending(r => r.score).Take(top25Count).ToList();
                lock (wordWriterLock)
                {
                    foreach (var (_, line) in top25Rows)
                        writer.WriteLine(line);
                }
            });

            Console.WriteLine($"Word pivot table written to: {outputPath}");
        }

        public static void GeneratePhrasePivotTable(
            Dictionary<string, SentimentStatResult> sentimentResults,
            Dictionary<string, SalesStatResult> salesResults,
            Dictionary<string, CtrStatResult> ctrResults,
            Dictionary<string, PriceStatResult> priceResults,
            Dictionary<string, ImageStatResult> imageResults,
            string outputDir,
            Dictionary<string, int> topCounts,
            Dictionary<string, int> bottomCounts)
        {
            var outputPath = Path.Combine(outputDir, "phrase_pivot_table.csv");
            using var writer = new StreamWriter(outputPath);

            writer.WriteLine("Category,Phrase,OverallScore," +
                "SentimentPct,SentimentPos,SentimentTotal," +
                "SalesVolume,SalesCount,SalesPerProduct," +
                "TopFreq,BottomFreq,TopBottomRatio,CtrLift," +
                "CtrViews,CtrCarts,CtrPurchases,ViewToCartPct,CartToPurchasePct,OverallCtrPct," +
                "PriceAvg,PriceVsCategory," +
                "ImageAvg,ImageCount");

            var allPhrases = new HashSet<string>();
            foreach (var sr in sentimentResults.Values)
                foreach (var p in sr.PhraseSentiment.Keys) allPhrases.Add(p);

            var phraseWriterLock = new object();

            Parallel.ForEach(sentimentResults.Keys, category =>
            {
                var topCount = topCounts.GetValueOrDefault(category, 1);
                var bottomCount = bottomCounts.GetValueOrDefault(category, 1);

                var sentimentResult = sentimentResults.GetValueOrDefault(category);
                var salesResult = salesResults.GetValueOrDefault(category);
                var ctrResult = ctrResults.GetValueOrDefault(category);
                var priceResult = priceResults.GetValueOrDefault(category);
                var imageResult = imageResults.GetValueOrDefault(category);

                var categoryPhraseRows = new List<(double score, string line)>();

                foreach (var phrase in allPhrases)
                {
                    var sentimentPct = 0.0;
                    var sentPos = 0;
                    var sentTotal = 0;
                    if (sentimentResult?.PhraseSentiment.TryGetValue(phrase, out var sentData) == true)
                    {
                        sentPos = sentData.Positive;
                        sentTotal = sentData.Total;
                        sentimentPct = sentTotal > 0 ? (double)sentPos / sentTotal * 100 : 0;
                    }

                    var salesVolume = 0L;
                    var salesCount = 0;
                    var topFreq = 0;
                    var bottomFreq = 0;
                    if (salesResult?.PhraseSales.TryGetValue(phrase, out var salesData) == true)
                    {
                        salesVolume = salesData.Sales;
                        salesCount = salesData.Count;
                    }
                    if (salesResult?.PhraseTopFreq.TryGetValue(phrase, out var tf) == true) topFreq = tf;
                    if (salesResult?.PhraseBottomFreq.TryGetValue(phrase, out var bf) == true) bottomFreq = bf;

                    // Skip phrases with no data in this category
                    if (sentTotal == 0 && salesVolume == 0 && topFreq == 0) continue;

                    var topBottomRatio = bottomFreq > 0 ? (double)topFreq / bottomFreq : (topFreq > 0 ? double.MaxValue : 0);
                    var ctrLift = (topFreq >= 50 && bottomFreq >= 50)
                        ? ((double)topFreq / topCount - (double)bottomFreq / bottomCount) / (bottomCount > 0 ? (double)bottomFreq / bottomCount : 1) * 100
                        : 0;

                    var ctrViews = 0L;
                    var ctrCarts = 0L;
                    var ctrPurchases = 0L;
                    var viewToCartPct = 0.0;
                    var cartToPurchasePct = 0.0;
                    var overallCtrPct = 0.0;
                    if (ctrResult?.PhraseCtr.TryGetValue(phrase, out var ctrData) == true)
                    {
                        ctrViews = ctrData.Views;
                        ctrCarts = ctrData.Carts;
                        ctrPurchases = ctrData.Purchases;
                        viewToCartPct = ctrViews > 0 ? (double)ctrCarts / ctrViews * 100 : 0;
                        cartToPurchasePct = ctrCarts > 0 ? (double)ctrPurchases / ctrCarts * 100 : 0;
                        overallCtrPct = ctrViews > 0 ? (double)ctrPurchases / ctrViews * 100 : 0;
                    }

                    var priceAvg = 0m;
                    var priceVsCategory = 1.0m;
                    if (priceResult?.PhrasePriceCompetitiveness.TryGetValue(phrase, out var priceComp) == true)
                    {
                        priceAvg = priceComp.AvgPrice;
                        priceVsCategory = priceComp.PriceVsCategoryAvg;
                    }

                    var imageAvg = 0.0;
                    var imageCount = 0;
                    if (imageResult?.PhraseImages.TryGetValue(phrase, out var imgData) == true)
                    {
                        imageAvg = imgData.ProductCount > 0 ? (double)imgData.TotalImages / imgData.ProductCount : 0;
                        imageCount = imgData.ProductCount;
                    }

                    var overallScore = CalculateOverallScore(sentimentPct, salesVolume, salesCount, ctrLift, overallCtrPct, priceVsCategory);

                    var line = $"\"{category}\",\"{phrase}\",{overallScore:F4}," +
                        $"{sentimentPct:F1},{sentPos},{sentTotal}," +
                        $"{salesVolume},{salesCount},{(salesCount > 0 ? salesVolume / (double)salesCount : 0):F1}," +
                        $"{topFreq},{bottomFreq},{topBottomRatio:F2},{ctrLift:F1}," +
                        $"{ctrViews},{ctrCarts},{ctrPurchases},{viewToCartPct:F2},{cartToPurchasePct:F2},{overallCtrPct:F3}," +
                        $"{priceAvg:F2},{priceVsCategory:F3}," +
                        $"{imageAvg:F1},{imageCount}";

                    categoryPhraseRows.Add((overallScore, line));
                }

                // Emit only top 25% by OverallScore
                var top25PhraseCount = (int)Math.Ceiling(categoryPhraseRows.Count * 0.25);
                var top25PhraseRows = categoryPhraseRows.OrderByDescending(r => r.score).Take(top25PhraseCount).ToList();
                lock (phraseWriterLock)
                {
                    foreach (var (_, line) in top25PhraseRows)
                        writer.WriteLine(line);
                }
            });

            Console.WriteLine($"Phrase pivot table written to: {outputPath}");
        }

        public static void GenerateColorPivotTable(
            Dictionary<string, ColorStatResult> colorResults,
            string outputDir)
        {
            var outputPath = Path.Combine(outputDir, "color_pivot_table.csv");
            using var writer = new StreamWriter(outputPath);

            writer.WriteLine("Category,Color,Count,AvgSentiment,TotalSales," +
                "SentimentRank,SalesRank,FrequencyRank,OverallScore");

            var colorWriterLock = new object();

            Parallel.ForEach(colorResults.Keys, category =>
            {
                var result = colorResults[category];

                var colorData = result.ColorStats
                    .Where(kv => kv.Value.Count >= 10)
                    .Select(kv => new
                    {
                        Color = kv.Key,
                        Count = kv.Value.Count,
                        AvgSentiment = kv.Value.AvgSentiment,
                        TotalSales = kv.Value.TotalSales
                    })
                    .ToList();

                var sentimentRanks = colorData.OrderByDescending(x => x.AvgSentiment)
                    .Select((x, idx) => new { x.Color, Rank = idx + 1 })
                    .ToDictionary(p => p.Color, p => p.Rank);

                var salesRanks = colorData.OrderByDescending(x => x.TotalSales)
                    .Select((x, idx) => new { x.Color, Rank = idx + 1 })
                    .ToDictionary(p => p.Color, p => p.Rank);

                var freqRanks = colorData.OrderByDescending(x => x.Count)
                    .Select((x, idx) => new { x.Color, Rank = idx + 1 })
                    .ToDictionary(p => p.Color, p => p.Rank);

                var categoryColorLines = new List<string>();
                foreach (var data in colorData)
                {
                    var score = CalculateOverallScore(
                        data.AvgSentiment / 1.0 * 100,
                        data.TotalSales,
                        data.Count,
                        0, 0, 1.0m);

                    categoryColorLines.Add($"\"{category}\",\"{data.Color}\",{data.Count}," +
                        $"{data.AvgSentiment:F1},{data.TotalSales:N0}," +
                        $"{sentimentRanks[data.Color]},{salesRanks[data.Color]},{freqRanks[data.Color]}," +
                        $"{score:F4}");
                }
                lock (colorWriterLock)
                {
                    foreach (var line in categoryColorLines)
                        writer.WriteLine(line);
                }
            });

            Console.WriteLine($"Color pivot table written to: {outputPath}");
        }

        public static void GenerateMaterialPivotTable(
            Dictionary<string, MaterialStatResult> materialResults,
            string outputDir)
        {
            var outputPath = Path.Combine(outputDir, "material_pivot_table.csv");
            using var writer = new StreamWriter(outputPath);

            writer.WriteLine("Category,Material,Count,AvgSentiment,TotalSales," +
                "SentimentRank,SalesRank,OverallScore");

            var materialWriterLock = new object();

            Parallel.ForEach(materialResults.Keys, category =>
            {
                var result = materialResults[category];

                var materialData = result.MaterialStats
                    .Where(kv => kv.Value.Count >= 10)
                    .Select(kv => new
                    {
                        Material = kv.Key,
                        Count = kv.Value.Count,
                        AvgSentiment = kv.Value.AvgSentiment,
                        TotalSales = kv.Value.TotalSales
                    })
                    .ToList();

                var sentimentRanks = materialData.OrderByDescending(x => x.AvgSentiment)
                    .Select((x, idx) => new { x.Material, Rank = idx + 1 })
                    .ToDictionary(p => p.Material, p => p.Rank);

                var salesRanks = materialData.OrderByDescending(x => x.TotalSales)
                    .Select((x, idx) => new { x.Material, Rank = idx + 1 })
                    .ToDictionary(p => p.Material, p => p.Rank);

                var categoryMaterialLines = new List<string>();
                foreach (var data in materialData)
                {
                    var score = CalculateOverallScore(
                        data.AvgSentiment / 1.0 * 100,
                        data.TotalSales,
                        data.Count,
                        0, 0, 1.0m);

                    categoryMaterialLines.Add($"\"{category}\",\"{data.Material}\",{data.Count}," +
                        $"{data.AvgSentiment:F1},{data.TotalSales:N0}," +
                        $"{sentimentRanks[data.Material]},{salesRanks[data.Material]}," +
                        $"{score:F4}");
                }
                lock (materialWriterLock)
                {
                    foreach (var line in categoryMaterialLines)
                        writer.WriteLine(line);
                }
            });

            Console.WriteLine($"Material pivot table written to: {outputPath}");
        }

        public static void GenerateBrandPivotTable(
            Dictionary<string, BrandStatResult> brandResults,
            string outputDir)
        {
            var outputPath = Path.Combine(outputDir, "brand_pivot_table.csv");
            using var writer = new StreamWriter(outputPath);

            writer.WriteLine("Category,Brand,Count,AvgSentiment,TotalSales," +
                "TopCount,BottomCount,TopPct,OverallScore");

            var brandWriterLock = new object();

            Parallel.ForEach(brandResults.Keys, category =>
            {
                var result = brandResults[category];

                var brandData = result.BrandStats
                    .Where(kv => kv.Value.Count >= 10)
                    .Select(kv => new
                    {
                        Brand = kv.Key,
                        Count = kv.Value.Count,
                        AvgSentiment = kv.Value.AvgSentiment,
                        TotalSales = kv.Value.TotalSales,
                        TopCount = kv.Value.TopCount,
                        BottomCount = kv.Value.BottomCount,
                        TopPct = kv.Value.Count > 0 ? (double)kv.Value.TopCount / kv.Value.Count * 100 : 0
                    })
                    .OrderByDescending(x => x.AvgSentiment)
                    .ToList();

                var categoryBrandLines = new List<string>();
                foreach (var data in brandData)
                {
                    var score = CalculateOverallScore(
                        data.AvgSentiment / 1.0 * 100,
                        data.TotalSales,
                        data.Count,
                        0, 0, 1.0m);

                    categoryBrandLines.Add($"\"{category}\",\"{data.Brand}\",{data.Count}," +
                        $"{data.AvgSentiment:F1},{data.TotalSales:N0}," +
                        $"{data.TopCount},{data.BottomCount},{data.TopPct:F1}," +
                        $"{score:F4}");
                }
                lock (brandWriterLock)
                {
                    foreach (var line in categoryBrandLines)
                        writer.WriteLine(line);
                }
            });

            Console.WriteLine($"Brand pivot table written to: {outputPath}");
        }

        private static double CalculateOverallScore(
            double sentimentPct, long salesVolume, int count,
            double ctrLift, double overallCtr, decimal priceVsCategory)
        {
            // Normalize metrics to 0-1 scale
            var sentimentScore = sentimentPct / 100.0;
            var salesScore = Math.Min(salesVolume / 1000000.0, 1.0);
            var countScore = Math.Min(count / 1000.0, 1.0);
            var ctrScore = Math.Min(overallCtr / 10.0, 1.0);
            var priceScore = priceVsCategory < 1.0m ? 1.0 : (double)(1.0m / priceVsCategory);

            // Weighted average
            return (sentimentScore * 0.3 + salesScore * 0.25 + countScore * 0.15 + 
                   ctrScore * 0.2 + priceScore * 0.1);
        }

        private static bool IsColor(string word)
        {
            var colors = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "red", "blue", "green", "yellow", "orange", "purple", "pink", "black", "white", "gray", "grey",
                "brown", "navy", "teal", "cyan", "magenta", "maroon", "olive", "lime", "aqua",
                "gold", "silver", "bronze", "copper", "beige", "tan", "ivory", "cream", "khaki",
                "coral", "salmon", "peach", "plum", "lavender", "mint", "rose", "wine", "burgundy",
                "turquoise", "indigo", "violet", "fuchsia", "crimson", "scarlet", "azure"
            };
            return colors.Contains(word);
        }

        private static bool IsMaterial(string word)
        {
            var materials = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "cotton", "polyester", "silk", "wool", "linen", "denim", "leather", "suede", "velvet",
                "satin", "chiffon", "nylon", "spandex", "elastane", "lycra", "rayon", "viscose",
                "canvas", "jersey", "fleece", "flannel", "tweed", "cashmere", "mohair", "alpaca",
                "synthetic", "blend", "mesh", "lace", "knit", "crochet", "ribbed", "quilted"
            };
            return materials.Contains(word);
        }

        private static bool IsBrand(string word)
        {
            // Brand detection would need a more comprehensive list or external data
            return false;
        }
    }
}
