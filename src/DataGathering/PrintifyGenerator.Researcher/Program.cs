using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using PrintifyGenerator.Researcher.Datasets;
using PrintifyGenerator.Researcher.Interfaces;
using PrintifyGenerator.Researcher.Models;
using PrintifyGenerator.Researcher.StatCalculators;

using JsonElement = System.Text.Json.JsonElement;

namespace PrintifyGenerator.Researcher
{
    class Program
    {
        const int CHUNK = 50000;
        const int MIN_WORD_COUNT = 50;
        const int PRODUCT_LIMIT = 0; // 0 = no limit
        const int QUICK_TEST_LIMIT = 0; // 0 = no limit, use all data

        private static readonly Regex CleanTextRegex = new Regex(@"[^a-z\s]", RegexOptions.Compiled);

        static readonly HashSet<string> Connectives = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "and", "or", "but", "nor", "yet", "so",
            "for", "with", "without", "plus", "minus",
            "vs", "versus", "to", "from", "between"
        };

        static async Task Main()
        {
            var process = Process.GetCurrentProcess();
            var exePath = process.MainModule?.FileName ??
                          System.Reflection.Assembly.GetExecutingAssembly().Location;

            Console.WriteLine("LOADING LISTING WORDS + SENTIMENT + SALES + CTR + PRICE...");

            var amazonRoot = "/home/rf/Desktop/Printify_prodcuct_generator/DataSets/RawData/Amazon-Reviews-2023";
            var (productPaths, reviewPaths) = ResolveAmazonCategoryPaths(amazonRoot);

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
            var (targetAsins, asinToSentiment) = ScanReviewsAndGetSentiment(reviewPaths, QUICK_TEST_LIMIT);
            Console.WriteLine($"Found {targetAsins.Count:N0} products with reviews");
            Console.WriteLine($"Calculated sentiment for {asinToSentiment.Count:N0} products");

            var amazonDataset = new Datasets.AmazonProductDataset(productPaths);
            await amazonDataset.LoadAsync();
            amazonDataset.SetSentimentData(asinToSentiment);

            var ctrDataset = new Datasets.EcommerceBehaviorDataset(new[]
            {
                "/home/rf/Desktop/Printify_prodcuct_generator/DataSets/RawData/cleaned-2019-Oct.csv",
                "/home/rf/Desktop/Printify_prodcuct_generator/DataSets/RawData/cleaned-2019-Nov.csv",
                "/home/rf/Desktop/Printify_prodcuct_generator/DataSets/RawData/cleaned-2019-Dec.csv"
            });
            await ctrDataset.LoadAsync();
            foreach (var productPath in productPaths)
                ctrDataset.BuildTitleMapping(productPath);

            var ebayDataset = new Datasets.EbayProductDataset(new[]
            {
                "/home/rf/Desktop/Printify_prodcuct_generator/DataSets/RawData/ebay_mens_clothing_600_products.csv",
                "/home/rf/Desktop/Printify_prodcuct_generator/DataSets/ebay_productlisting/ebay__20210401_20210430__30k_data(1).csv"
            });
            await ebayDataset.LoadAsync();

            var womenClothingDataset = new Datasets.WomenClothingSalesDataset(
                "/home/rf/Desktop/Printify_prodcuct_generator/DataSets/RawData/women_clothing_ecommerce_sales.csv");
            await womenClothingDataset.LoadAsync();

            var onlineSalesDataset = new Datasets.OnlineSalesDataset(
                "/home/rf/Desktop/Printify_prodcuct_generator/DataSets/RawData/Online Sales Data.csv");
            await onlineSalesDataset.LoadAsync();

            var datasetRegistry = new DatasetRegistry();
            datasetRegistry.Register(amazonDataset);
            datasetRegistry.Register(ctrDataset);
            datasetRegistry.Register(ebayDataset);
            datasetRegistry.Register(womenClothingDataset);
            datasetRegistry.Register(onlineSalesDataset);

            Console.WriteLine("\n=== PHASE 2: DETERMINING TOP/BOTTOM SELLERS (ENHANCED CATEGORIZATION) ===");

            // Build comprehensive product data dictionary
            var asinData = amazonDataset.GetAllProductData();
            var productDataDict = new Dictionary<string, (string category, int sales, string title, string brand, decimal price)>();

            // Use targetAsins for categorization (products with reviews)
            foreach (var asin in targetAsins)
            {
                if (!asinData.TryGetValue(asin, out var data)) continue;
                if (data.Sales <= 0) continue;
                productDataDict[asin] = (data.Category, data.Sales, data.Title, data.Brand, data.Price);
            }

            Console.WriteLine($"Built product data dict with {productDataDict.Count} products (from targetAsins) for categorization");

            // Create and use the enhanced categorizer
            var categorizer = new ProductCategorizer(stopWords);
            foreach (var kv in productDataDict)
            {
                if (asinData.TryGetValue(kv.Key, out var data))
                    categorizer.IndexProduct(kv.Key, kv.Value.title, kv.Value.brand, kv.Value.price, data.Color, data.Material);
                else
                    categorizer.IndexProduct(kv.Key, kv.Value.title, kv.Value.brand, kv.Value.price, "", "");
            }

            // Get all category groupings (use lower threshold for testing with QUICK_TEST_LIMIT)
            int minCatSize = QUICK_TEST_LIMIT <= 1000 ? 5 : 10;
            var allCategoryGroups = categorizer.CategorizeProducts(productDataDict, minProductsPerCategory: minCatSize);
            Console.WriteLine($"\nFound {allCategoryGroups.Count} category groupings (min {minCatSize} products per category):");
            foreach (var grp in allCategoryGroups.GroupBy(g => g.Type))
                Console.WriteLine($"  {grp.Key}: {grp.Count()} categories");

            // Build lookup dictionaries for all category types
            var categoryProducts = allCategoryGroups
                .Where(g => g.Type == CategoryType.MainCategory)
                .ToDictionary(g => g.CategoryName, g => g.Products);

            var topCategories = categoryProducts
                .OrderByDescending(kvp => kvp.Value.Count)
                .Take(15)
                .Select(kvp => kvp.Key)
                .ToList();

            Console.WriteLine($"\nMain top categories: {string.Join(", ", topCategories)}");

            // Build phrase lookup for phrase cluster categories
            var productPhrases = new Dictionary<string, HashSet<string>>();
            // (phrases will be populated during ProcessProducts)

            // Determine top/bottom for main categories
            var (topAsins, bottomAsins, topCounts, bottomCounts) = DetermineTopBottom(categoryProducts, topCategories);

            // Store all category groups for later use
            var allCategoryInfos = allCategoryGroups;

            // Build product-to-categories mapping for processing
            var productToCategories = new Dictionary<string, List<string>>();
            foreach (var catInfo in allCategoryInfos)
            {
                foreach (var (asin, _) in catInfo.Products)
                {
                    if (!productToCategories.TryGetValue(asin, out var cats))
                        productToCategories[asin] = cats = new List<string>();
                    if (!cats.Contains(catInfo.CategoryName))
                        cats.Add(catInfo.CategoryName);
                }
            }
            Console.WriteLine($"Built product-to-categories mapping for {productToCategories.Count} products");

            var registry = new StatCalculatorRegistry();
            var sentimentCalc = new SentimentStatCalculator();
            var salesCalc = new SalesStatCalculator();
            var ctrCalc = new CtrStatCalculator();
            var priceCalc = new PriceStatCalculator();
            var imageCalc = new ImageStatCalculator();
            var seasonalityCalc = new SeasonalityStatCalculator();
            var colorCalc = new ColorStatCalculator();
            var materialCalc = new MaterialStatCalculator();
            var brandCalc = new BrandStatCalculator();

            registry.Register(sentimentCalc);
            registry.Register(salesCalc);
            registry.Register(ctrCalc);
            registry.Register(priceCalc);
            registry.Register(imageCalc);
            registry.Register(seasonalityCalc);
            registry.Register(colorCalc);
            registry.Register(materialCalc);
            registry.Register(brandCalc);

            // Initialize stat calculators for ALL category groups (not just main categories)
            var allCategoryNames = allCategoryInfos.Select(g => g.CategoryName).Distinct().ToList();
            foreach (var cat in allCategoryNames)
                registry.InitializeForCategory(cat);

            Console.WriteLine($"\nInitialized stat calculators for {allCategoryNames.Count} total categories");

            Console.WriteLine("\n=== PHASE 3: ANALYZING WORDS AND PHRASES (PASS 2) ===");
            Console.WriteLine($"Processing {targetAsins.Count} products for analysis...");

            // Track phrases for all products (needed for phrase clusters)
            var allProductPhrases = new Dictionary<string, HashSet<string>>();

            await ProcessProducts(productPaths, amazonDataset, ctrDataset, ebayDataset, womenClothingDataset, onlineSalesDataset,
                registry, topAsins, bottomAsins, topCounts, bottomCounts, allCategoryNames, stopWords, targetAsins, allProductPhrases, productToCategories);

            Console.WriteLine("Phase 3 complete!");

            // Now create phrase cluster categories
            Console.WriteLine("\n=== CREATING PHRASE CLUSTER CATEGORIES ===");
            var phraseClusters = categorizer.CreatePhraseClusters(productDataDict, allProductPhrases, minClusterSize: 20);
            allCategoryInfos.AddRange(phraseClusters);
            Console.WriteLine($"Added {phraseClusters.Count} phrase cluster categories");

            // Initialize for phrase cluster categories too
            foreach (var c in phraseClusters.Select(c => c.CategoryName).Where(c => !allCategoryNames.Contains(c)))
            {
                registry.InitializeForCategory(c);
                allCategoryNames.Add(c);
            }

            // Product-type categories: garment type + modifier combos (e.g. "Short Sleeve T-Shirt", "Unisex Hoodie")
            Console.WriteLine("\n=== CREATING PRODUCT TYPE CATEGORIES ===");
            var productTypeCategories = categorizer.CreateProductTypeCategories(productDataDict, minProducts: 10);
            allCategoryInfos.AddRange(productTypeCategories);
            Console.WriteLine($"Added {productTypeCategories.Count} product type categories");

            foreach (var c in productTypeCategories.Select(c => c.CategoryName).Where(c => !allCategoryNames.Contains(c)))
            {
                registry.InitializeForCategory(c);
                allCategoryNames.Add(c);
            }

            // Re-process products for the new product-type categories so they accumulate word/phrase stats.
            Console.WriteLine("Processing product-type categories for stats...");
            var productTypeCategoryNames = productTypeCategories.Select(c => c.CategoryName).ToList();
            var productTypeProductToCategories = new Dictionary<string, List<string>>();
            foreach (var catInfo in productTypeCategories)
            {
                foreach (var (asin, _) in catInfo.Products)
                {
                    if (!productTypeProductToCategories.TryGetValue(asin, out var cats))
                        productTypeProductToCategories[asin] = cats = new List<string>();
                    if (!cats.Contains(catInfo.CategoryName))
                        cats.Add(catInfo.CategoryName);
                }
            }
            await ProcessProducts(productPaths, amazonDataset, ctrDataset, ebayDataset, womenClothingDataset, onlineSalesDataset,
                registry, topAsins, bottomAsins, topCounts, bottomCounts, productTypeCategoryNames, stopWords,
                new HashSet<string>(productTypeProductToCategories.Keys), allProductPhrases, productTypeProductToCategories);

            Console.WriteLine("\n=== CALCULATING PRICE COMPETITIVENESS ===");
            priceCalc.CalculateCompetitiveness();

            Console.WriteLine("\n=== GENERATING PIVOT TABLES ===");
            var sentimentResults = (Dictionary<string, SentimentStatResult>)sentimentCalc.GetResults();
            var salesResults = (Dictionary<string, SalesStatResult>)salesCalc.GetResults();
            var ctrResults = (Dictionary<string, CtrStatResult>)ctrCalc.GetResults();
            var priceResults = (Dictionary<string, PriceStatResult>)priceCalc.GetResults();
            var imageResults = (Dictionary<string, ImageStatResult>)imageCalc.GetResults();
            var seasonalityResults = (Dictionary<string, SeasonalityStatResult>)seasonalityCalc.GetResults();
            var colorResults = (Dictionary<string, ColorStatResult>)colorCalc.GetResults();
            var materialResults = (Dictionary<string, MaterialStatResult>)materialCalc.GetResults();
            var brandResults = (Dictionary<string, BrandStatResult>)brandCalc.GetResults();

            // Build comprehensive category mapping for ALL category groups
            var allCategoryProducts = new Dictionary<string, List<(string asin, int sales)>>();
            foreach (var catInfo in allCategoryInfos)
            {
                allCategoryProducts[catInfo.CategoryName] = catInfo.Products;
            }

            // Also include main categories
            foreach (var kv in categoryProducts)
            {
                if (!allCategoryProducts.ContainsKey(kv.Key))
                    allCategoryProducts[kv.Key] = kv.Value;
            }

            Parallel.Invoke(
                () => PivotTableGenerator.GenerateWordPivotTable(
                    sentimentResults, salesResults, ctrResults, priceResults,
                    imageResults, seasonalityResults, colorResults, materialResults, brandResults,
                    outputDir, topCounts, bottomCounts, allCategoryProducts),
                () => PivotTableGenerator.GeneratePhrasePivotTable(
                    sentimentResults, salesResults, ctrResults, priceResults,
                    imageResults, outputDir, topCounts, bottomCounts),
                () => PivotTableGenerator.GenerateColorPivotTable(colorResults, outputDir),
                () => PivotTableGenerator.GenerateMaterialPivotTable(materialResults, outputDir),
                () => PivotTableGenerator.GenerateBrandPivotTable(brandResults, outputDir)
            );

            // Generate summary of all category groups
            Console.WriteLine("\n=== CATEGORY GROUPS SUMMARY ===");
            foreach (var typeGroup in allCategoryInfos.GroupBy(c => c.Type))
            {
                Console.WriteLine($"\n{typeGroup.Key} ({typeGroup.Count()} categories):");
                foreach (var cat in typeGroup.OrderByDescending(c => c.Products.Count).Take(10))
                    Console.WriteLine($"  {cat.CategoryName}: {cat.Products.Count} products");
            }

            Console.WriteLine("\n=== TRAINING PREDICTION ENGINE ===");
            var predictionEngine = new PredictionEngine();
            predictionEngine.Train(sentimentResults, salesResults, ctrResults, priceResults,
                imageResults, seasonalityResults, colorResults, materialResults, brandResults,
                allCategoryProducts);

            // Generate prediction examples
            Console.WriteLine("\n=== GENERATING PREDICTIONS ===");
            var examples = new[]
            {
                ("Women's Summer Floral Maxi Dress Sleeveless", "Perfect floral maxi dress for summer beach vacation. Lightweight and comfortable."),
                ("Men's Classic Fit Cotton T-Shirt 3-Pack", "Soft cotton t-shirts in assorted colors. Machine wash cold."),
                ("Premium Leather Hiking Boots Waterproof", "Genuine leather hiking boots with waterproof membrane. Durable outsole."),
                ("Kids' Cartoon Print Hoodie Sweatshirt", "Fun cartoon print pullover hoodie for kids. Soft fleece lining."),
                ("Silk Scarf Floral Print Large Square", "100% mulberry silk scarf with vibrant floral pattern. Hand-rolled edges.")
            };

            var predictionPath = Path.Combine(outputDir, "predictions.csv");
            using (var writer = new StreamWriter(predictionPath))
            {
                writer.WriteLine("Title,OverallScore,SentimentPrediction,SalesPrediction,CtrPrediction,TopPositiveWords,RecommendedColors,RecommendedMaterials,TopNegativeWords,Warnings");
                foreach (var (title, desc) in examples)
                {
                    var pred = predictionEngine.Predict(title, desc);
                    writer.WriteLine($"\"{pred.Title}\",{pred.OverallScore:F4},{pred.SentimentPrediction:F4},{pred.SalesPrediction:F4},{pred.CtrPrediction:F4},\"{pred.TopPositiveWords}\",\"{pred.RecommendedColors}\",\"{pred.RecommendedMaterials}\",\"{pred.TopNegativeWords}\",\"{string.Join("; ", pred.Warnings)}\"");
                    Console.WriteLine($"  Predicted: {pred.Title} => Score: {pred.OverallScore:F4}");
                }
            }
            Console.WriteLine($"Predictions saved to: {predictionPath}");

            Console.WriteLine("\n=== RESULTS ===");
            OutputResults(registry, topCategories, categoryProducts, outputDir, topAsins, bottomAsins, topCounts, bottomCounts, allCategoryInfos);
        }

        static (HashSet<string> asins, Dictionary<string, double> sentiment) ScanReviewsAndGetSentiment(IEnumerable<string> reviewPaths, int limit = PRODUCT_LIMIT)
        {
            var asinSumCount = new ConcurrentDictionary<string, (long sum, int count)>();
            int processed = 0;

            foreach (var reviewPath in reviewPaths)
            {
                Console.WriteLine($"Scanning reviews: {Path.GetFileName(reviewPath)}");
                foreach (var chunk in StreamLines(reviewPath).Chunk(CHUNK))
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
                                asinSumCount.AddOrUpdate(asin, (overall, 1),
                                    (_, sc) => (sc.sum + overall, sc.count + 1));
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

                if (asinSumCount.Count >= limit)
                    break;
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

        static void ProcessProductForCategory(string asin, string category,
            AmazonProductDataset.ProductData data,
            Datasets.AmazonProductDataset amazonDataset,
            Datasets.EcommerceBehaviorDataset ctrDataset,
            Datasets.EbayProductDataset ebayDataset,
            HashSet<string> stopWords,
            StatCalculatorRegistry registry,
            HashSet<string> topAsins,
            HashSet<string> bottomAsins,
            JsonElement root,
            Dictionary<string, HashSet<string>>? allProductPhrases = null)
        {
            var title = data.Title;
            var sales = data.Sales;
            var price = data.Price;
            var imageCount = data.ImageCount;
            var brand = data.Brand;
            var color = data.Color;
            var material = data.Material;

            // Extract and store phrases for phrase clustering
            if (allProductPhrases != null && !string.IsNullOrEmpty(title))
            {
                var extractedPhrases = ExtractPhrases(title, stopWords);
                allProductPhrases[asin] = extractedPhrases;
            }

            var sentiment = amazonDataset.GetSentiment(asin);
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

            // Add CTR data if available
            if (ctrDataset.HasProductData(asin))
            {
                var ctr = ctrDataset.GetProductData<Datasets.EcommerceBehaviorDataset.BehaviorData>(asin);
                if (ctr != null)
                    context.DatasetData["CTR"] = (ctr.Views, ctr.Carts, ctr.Purchases);
            }

            // Add eBay data if available (by title match)
            if (!string.IsNullOrEmpty(title) && ebayDataset.HasProductData(title))
            {
                var ebayData = ebayDataset.GetProductData<Datasets.EbayProductDataset.EbayProductData>(title);
                if (ebayData != null)
                {
                    context.DatasetData["Ebay"] = ebayData;
                }
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
                registry.ProcessBrandForCategory(brand, context);

            if (!string.IsNullOrEmpty(color))
                registry.ProcessColorForCategory(color, context);

            if (!string.IsNullOrEmpty(material))
                registry.ProcessMaterialForCategory(material, context);
        }

        static async Task ProcessProducts(IEnumerable<string> productPaths,
            Datasets.AmazonProductDataset amazonDataset,
            Datasets.EcommerceBehaviorDataset ctrDataset,
            Datasets.EbayProductDataset ebayDataset,
            Datasets.WomenClothingSalesDataset womenClothingDataset,
            Datasets.OnlineSalesDataset onlineSalesDataset,
            StatCalculatorRegistry registry,
            HashSet<string> topAsins,
            HashSet<string> bottomAsins,
            Dictionary<string, int> topCounts,
            Dictionary<string, int> bottomCounts,
            List<string> topCategories,
            HashSet<string> stopWords,
            HashSet<string> targetAsins,
            Dictionary<string, HashSet<string>>? allProductPhrases = null,
            Dictionary<string, List<string>>? productToCategories = null)
        {
            int processed = 0;
            var asinData = amazonDataset.GetAllProductData();
            var topCategoriesSet = new HashSet<string>(topCategories);

            await Task.Run(() =>
            {
                foreach (var productPath in productPaths)
                {
                    Console.WriteLine($"Pass 2: reading {Path.GetFileName(productPath)}...");
                    foreach (var line in StreamLines(productPath))
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

                            var category = data.Category;
                            var sales = data.Sales;
                            var price = data.Price;
                            var imageCount = data.ImageCount;
                            var title = data.Title;
                            var brand = data.Brand;

                            // Process for all categories this product belongs to
                            if (productToCategories != null && productToCategories.TryGetValue(asin, out var productCats))
                            {
                                foreach (var cat in productCats)
                                {
                                    ProcessProductForCategory(asin, cat, data, amazonDataset, ctrDataset, ebayDataset,
                                        stopWords, registry, topAsins, bottomAsins, root, allProductPhrases);
                                }
                            }
                            else
                            {
                                // Fallback: process only for main category
                                if (!topCategoriesSet.Contains(category)) continue;
                                ProcessProductForCategory(asin, category, data, amazonDataset, ctrDataset, ebayDataset,
                                    stopWords, registry, topAsins, bottomAsins, root, allProductPhrases);
                            }
                        }
                        catch { }
                    }
                }
            });

            // Process eBay dataset products for cross-dataset stats
            Console.WriteLine("Processing eBay dataset for additional stats...");
            await ProcessEbayDataset(ebayDataset, registry, stopWords);

            // Process Women Clothing Sales dataset
            Console.WriteLine("Processing Women Clothing Sales dataset...");
            await ProcessWomenClothingDataset(womenClothingDataset, registry, stopWords);

            // Process Online Sales dataset
            Console.WriteLine("Processing Online Sales dataset...");
            await ProcessOnlineSalesDataset(onlineSalesDataset, registry, stopWords);
        }

        static async Task ProcessEbayDataset(Datasets.EbayProductDataset ebayDataset,
            StatCalculatorRegistry registry, HashSet<string> stopWords)
        {
            await Task.Run(() =>
            {
                var allData = ebayDataset.GetCategoryProducts();
                foreach (var kv in allData)
                {
                    var category = kv.Key;
                    foreach (var product in kv.Value)
                    {
                        var context = new ProductContext
                        {
                            Asin = product.ProductName,
                            Category = category,
                            Sentiment = 0,
                            Sales = product.ReviewCount,
                            IsTopSeller = false,
                            IsBottomSeller = false,
                            Price = product.Price,
                            ImageCount = 0,
                            ProductJson = default,
                            DatasetData = new Dictionary<string, object>()
                        };

                        // Extract words from product name
                        var words = ExtractWordsFromText(product.ProductName, stopWords);
                        foreach (var word in words)
                        {
                            registry.ProcessWordForCategory(word, context);
                        }
                    }
                }
            });
        }

        static async Task ProcessWomenClothingDataset(Datasets.WomenClothingSalesDataset dataset,
            StatCalculatorRegistry registry, HashSet<string> stopWords)
        {
            await Task.Run(() =>
            {
                var colorData = dataset.GetColorSales();
                foreach (var kv in colorData)
                {
                    var color = kv.Key;
                    var totalSales = kv.Value.Sum(d => d.Quantity);
                    var avgPrice = kv.Value.Average(d => d.UnitPrice);

                    var context = new ProductContext
                    {
                        Asin = color,
                        Category = "Women Clothing",
                        Sentiment = 0,
                        Sales = totalSales,
                        IsTopSeller = false,
                        IsBottomSeller = false,
                        Price = (decimal)avgPrice,
                        ImageCount = 0,
                        ProductJson = default,
                        DatasetData = new Dictionary<string, object>()
                    };

                    registry.ProcessWordForCategory(color.ToLower(), context);
                }
            });
        }

        static async Task ProcessOnlineSalesDataset(Datasets.OnlineSalesDataset dataset,
            StatCalculatorRegistry registry, HashSet<string> stopWords)
        {
            await Task.Run(() =>
            {
                var categoryData = dataset.GetCategorySales();
                foreach (var kv in categoryData)
                {
                    var category = kv.Key;
                    foreach (var sale in kv.Value)
                    {
                        var context = new ProductContext
                        {
                            Asin = sale.TransactionId,
                            Category = category,
                            Sentiment = 0,
                            Sales = sale.UnitsSold,
                            IsTopSeller = false,
                            IsBottomSeller = false,
                            Price = sale.UnitPrice,
                            ImageCount = 0,
                            ProductJson = default,
                            DatasetData = new Dictionary<string, object>()
                        };

                        var words = ExtractWordsFromText(sale.ProductName, stopWords);
                        foreach (var word in words)
                        {
                            registry.ProcessWordForCategory(word, context);
                        }
                    }
                }
            });
        }

        static List<string> ExtractWordsFromText(string text, HashSet<string> stopWords)
        {
            if (string.IsNullOrEmpty(text)) return new List<string>();

            text = text.ToLower();
            text = CleanTextRegex.Replace(text, " ");
            return text.Split(' ', StringSplitOptions.RemoveEmptyEntries)
                .Where(w => w.Length >= 3 && !stopWords.Contains(w))
                .ToList();
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

        static (List<string> productPaths, List<string> reviewPaths) ResolveAmazonCategoryPaths(string amazonRoot)
        {
            var categoriesFile = Path.Combine(amazonRoot, "all_categories.txt");
            var metaDir = Path.Combine(amazonRoot, "meta_categories");
            var reviewDir = Path.Combine(amazonRoot, "review_categories");

            if (!File.Exists(categoriesFile))
                throw new FileNotFoundException($"Missing category list: {categoriesFile}");

            var categoryNames = File.ReadLines(categoriesFile)
                .Select(line => line.Trim())
                .Where(line => !string.IsNullOrWhiteSpace(line))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            var productPaths = new List<string>();
            var reviewPaths = new List<string>();

            foreach (var category in categoryNames)
            {
                var productPath = Path.Combine(metaDir, $"meta_{category}.jsonl");
                var reviewPath = Path.Combine(reviewDir, $"{category}.jsonl");

                if (!File.Exists(productPath) || !File.Exists(reviewPath))
                {
                    Console.WriteLine($"Skipping category '{category}' because one or both files are missing.");
                    continue;
                }

                productPaths.Add(productPath);
                reviewPaths.Add(reviewPath);
            }

            if (productPaths.Count == 0 || reviewPaths.Count == 0)
                throw new InvalidOperationException("No valid Amazon category dataset pairs found from all_categories.txt");

            Console.WriteLine($"Resolved {productPaths.Count} Amazon category dataset pair(s) from all_categories.txt");
            return (productPaths, reviewPaths);
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
                    text = CleanTextRegex.Replace(text, " ");

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
            Dictionary<string, int> bottomCounts,
            List<ProductCategoryInfo>? allCategoryInfos = null)
        {
            var additionalData = new Dictionary<string, object>
            {
                ["topCounts"] = topCounts,
                ["bottomCounts"] = bottomCounts,
                ["topAsins"] = topAsins,
                ["bottomAsins"] = bottomAsins
            };

            // Output for main categories
            Console.WriteLine($"\n=== OUTPUTTING RESULTS FOR {topCategories.Count} MAIN CATEGORIES ===");
            foreach (var category in topCategories)
            {
                if (!categoryProducts.TryGetValue(category, out var products) || products.Count < 10) continue;

                var totalCount = products.Count;
                Console.WriteLine($"Writing {category} ({totalCount:N0} products)...");

                var safeName = string.Join("_", category.Split(Path.GetInvalidFileNameChars()));
                var filePath = Path.Combine(outputDir, $"{safeName}.txt");
                using var writer = new StreamWriter(filePath);
                writer.WriteLine($"=== {category.ToUpper()} ({totalCount:N0} products) ===");
                writer.WriteLine();

                registry.WriteResultsForCategory(category, writer, additionalData);
            }

            // Output for ALL category groups (if provided)
            if (allCategoryInfos != null)
            {
                Console.WriteLine($"\n=== OUTPUTTING RESULTS FOR ALL {allCategoryInfos.Count} CATEGORY GROUPS ===");

                foreach (var catInfo in allCategoryInfos)
                {
                    if (catInfo.Products.Count < 10) continue;

                    var category = catInfo.CategoryName;
                    var totalCount = catInfo.Products.Count;
                    Console.WriteLine($"Writing {category} ({totalCount:N0} products, Type: {catInfo.Type})...");

                    // Sanitize filename more thoroughly
                    var safeName = SanitizeFileName(category);
                    var filePath = Path.Combine(outputDir, $"{catInfo.Type}_{safeName}.txt");
                    using var writer = new StreamWriter(filePath);
                    writer.WriteLine($"=== {category.ToUpper()} ({totalCount:N0} products, Type: {catInfo.Type}) ===");
                    writer.WriteLine();

                    registry.WriteResultsForCategory(category, writer, additionalData);
                }
            }
        }

        static string SanitizeFileName(string name)
        {
            if (string.IsNullOrEmpty(name)) return "unknown";
            var invalidChars = Path.GetInvalidFileNameChars();
            var sb = new StringBuilder();
            foreach (var c in name)
            {
                if (invalidChars.Contains(c) || c == ' ' || c == '$' || c == '(' || c == ')')
                    sb.Append('_');
                else
                    sb.Append(c);
            }
            return sb.ToString().Trim('_');
        }

        static HashSet<string> ExtractPhrases(string title, HashSet<string> stopWords)
        {
            var phrases = new HashSet<string>();
            if (string.IsNullOrEmpty(title)) return phrases;

            var words = title.ToLower()
                .Split(new[] { ' ', '\t', '-', '_', ',', '.', ';', ':', '(', ')', '[', ']', '"', '\'' },
                       StringSplitOptions.RemoveEmptyEntries)
                .Where(w => w.Length > 2 && !stopWords.Contains(w) && !int.TryParse(w, out _))
                .ToArray();

            // Extract 2-4 word phrases
            for (int len = 2; len <= Math.Min(4, words.Length); len++)
            {
                for (int i = 0; i <= words.Length - len; i++)
                {
                    var phrase = string.Join(" ", words.Skip(i).Take(len));
                    if (phrase.Split(' ').Length == len)
                        phrases.Add(phrase);
                }
            }

            return phrases;
        }
    }
}
