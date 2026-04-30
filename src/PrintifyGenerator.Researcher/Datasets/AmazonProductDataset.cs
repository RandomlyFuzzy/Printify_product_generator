using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using PrintifyGenerator.Researcher.Interfaces;

namespace PrintifyGenerator.Researcher.Datasets
{
    public class AmazonProductDataset : IDatasetProvider
    {
        public string Name => "Amazon Product Dataset";
        public string Type => "AmazonProduct";

        private readonly string _productPath;
        private readonly Dictionary<string, (string Category, int Sales, decimal Price, int ImageCount, string Title, string Brand)> _asinData = new();
        private readonly Dictionary<string, double> _asinSentiment = new();
        private readonly Dictionary<string, HashSet<string>> _asinCategories = new();

        public AmazonProductDataset(string productPath)
        {
            _productPath = productPath;
        }

        public async Task LoadAsync()
        {
            Console.WriteLine($"Loading Amazon product data from: {Path.GetFileName(_productPath)}");
            int loaded = 0;

            await Task.Run(() =>
            {
                foreach (var line in File.ReadLines(_productPath))
                {
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

                        if (string.IsNullOrEmpty(asin)) continue;

                        int sales = 0;
                        if (root.TryGetProperty("rating_number", out var rn))
                            sales = rn.ValueKind == JsonValueKind.Number ? rn.GetInt32() : 0;

                        string? category = null;
                        if (root.TryGetProperty("categories", out var cats) && cats.ValueKind == JsonValueKind.Array)
                        {
                            foreach (var c in cats.EnumerateArray())
                                if (c.ValueKind == JsonValueKind.String)
                                    category = c.GetString();
                        }

                        if (sales <= 0 || string.IsNullOrEmpty(category)) continue;

                        decimal price = 0;
                        if (root.TryGetProperty("price", out var priceElem) && priceElem.ValueKind == JsonValueKind.Number)
                            price = (decimal)priceElem.GetDouble();

                        int imageCount = 0;
                        if (root.TryGetProperty("images", out var imgElem) && imgElem.ValueKind == JsonValueKind.Array)
                            imageCount = imgElem.GetArrayLength();

                        string? title = null;
                        if (root.TryGetProperty("title", out var titleElem) && titleElem.ValueKind == JsonValueKind.String)
                            title = titleElem.GetString();

                        string? brand = null;
                        if (root.TryGetProperty("brand", out var brandElem) && brandElem.ValueKind == JsonValueKind.String)
                            brand = brandElem.GetString();

                        _asinData[asin] = (category, sales, price, imageCount, title ?? "", brand ?? "");

                        if (!_asinCategories.TryGetValue(category, out var catSet))
                            _asinCategories[category] = catSet = new HashSet<string>();
                        catSet.Add(asin);

                        loaded++;
                        if (loaded % 10000 == 0)
                            Console.WriteLine($"  Loaded {loaded:N0} products...");
                    }
                    catch { }
                }
            });

            Console.WriteLine($"Loaded {loaded:N0} Amazon products");
        }

        public bool HasProductData(string asin) => _asinData.ContainsKey(asin);

        public T? GetProductData<T>(string asin) where T : class
        {
            if (!_asinData.TryGetValue(asin, out var data)) return null;

            if (typeof(T) == typeof(string)) // Category
                return data.Category as T;
            if (typeof(T) == typeof(int)) // Sales
                return (T)(object)data.Sales;
            if (typeof(T) == typeof(decimal)) // Price
                return (T)(object)data.Price;
            if (typeof(T) == typeof(int)) // ImageCount
                return (T)(object)data.ImageCount;

            return null;
        }

        public IEnumerable<string> GetAllAsins() => _asinData.Keys;

        public Dictionary<string, (string Category, int Sales, decimal Price, int ImageCount, string Title, string Brand)> GetAsinData() => _asinData;

        public void SetSentimentData(Dictionary<string, double> sentiment)
        {
            foreach (var kv in sentiment)
                _asinSentiment[kv.Key] = kv.Value;
        }

        public double GetSentiment(string asin) => _asinSentiment.TryGetValue(asin, out var s) ? s : 0;

        public HashSet<string> GetCategoryAsins(string category)
        {
            _asinCategories.TryGetValue(category, out var asins);
            return asins ?? new HashSet<string>();
        }
    }
}
