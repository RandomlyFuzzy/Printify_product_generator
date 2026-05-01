using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using PrintifyGenerator.Researcher.Interfaces;

namespace PrintifyGenerator.Researcher.Datasets
{
    public class EbayProductDataset : IDatasetProvider
    {
        public string Name => "eBay Product Dataset";
        public string Type => "EbayProduct";

        private readonly string[] _csvPaths;
        private readonly Dictionary<string, EbayProductData> _productData = new();
        private readonly Dictionary<string, List<EbayProductData>> _categoryProducts = new();

        public class EbayProductData
        {
            public string ProductName { get; set; } = "";
            public decimal Price { get; set; }
            public string Category { get; set; } = "";
            public string Brand { get; set; } = "";
            public string Color { get; set; } = "";
            public string Material { get; set; } = "";
            public string Condition { get; set; } = "";
            public int ReviewCount { get; set; }
            public string ListingStatus { get; set; } = "";
        }

        public EbayProductDataset(string[] csvPaths)
        {
            _csvPaths = csvPaths;
        }

        public async Task LoadAsync()
        {
            Console.WriteLine("Loading eBay product data...");
            int loaded = 0;

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
                        if (lineNum % 10000 == 0)
                            Console.WriteLine($"    Processed {lineNum:N0} products...");

                        var parts = ParseCsvLine(line);
                        if (parts.Length < 5) continue;

                        var data = new EbayProductData
                        {
                            ProductName = parts[0].Trim('"').Trim(),
                            Price = ParsePrice(parts[1]),
                            Category = parts[3].Trim('"').Trim(),
                            Brand = parts[4].Trim('"').Trim(),
                            Color = parts.Length > 6 ? parts[6].Trim('"').Trim() : "",
                            Material = parts.Length > 7 ? parts[7].Trim('"').Trim() : "",
                            Condition = parts.Length > 8 ? parts[8].Trim('"').Trim() : "",
                            ReviewCount = parts.Length > 9 ? ParseInt(parts[9]) : 0,
                            ListingStatus = parts.Length > 10 ? parts[10].Trim('"').Trim() : ""
                        };

                        if (string.IsNullOrEmpty(data.ProductName)) continue;

                        var key = data.ProductName.ToLower();
                        _productData[key] = data;

                        if (!string.IsNullOrEmpty(data.Category))
                        {
                            if (!_categoryProducts.TryGetValue(data.Category, out var list))
                                _categoryProducts[data.Category] = list = new List<EbayProductData>();
                            list.Add(data);
                        }

                        loaded++;
                    }
                }
            });

            Console.WriteLine($"Loaded {loaded:N0} eBay products");
        }

        public bool HasProductData(string productName) =>
            _productData.ContainsKey(productName.ToLower());

        public T? GetProductData<T>(string productName) where T : class
        {
            if (_productData.TryGetValue(productName.ToLower(), out var data))
                return data as T;
            return null;
        }

        public IEnumerable<string> GetAllAsins() => _productData.Keys;

        public Dictionary<string, List<EbayProductData>> GetCategoryProducts() => _categoryProducts;

        private static string[] ParseCsvLine(string line)
        {
            var result = new List<string>();
            var current = "";
            var inQuotes = false;

            for (int i = 0; i < line.Length; i++)
            {
                if (line[i] == '"')
                {
                    inQuotes = !inQuotes;
                }
                else if (line[i] == ',' && !inQuotes)
                {
                    result.Add(current);
                    current = "";
                }
                else
                {
                    current += line[i];
                }
            }
            result.Add(current);

            return result.ToArray();
        }

        private static decimal ParsePrice(string priceStr)
        {
            priceStr = priceStr.Trim('"', '$', ' ');
            if (priceStr.Contains("to"))
            {
                var parts = priceStr.Split("to");
                return parts.Length > 0 && decimal.TryParse(parts[0].Trim(), out var price) ? price : 0;
            }
            decimal.TryParse(priceStr, out var result);
            return result;
        }

        private static int ParseInt(string str)
        {
            str = str.Trim('"', ' ');
            int.TryParse(str, out var result);
            return result;
        }
    }
}
