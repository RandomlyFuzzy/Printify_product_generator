using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using PrintifyGenerator.Researcher.Interfaces;

namespace PrintifyGenerator.Researcher.Datasets
{
    public class EcommerceBehaviorDataset : IDatasetProvider
    {
        public string Name => "Ecommerce Behavior Dataset (2019)";
        public string Type => "EcommerceBehavior";

        private readonly string[] _csvPaths;
        private readonly Dictionary<string, BehaviorData> _productData = new();
        private readonly Dictionary<string, string> _titleToAsin = new(StringComparer.OrdinalIgnoreCase);

        public class BehaviorData
        {
            public long Views { get; set; }
            public long Carts { get; set; }
            public long Purchases { get; set; }
            public string Brand { get; set; } = "";
            public string Category { get; set; } = "";
            public decimal Price { get; set; }
        }

        public EcommerceBehaviorDataset(string[] csvPaths)
        {
            _csvPaths = csvPaths;
        }

        public async Task LoadAsync()
        {
            Console.WriteLine("Loading Ecommerce Behavior data...");
            int totalRecords = 0;

            await Task.Run(() =>
            {
                foreach (var csvPath in _csvPaths)
                {
                    if (!File.Exists(csvPath)) continue;
                    Console.WriteLine($"  Loading from: {Path.GetFileName(csvPath)}");

                    int lineNum = 0;
                    foreach (var line in File.ReadLines(csvPath).Skip(1))
                    {
                        lineNum++;
                        if (lineNum % 500000 == 0)
                            Console.WriteLine($"    Processed {lineNum:N0} records...");

                        var parts = line.Split(',', 9);
                        if (parts.Length < 8) continue;

                        var eventType = parts[1].Trim();
                        var productId = parts[2].Trim();
                        if (string.IsNullOrEmpty(productId)) continue;

                        if (!_productData.TryGetValue(productId, out var tempData))
                        {
                            tempData = new BehaviorData();
                            _productData[productId] = tempData;
                        }
                        var data = tempData;

                        switch (eventType)
                        {
                            case "view": data.Views++; break;
                            case "cart": data.Carts++; break;
                            case "purchase": data.Purchases++; break;
                        }

                        // Extract brand and category if available
                        if (parts.Length > 5 && string.IsNullOrEmpty(data.Brand))
                            data.Brand = parts[5].Trim();
                        if (parts.Length > 4 && string.IsNullOrEmpty(data.Category))
                            data.Category = parts[4].Trim();
                        if (parts.Length > 6 && data.Price == 0)
                        {
                            decimal.TryParse(parts[6].Trim(), out var parsedPrice);
                            data.Price = parsedPrice;
                        }

                        totalRecords++;
                    }
                }
            });

            Console.WriteLine($"Loaded Behavior data: {_productData.Count:N0} products, {totalRecords:N0} total events");
        }

        public void BuildTitleMapping(string jsonPath)
        {
            if (!File.Exists(jsonPath)) return;

            Console.WriteLine("Building title→ASIN mapping for Behavior data...");
            int scanned = 0;

            foreach (var line in File.ReadLines(jsonPath))
            {
                scanned++;
                if (scanned % 100000 == 0) Console.WriteLine($"  Scanned {scanned:N0} products for title mapping...");
                try
                {
                    using var doc = JsonDocument.Parse(line);
                    var root = doc.RootElement;
                    string? asin = GetAsin(root);
                    if (string.IsNullOrEmpty(asin)) continue;

                    if (root.TryGetProperty("title", out var titleElem) && titleElem.ValueKind == JsonValueKind.String)
                    {
                        var title = titleElem.GetString()?.ToLower().Trim();
                        if (!string.IsNullOrEmpty(title) && !_titleToAsin.ContainsKey(title))
                            _titleToAsin[title] = asin;
                    }
                }
                catch { }
            }

            Console.WriteLine($"  Mapped {_titleToAsin.Count:N0} titles to ASINs");
        }

        public bool HasProductData(string asin) =>
            _productData.ContainsKey(asin) ||
            (_titleToAsin.TryGetValue(asin, out var mappedAsin) && _productData.ContainsKey(mappedAsin));

        public T? GetProductData<T>(string asin) where T : class
        {
            BehaviorData? data = null;

            if (_productData.TryGetValue(asin, out data))
                return data as T;

            if (_titleToAsin.TryGetValue(asin, out var mappedAsin) && _productData.TryGetValue(mappedAsin, out data))
                return data as T;

            return null;
        }

        public IEnumerable<string> GetAllAsins() => _productData.Keys.Concat(_titleToAsin.Keys).Distinct();

        private static string? GetAsin(System.Text.Json.JsonElement root)
        {
            if (root.TryGetProperty("parent_asin", out var pa)) return pa.GetString();
            if (root.TryGetProperty("asin", out var a)) return a.GetString();
            return null;
        }
    }
}
