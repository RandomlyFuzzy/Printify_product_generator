using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using PrintifyGenerator.Researcher.Interfaces;

namespace PrintifyGenerator.Researcher.Datasets
{
    public class AmazonProductDataset : IDatasetProvider
    {
        public string Name => "Amazon Product Dataset";
        public string Type => "AmazonProduct";

        private static readonly Regex CategorySymbolRegex = new Regex(@"[^a-z0-9]+", RegexOptions.Compiled);

        private readonly string _productPath;
        private readonly Dictionary<string, ProductData> _asinData = new();
        private readonly Dictionary<string, double> _asinSentiment = new();
        private readonly Dictionary<string, HashSet<string>> _asinCategories = new();

        public class ProductData
        {
            public string Category { get; set; } = "";
            public int Sales { get; set; }
            public decimal Price { get; set; }
            public int ImageCount { get; set; }
            public string Title { get; set; } = "";
            public string Brand { get; set; } = "";
            public string Color { get; set; } = "";
            public string Material { get; set; } = "";
            public string Manufacturer { get; set; } = "";
            public DateTime? Date { get; set; }
        }

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

                        string? asin = GetAsin(root);
                        if (string.IsNullOrEmpty(asin)) continue;

                        int sales = GetSales(root);
                        string category = GetCategory(root);
                        if (sales <= 0 || string.IsNullOrEmpty(category)) continue;

                        var data = new ProductData
                        {
                            Category = category,
                            Sales = sales,
                            Price = GetPrice(root),
                            ImageCount = GetImageCount(root),
                            Title = GetTitle(root),
                            Brand = GetBrand(root),
                            Color = GetColor(root),
                            Material = GetMaterial(root),
                            Manufacturer = GetManufacturer(root)
                        };

                        _asinData[asin] = data;

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

        public void SetSentimentData(Dictionary<string, double> sentiment)
        {
            foreach (var kv in sentiment)
                _asinSentiment[kv.Key] = kv.Value;
        }

        public double GetSentiment(string asin) => _asinSentiment.TryGetValue(asin, out var s) ? s : 0;

        public bool HasProductData(string asin) => _asinData.ContainsKey(asin);

        public T? GetProductData<T>(string asin) where T : class
        {
            if (!_asinData.TryGetValue(asin, out var data)) return null;

            if (typeof(T) == typeof(ProductData)) return data as T;
            if (typeof(T) == typeof(string)) return (T)(object)data.Category;
            if (typeof(T) == typeof(int)) return (T)(object)data.Sales;
            if (typeof(T) == typeof(decimal)) return (T)(object)data.Price;

            return null;
        }

        public IEnumerable<string> GetAllAsins() => _asinData.Keys;

        public Dictionary<string, ProductData> GetAllProductData() => _asinData;

        public HashSet<string> GetCategoryAsins(string category)
        {
            _asinCategories.TryGetValue(category, out var asins);
            return asins ?? new HashSet<string>();
        }

        private static string? GetAsin(JsonElement root)
        {
            if (root.TryGetProperty("parent_asin", out var pa))
                return pa.ValueKind == JsonValueKind.String ? pa.GetString() : null;
            if (root.TryGetProperty("asin", out var a))
                return a.ValueKind == JsonValueKind.String ? a.GetString() : null;
            return null;
        }

        private static int GetSales(JsonElement root)
        {
            if (root.TryGetProperty("rating_number", out var rn) && rn.ValueKind == JsonValueKind.Number)
                return rn.GetInt32();
            return 0;
        }

        private static string GetCategory(JsonElement root)
        {
            var department = GetDetailString(root, "Department");
            if (!string.IsNullOrWhiteSpace(department))
                return NormalizeCategory(department, singularize: true);

            if (root.TryGetProperty("categories", out var cats) && cats.ValueKind == JsonValueKind.Array)
            {
                foreach (var c in cats.EnumerateArray())
                    if (c.ValueKind == JsonValueKind.String)
                        return NormalizeCategory(c.GetString() ?? "", singularize: true);
            }

            if (root.TryGetProperty("main_category", out var mainCategory) && mainCategory.ValueKind == JsonValueKind.String)
                return NormalizeCategory(mainCategory.GetString() ?? "", singularize: true);

            var manufacturer = GetManufacturer(root);
            if (!string.IsNullOrWhiteSpace(manufacturer))
                return NormalizeCategory(manufacturer, singularize: false);

            return "";
        }

        private static decimal GetPrice(JsonElement root)
        {
            if (root.TryGetProperty("price", out var priceElem) && priceElem.ValueKind == JsonValueKind.Number)
                return (decimal)priceElem.GetDouble();
            return 0;
        }

        private static int GetImageCount(JsonElement root)
        {
            if (root.TryGetProperty("images", out var imgElem) && imgElem.ValueKind == JsonValueKind.Array)
                return imgElem.GetArrayLength();
            return 0;
        }

        private static string GetTitle(JsonElement root)
        {
            if (root.TryGetProperty("title", out var titleElem) && titleElem.ValueKind == JsonValueKind.String)
                return titleElem.GetString() ?? "";
            return "";
        }

        private static string GetBrand(JsonElement root)
        {
            var detailBrand = GetDetailString(root, "Brand", "Brand Name");
            if (!string.IsNullOrWhiteSpace(detailBrand))
                return detailBrand;

            if (root.TryGetProperty("brand", out var brandElem) && brandElem.ValueKind == JsonValueKind.String)
                return brandElem.GetString() ?? "";

            if (root.TryGetProperty("store", out var storeElem) && storeElem.ValueKind == JsonValueKind.String)
                return storeElem.GetString() ?? "";

            return "";
        }

        private static string GetColor(JsonElement root)
        {
            return GetDetailString(root, "Color", "Color Name", "Stone Color", "Band Color", "Shade Color", "Light Color", "Dial Color", "Lens Color", "Blade Color");
        }

        private static string GetMaterial(JsonElement root)
        {
            return GetDetailString(root, "Material", "Material Type", "Outer Material", "Inner Material", "Fabric Type", "material_composition", "Material Composition", "Top Material", "Sole Material", "Insole Material");
        }

        private static string GetManufacturer(JsonElement root)
        {
            return GetDetailString(root, "Manufacturer");
        }

        private static string GetDetailString(JsonElement root, params string[] propertyNames)
        {
            if (!root.TryGetProperty("details", out var details) || details.ValueKind != JsonValueKind.Object)
                return "";

            foreach (var propertyName in propertyNames)
            {
                if (details.TryGetProperty(propertyName, out var value) && value.ValueKind == JsonValueKind.String)
                {
                    var text = value.GetString()?.Trim();
                    if (!string.IsNullOrWhiteSpace(text))
                        return text;
                }
            }

            return "";
        }

        private static string NormalizeCategory(string value, bool singularize)
        {
            if (string.IsNullOrWhiteSpace(value))
                return "";

            var normalized = CategorySymbolRegex.Replace(value.Trim().ToLowerInvariant(), " ");
            var tokens = normalized
                .Split(' ', StringSplitOptions.RemoveEmptyEntries)
                .Select(token => singularize ? SingularizeCategoryToken(token) : token)
                .Where(token => !string.IsNullOrWhiteSpace(token));

            return string.Join(" ", tokens);
        }

        private static string SingularizeCategoryToken(string token)
        {
            return token switch
            {
                "women" => "woman",
                "womens" => "woman",
                "men" => "man",
                "mens" => "man",
                "ladies" => "lady",
                "gentlemen" => "gentleman",
                "kids" => "kid",
                "children" => "child",
                _ => SingularizePlural(token)
            };
        }

        private static string SingularizePlural(string token)
        {
            if (token.Length <= 3)
                return token;

            if (token.EndsWith("ies", StringComparison.Ordinal) && token.Length > 4)
                return token[..^3] + "y";

            if (token.EndsWith("oes", StringComparison.Ordinal))
                return token[..^1];

            if (token.EndsWith("sses", StringComparison.Ordinal) ||
                token.EndsWith("shes", StringComparison.Ordinal) ||
                token.EndsWith("ches", StringComparison.Ordinal) ||
                token.EndsWith("xes", StringComparison.Ordinal) ||
                token.EndsWith("zes", StringComparison.Ordinal))
                return token[..^2];

            if (token.EndsWith("s", StringComparison.Ordinal) &&
                !token.EndsWith("ss", StringComparison.Ordinal) &&
                !token.EndsWith("us", StringComparison.Ordinal) &&
                !token.EndsWith("is", StringComparison.Ordinal))
                return token[..^1];

            return token;
        }
    }
}
