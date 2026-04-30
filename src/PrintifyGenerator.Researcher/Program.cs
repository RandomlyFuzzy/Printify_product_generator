using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using PrintifyGenerator.Researcher.Datasets;
using PrintifyGenerator.Researcher.Interfaces;
using PrintifyGenerator.Researcher.StatCalculators;

class Program
{
    const int CHUNK = 50000;
    const int MIN_WORD_COUNT = 50;
    const int PRODUCT_LIMIT = 5000000;
    const int QUICK_TEST_LIMIT = 100000;

    static string? exePath;

    static readonly HashSet<string> Connectives = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        "and", "or", "but", "nor", "yet", "so",
        "for", "with", "without", "plus", "minus",
        "vs", "versus", "to", "from", "between"
    };

    static async Task Main()
    {
        var process = Process.GetCurrentProcess();
        exePath = process.MainModule?.FileName ??
                  System.Reflection.Assembly.GetExecutingAssembly().Location;

        Console.WriteLine("LOADING LISTING WORDS + SENTIMENT + SALES + CTR + PRICE...");

        var productPath = "/home/rf/Desktop/Printify_prodcuct_generator/DataSets/RawData/meta_Clothing_Shoes_and_Jewelry.jsonl";
        var reviewPath = "/home/rf/Desktop/Printify_prodcuct_generator/DataSets/RawData/Clothing_Shoes_and_Jewelry.jsonl";

        var stopWords = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "the", "a", "an", "is", "are", "was", "were", "be", "been", "being",
            "have", "has", "had", "do", "does", "did", "will", "would", "could", "should",
            "may", "might", "must", "shall", "can", "need", "to", "of", "in", "for",
            "on", "with", "at", "by", "from", "as", "into", "through", "during",
            "before", "after", "above", "below", "between", "under", "again", "further",
            "then", "once", "here", "there", "when", "where", "why", "how", "all",
            "each", "few", "more", "most", "other", "some", "such", "no", "nor",
            "not", "only", "own", "same", "so", "than", "too", "very", "just",
            "and", "but", "if", "or", "because", "until", "while", "about", "this",
            "that", "it", "its", "my", "me", "we", "our", "you", "your", "he",
            "she", "him", "her", "they", "them", "their", "what", "which", "who",
            "i", "am", "get", "got", "one", "two", "also", "back", "any",
            "size", "fit", "color", "brand", "material", "made", "buy", "product",
            "wear", "worn", "pair", "day", "time", "year", "month", "week"
        };

        var outputDir = "/home/rf/Desktop/Printify_prodcuct_generator/category_features";
        Directory.CreateDirectory(outputDir);

        Console.WriteLine("\n=== PHASE 1: SCANNING REVIEWS FOR PRODUCTS WITH REVIEWS ===");
        var (targetAsins, asinToSentiment) = ScanReviewsAndGetSentiment(reviewPath, QUICK_TEST_LIMIT);
        Console.WriteLine($"Found {targetAsins.Count:N0} products with reviews");
        Console.WriteLine($"Calculated sentiment for {asinToSentiment.Count:N0} products");

        var amazonDataset = new AmazonProductDataset(productPath);
        await amazonDataset.LoadAsync();
        amazonDataset.SetSentimentData(asinToSentiment);

        var ctrDataset = new CtrDataset(new[]
        {
            "/home/rf/Desktop/Printify_prodcuct_generator/DataSets/RawData/cleaned-2019-Oct.csv",
            "/home/rf/Desktop/Printify_prodcuct_generator/DataSets/RawData/cleaned-2019-Nov.csv",
            "/home/rf/Desktop/Printify_prodcuct_generator/DataSets/RawData/cleaned-2019-Dec.csv"
        });
        await ctrDataset.LoadAsync();
        ctrDataset.BuildTitleMapping(productPath);

        var datasetRegistry = new DatasetRegistry();
        datasetRegistry.Register(amazonDataset);
        datasetRegistry.Register(ctrDataset);

        Console.WriteLine("\n=== PHASE 2: DETERMINING TOP/BOTTOM SELLERS ===");
        var categoryProducts = new Dictionary<string, List<(string asin, int sales)>>();
        var asinToCategory = amazonDataset.GetAsinData();

        foreach (var kv in asinToCategory)
        {
            var asin = kv.Key;
            var (category, sales, _, _, _, _) = kv.Value;

            if (!targetAsins.Contains(asin)) continue;
            if (sales <= 0) continue;

            if (!categoryProducts.TryGetValue(category, out var list))
                categoryProducts[category] = list = new List<(string, int)>();
            list.Add((asin, sales));
        }

        var topCategories = categoryProducts
            .OrderByDescending(kvp => kvp.Value.Count)
            .Take(10)
            .Select(kvp => kvp.Key)
            .ToList();

        Console.WriteLine($"\nTop categories: {string.Join(", ", topCategories)}");

        var (topAsins, bottomAsins, topCounts, bottomCounts) = DetermineTopBottom(categoryProducts, topCategories);

        var registry = new StatCalculatorRegistry();
        registry.Register(new SentimentStatCalculator());
        registry.Register(new SalesStatCalculator());
        registry.Register(new CtrStatCalculator());
        registry.Register(new PriceStatCalculator());
        registry.Register(new ImageStatCalculator());
        registry.Register(new SeasonalityStatCalculator());
        registry.Register(new ColorStatCalculator());
        registry.Register(new MaterialStatCalculator());
        registry.Register(new BrandStatCalculator());

        foreach (var cat in topCategories)
            registry.InitializeForCategory(cat);

        Console.WriteLine("\n=== PHASE 3: ANALYZING WORDS AND PHRASES (PASS 2) ===");
        await ProcessProducts(productPath, amazonDataset, ctrDataset, registry, topAsins, bottomAsins,
            topCounts, bottomCounts, topCategories, stopWords, targetAsins);

        var priceCalc = registry.GetAll().OfType<PriceStatCalculator>().FirstOrDefault();
        if (priceCalc != null)
        {
            Console.WriteLine("\n=== CALCULATING PRICE COMPETITIVENESS ===");
            priceCalc.CalculateCompetitiveness();
        }

        Console.WriteLine("\n=== RESULTS ===");
        OutputResults(registry, topCategories, categoryProducts, outputDir, topAsins, bottomAsins, topCounts, bottomCounts);
    }

    static (HashSet<string> asins, Dictionary<string, double> sentiment) ScanReviewsAndGetSentiment(string path, int limit = PRODUCT_LIMIT)
    {
        var asinSumCount = new Dictionary<string, (long sum, int count)>();
        int processed = 0;

        foreach (var chunk in StreamLines(path).Chunk(CHUNK))
        {
            processed += chunk.Length;
            if (processed % 1000000 == 0)
                Console.WriteLine($"Scanned {processed:N0} reviews...");

            Parallel.ForEach(chunk, line =>
            {
                if (string.IsNullOrWhiteSpace(line)) return;
                try
                {
                    using var doc = JsonDocument.Parse(line);
                    var root = doc.RootElement;

                    string? asin = null;
                    int overall = 0;

                    if (root.TryGetProperty("parent_asin", out var pa))
                        asin = pa.ValueKind == JsonValueKind.String ? pa.GetString() : null;
                    else if (root.TryGetProperty("asin", out var a))
                        asin = a.ValueKind == JsonValueKind.String ? a.GetString() : null;

                    if (root.TryGetProperty("rating", out var o))
                        overall = o.ValueKind == JsonValueKind.Number ? (int)o.GetDouble() : 0;

                    if (!string.IsNullOrEmpty(asin) && overall > 0)
                    {
                        lock (asinSumCount)
                        {
                            if (!asinSumCount.TryGetValue(asin, out var sc))
                                asinSumCount[asin] = (overall, 1);
                            else
                                asinSumCount[asin] = (sc.sum + overall, sc.count + 1);
                        }
                    }
                }
                catch { }
            });

            if (asinSumCount.Count >= limit)
            {
                Console.WriteLine($"Reached limit of {asinSumCount.Count:N0} products");
                break;
            }
        }

        var targetAsins = new HashSet<string>(asinSumCount.Keys);
        var sentiment = asinSumCount.ToDictionary(kv => kv.Key, kv => (double)kv.Value.sum / kv.Value.count);

        return (targetAsins, sentiment);
    }

    static (HashSet<string> topAsins, HashSet<string> bottomAsins, Dictionary<string, int> topCounts, Dictionary<string, int> bottomCounts)
        DetermineTopBottom(Dictionary<string, List<(string asin, int sales)>> categoryProducts, List<string> topCategories)
    {
        var topAsins = new HashSet<string>();
        var bottomAsins = new HashSet<string>();
        var topCounts = new Dictionary<string, int>();
        var bottomCounts = new Dictionary<string, int>();

        foreach (var category in topCategories)
        {
            if (!categoryProducts.TryGetValue(category, out var products) || products.Count < 10) continue;

            var sortedBySales = products.OrderByDescending(p => p.sales).ToList();
            var topCount = Math.Max(1, (int)(products.Count * 0.1));
            var bottomCount = Math.Max(1, (int)(products.Count * 0.1));

            topCounts[category] = topCount;
            bottomCounts[category] = bottomCount;

            foreach (var (asin, _) in sortedBySales.Take(topCount))
                topAsins.Add(asin);
            foreach (var (asin, _) in sortedBySales.Skip(Math.Max(0, sortedBySales.Count - bottomCount)))
                bottomAsins.Add(asin);
        }

        return (topAsins, bottomAsins, topCounts, bottomCounts);
    }

    static async Task ProcessProducts(string path,
        AmazonProductDataset amazonDataset,
        CtrDataset ctrDataset,
        StatCalculatorRegistry registry,
        HashSet<string> topAsins,
        HashSet<string> bottomAsins,
        Dictionary<string, int> topCounts,
        Dictionary<string, int> bottomCounts,
        List<string> topCategories,
        HashSet<string> stopWords,
        HashSet<string> targetAsins)
    {
        int processed = 0;
        var asinData = amazonDataset.GetAsinData();

        await Task.Run(() =>
        {
            foreach (var line in StreamLines(path))
            {
                processed++;
                if (processed % 100000 == 0)
                    Console.WriteLine($"Pass 2: processed {processed:N0} products...");

                if (string.IsNullOrWhiteSpace(line)) continue;
                try
                {
                    using var doc = JsonDocument.Parse(line);
                    var root = doc.RootElement;

                    string? asin = null;
                    if (root.TryGetProperty("parent_asin", out var pa))
                        asin = pa.ValueKind == JsonValueKind.String ? pa.GetString() : null;
                    else if (root.TryGetProperty("asin", out var a))
                        asin = a.ValueKind == JsonValueKind.String ? a.GetString() : null;

                    if (string.IsNullOrEmpty(asin) || !targetAsins.Contains(asin)) continue;
                    if (!asinData.TryGetValue(asin, out var data)) continue;
                    
                    var (category, sales, price, imageCount, title, brand) = data;
                    if (!topCategories.Contains(category)) continue;

                    var sentiment = amazonDataset.GetSentiment(asin);
                    var isPositive = sentiment >= 4.0;
                    var isTop = topAsins.Contains(asin);
                    var isBottom = bottomAsins.Contains(asin);

                    var context = new ProductContext
                    {
                        Asin = asin,
                        Category = category,
                        Sentiment = sentiment,
                        Sales = sales,
                        IsTopSeller = isTop,
                        IsBottomSeller = isBottom,
                        Price = price,
                        ImageCount = imageCount,
                        ProductJson = root,
                        DatasetData = new Dictionary<string, object>()
                    };

                    if (ctrDataset.HasProductData(asin))
                    {
                        var ctr = ctrDataset.GetProductData<CtrData>(asin);
                        if (ctr != null)
                            context.DatasetData["CTR"] = (ctr.Views, ctr.Carts, ctr.Purchases);
                    }

                    var (words, phrases) = ExtractWordsAndPhrases(root, stopWords);

                    foreach (var word in words)
                    {
                        if (stopWords.Contains(word)) continue;
                        registry.ProcessWordForCategory(word, context);
                    }

                    foreach (var phrase in phrases)
                    {
                        registry.ProcessPhraseForCategory(phrase, context);
                    }

                    if (!string.IsNullOrEmpty(brand))
                    {
                        var brandCalc = registry.GetAll().OfType<BrandStatCalculator>().FirstOrDefault();
                        if (brandCalc != null)
                        {
                            var brandContext = context;
                            brandCalc.ProcessBrand(brand, context);
                        }
                    }
                }
                catch { }
            }
        });
    }

    static IEnumerable<string> StreamLines(string path)
    {
        using var reader = new StreamReader(path);
        while (!reader.EndOfStream)
        {
            var line = reader.ReadLine();
            if (!string.IsNullOrWhiteSpace(line))
                yield return line;
        }
    }

    static (List<string> words, List<string> phrases) ExtractWordsAndPhrases(JsonElement root, HashSet<string> stopWords)
    {
        var words = new List<string>();
        var phrases = new List<string>();

        var textFields = new[] { "title", "description", "features", "brand" };

        foreach (var field in textFields)
        {
            if (root.TryGetProperty(field, out var elem))
            {
                string? text = null;
                if (elem.ValueKind == JsonValueKind.String)
                    text = elem.GetString();
                else if (elem.ValueKind == JsonValueKind.Array)
                {
                    var arr = new List<string>();
                    foreach (var item in elem.EnumerateArray())
                        if (item.ValueKind == JsonValueKind.String)
                            arr.Add(item.GetString()!);
                    text = string.Join(" ", arr);
                }

                if (string.IsNullOrEmpty(text)) continue;

                text = text.ToLower();
                text = Regex.Replace(text, @"[^a-z\s]", " ");

                var tokens = text.Split(' ', StringSplitOptions.RemoveEmptyEntries);

                var segments = new List<List<string>>();
                var currentSegment = new List<string>();
                foreach (var token in tokens)
                {
                    if (Connectives.Contains(token))
                    {
                        if (currentSegment.Count > 0)
                        {
                            segments.Add(currentSegment);
                            currentSegment = new List<string>();
                        }
                        continue;
                    }
                    currentSegment.Add(token);
                }
                if (currentSegment.Count > 0)
                    segments.Add(currentSegment);

                foreach (var segment in segments)
                {
                    var cleanedTokens = segment
                        .Where(t => !string.IsNullOrEmpty(t) && t.Length >= 3 && t.All(c => char.IsLetter(c)))
                        .ToList();

                    words.AddRange(cleanedTokens);

                    for (int i = 0; i < cleanedTokens.Count; i++)
                    {
                        if (i < cleanedTokens.Count - 1)
                            phrases.Add($"{cleanedTokens[i]} {cleanedTokens[i + 1]}");
                        if (i < cleanedTokens.Count - 2)
                            phrases.Add($"{cleanedTokens[i]} {cleanedTokens[i + 1]} {cleanedTokens[i + 2]}");
                        if (i < cleanedTokens.Count - 3)
                            phrases.Add($"{cleanedTokens[i]} {cleanedTokens[i + 1]} {cleanedTokens[i + 2]} {cleanedTokens[i + 3]}");
                        if (i < cleanedTokens.Count - 4)
                            phrases.Add($"{cleanedTokens[i]} {cleanedTokens[i + 1]} {cleanedTokens[i + 2]} {cleanedTokens[i + 3]} {cleanedTokens[i + 4]}");
                    }
                }
            }
        }

        return (words, phrases);
    }

    static void OutputResults(StatCalculatorRegistry registry,
        List<string> topCategories,
        Dictionary<string, List<(string asin, int sales)>> categoryProducts,
        string outputDir,
        HashSet<string> topAsins,
        HashSet<string> bottomAsins,
        Dictionary<string, int> topCounts,
        Dictionary<string, int> bottomCounts)
    {
        var additionalData = new Dictionary<string, object>
        {
            ["topCounts"] = topCounts,
            ["bottomCounts"] = bottomCounts,
            ["topAsins"] = topAsins,
            ["bottomAsins"] = bottomAsins
        };

        foreach (var category in topCategories)
        {
            if (!categoryProducts.TryGetValue(category, out var products) || products.Count < 10) continue;

            var totalCount = products.Count;
            Console.WriteLine($"\n=== {category.ToUpper()} ({totalCount:N0} products) ===");

            var safeName = string.Join("_", category.Split(Path.GetInvalidFileNameChars()));
            var filePath = Path.Combine(outputDir, $"{safeName}.txt");
            using var writer = new StreamWriter(filePath);
            writer.WriteLine($"=== {category.ToUpper()} ({totalCount:N0} products) ===");
            writer.WriteLine();

            registry.WriteResultsForCategory(category, writer, additionalData);
        }
    }
}
