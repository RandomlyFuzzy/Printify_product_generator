using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace PrintifyGenerator.Researcher.Models
{
    public enum CategoryType
    {
        MainCategory,
        PriceRange,
        Brand,
        Gender,
        SubCategory,
        ColorBased,
        MaterialBased,
        PhraseCluster,
        ProductType
    }

    public class ProductCategoryInfo
    {
        public CategoryType Type { get; set; }
        public string CategoryName { get; set; } = "";
        public List<(string asin, int sales)> Products { get; set; } = new();
    }

    public class ProductCategorizer
    {
        private readonly Dictionary<string, string> _asinToTitle = new();
        private readonly Dictionary<string, string> _asinToBrand = new();
        private readonly Dictionary<string, decimal> _asinToPrice = new();
        private readonly Dictionary<string, string> _asinToColor = new();
        private readonly Dictionary<string, string> _asinToMaterial = new();
        private readonly HashSet<string> _stopWords;

        public ProductCategorizer(HashSet<string> stopWords)
        {
            _stopWords = stopWords ?? new HashSet<string>();
        }

        public void IndexProduct(string asin, string title, string brand, decimal price, string color, string material)
        {
            if (!string.IsNullOrEmpty(title)) _asinToTitle[asin] = title;
            if (!string.IsNullOrEmpty(brand)) _asinToBrand[asin] = brand;
            if (price > 0) _asinToPrice[asin] = price;
            if (!string.IsNullOrEmpty(color)) _asinToColor[asin] = color;
            if (!string.IsNullOrEmpty(material)) _asinToMaterial[asin] = material;
        }

        public List<ProductCategoryInfo> CategorizeProducts(
            Dictionary<string, (string category, int sales, string title, string brand, decimal price)> productData,
            int minProductsPerCategory = 10)
        {
            var result = new List<ProductCategoryInfo>();

            // 1. Main category (existing)
            var mainCatGroups = productData
                .Where(kv => !string.IsNullOrEmpty(kv.Value.category) && kv.Value.sales > 0)
                .GroupBy(kv => kv.Value.category)
                .ToDictionary(g => g.Key, g => g.Select(kv => (kv.Key, kv.Value.sales)).ToList());

            foreach (var kv in mainCatGroups.Where(g => g.Value.Count >= minProductsPerCategory))
            {
                result.Add(new ProductCategoryInfo
                {
                    Type = CategoryType.MainCategory,
                    CategoryName = kv.Key,
                    Products = kv.Value
                });
            }

            // 2. Price range categories
            var priceRanges = new[]
            {
                ("Extreme Budget (Under $10)", 0, 10m),
                ("Budget (Under $25)", 10, 25m),
                ("Affordable ($25-$50)", 25, 50m),
                ("Mid-Range ($50-$100)", 50, 100m),
                ("Premium ($100-$200)", 100, 200m),
                ("Luxury ($200+)", 200, decimal.MaxValue)
            };

            foreach (var (rangeName, minPrice, maxPrice) in priceRanges)
            {
                var products = productData
                    .Where(kv => kv.Value.price > minPrice && kv.Value.price <= maxPrice && kv.Value.sales > 0)
                    .Select(kv => (kv.Key, kv.Value.sales))
                    .ToList();

                if (products.Count >= minProductsPerCategory)
                {
                    result.Add(new ProductCategoryInfo
                    {
                        Type = CategoryType.PriceRange,
                        CategoryName = rangeName,
                        Products = products
                    });
                }
            }

            // 3. Brand categories (top brands only)
            var brandGroups = productData
                .Where(kv => !string.IsNullOrEmpty(kv.Value.brand) && kv.Value.sales > 0)
                .GroupBy(kv => kv.Value.brand)
                .Where(g => g.Count() >= minProductsPerCategory)
                .OrderByDescending(g => g.Count())
                .Take(20);

            foreach (var brandGroup in brandGroups)
            {
                result.Add(new ProductCategoryInfo
                {
                    Type = CategoryType.Brand,
                    CategoryName = $"Brand: {brandGroup.Key}",
                    Products = brandGroup.Select(kv => (kv.Key, kv.Value.sales)).ToList()
                });
            }

            // 4. Gender-based categories
            var genderPatterns = new Dictionary<string, string[]>
            {
                ["Women's"] = new[] { "women", "womens", "woman", "ladies", "female", "her" },
                ["Men's"] = new[] { "men", "mens", "man", "gentlemen", "male", "his" },
                ["Unisex"] = new[] { "unisex", "neutral", "both" },
                ["Kids"] = new[] { "kids", "children", "child", "boy", "girl", "baby", "toddler", "infant" }
            };

            foreach (var genderPattern in genderPatterns)
            {
                var genderProducts = new List<(string asin, int sales)>();
                var seen = new HashSet<string>();

                foreach (var kv in productData.Where(kv => kv.Value.sales > 0))
                {
                    if (seen.Contains(kv.Key)) continue;
                    var title = kv.Value.title?.ToLower() ?? "";
                    if (genderPattern.Value.Any(p => title.Contains(p)))
                    {
                        genderProducts.Add((kv.Key, kv.Value.sales));
                        seen.Add(kv.Key);
                    }
                }

                if (genderProducts.Count >= minProductsPerCategory)
                {
                    result.Add(new ProductCategoryInfo
                    {
                        Type = CategoryType.Gender,
                        CategoryName = genderPattern.Key,
                        Products = genderProducts
                    });
                }
            }

            // 5. Sub-category patterns
            var subCategories = new Dictionary<string, string[]>
            {
                ["Shoes"] = new[] { "shoe", "sneaker", "boot", "sandal", "heel", "flat", "loafer", "slipper" },
                ["Dresses"] = new[] { "dress", "gown", "frock" },
                ["Tops"] = new[] { "t-shirt", "shirt", "blouse", "top", "tee", "sweater", "hoodie", "jacket" },
                ["Pants"] = new[] { "pant", "jean", "trouser", "legging", "short" },
                ["Accessories"] = new[] { "necklace", "bracelet", "ring", "earring", "watch", "sunglass", "bag", "purse" },
                ["Socks"] = new[] { "sock", "hose", "stocking" },
                ["Jewelry"] = new[] { "jewelry", "pendant", "chain", "gem", "diamond", "gold", "silver" }
            };

            foreach (var subCat in subCategories)
            {
                var subCatProducts = new List<(string asin, int sales)>();
                var seen = new HashSet<string>();

                foreach (var kv in productData.Where(kv => kv.Value.sales > 0))
                {
                    if (seen.Contains(kv.Key)) continue;
                    var title = kv.Value.title?.ToLower() ?? "";
                    if (subCat.Value.Any(p => title.Contains(p)))
                    {
                        subCatProducts.Add((kv.Key, kv.Value.sales));
                        seen.Add(kv.Key);
                    }
                }

                if (subCatProducts.Count >= minProductsPerCategory)
                {
                    result.Add(new ProductCategoryInfo
                    {
                        Type = CategoryType.SubCategory,
                        CategoryName = subCat.Key,
                        Products = subCatProducts
                    });
                }
            }

            // 6. Color-based categories
            var colorGroups = productData
                .Where(kv => kv.Value.sales > 0)
                .Select(kv =>
                {
                    _asinToColor.TryGetValue(kv.Key, out var indexedColor);
                    var title = kv.Value.title?.ToLowerInvariant() ?? "";
                    var color = ExtractIndexedColor(indexedColor) ?? ExtractColor(title);
                    return (kv.Key, kv.Value.sales, color);
                })
                .Where(x => !string.IsNullOrEmpty(x.color))
                .GroupBy(x => x.color)
                .Where(g => g.Count() >= minProductsPerCategory)
                .OrderByDescending(g => g.Count())
                .Take(15);

            foreach (var colorGroup in colorGroups)
            {
                result.Add(new ProductCategoryInfo
                {
                    Type = CategoryType.ColorBased,
                    CategoryName = $"Color: {colorGroup.Key}",
                    Products = colorGroup.Select(x => (x.Key, x.sales)).ToList()
                });
            }

            // 7. Material-based categories
            var materialKeywords = new Dictionary<string, string[]>
            {
                ["Cotton"] = new[] { "cotton" },
                ["Polyester"] = new[] { "polyester" },
                ["Wool"] = new[] { "wool", "merino" },
                ["Leather"] = new[] { "leather", "suede" },
                ["Silk"] = new[] { "silk" },
                ["Denim"] = new[] { "denim", "jean" },
                ["Synthetic"] = new[] { "nylon", "spandex", "elastane", "lycra" }
            };

            foreach (var material in materialKeywords)
            {
                var materialProducts = new List<(string asin, int sales)>();
                var seen = new HashSet<string>();

                foreach (var kv in productData.Where(kv => kv.Value.sales > 0))
                {
                    if (seen.Contains(kv.Key)) continue;
                    _asinToMaterial.TryGetValue(kv.Key, out var indexedMaterial);
                    var materialSource = !string.IsNullOrWhiteSpace(indexedMaterial)
                        ? indexedMaterial.ToLowerInvariant()
                        : kv.Value.title?.ToLowerInvariant() ?? "";

                    if (material.Value.Any(p => materialSource.Contains(p)))
                    {
                        materialProducts.Add((kv.Key, kv.Value.sales));
                        seen.Add(kv.Key);
                    }
                }

                if (materialProducts.Count >= minProductsPerCategory)
                {
                    result.Add(new ProductCategoryInfo
                    {
                        Type = CategoryType.MaterialBased,
                        CategoryName = $"Material: {material.Key}",
                        Products = materialProducts
                    });
                }
            }

            return result;
        }

        private string? ExtractColor(string title)
        {
            var colors = new[] { "red", "blue", "green", "yellow", "black", "white", "pink", "purple",
                                "orange", "brown", "gray", "grey", "navy", "teal", "maroon", "gold",
                                "silver", "beige", "cream", "coral", "turquoise", "lavender" };

            foreach (var color in colors)
            {
                if (title.Contains(color))
                    return color;
            }
            return null;
        }

        private string? ExtractIndexedColor(string? rawColor)
        {
            if (string.IsNullOrWhiteSpace(rawColor)) return null;

            return ExtractColor(rawColor.ToLowerInvariant());
        }

        // 8. Phrase cluster categories (products sharing common phrases)
        public List<ProductCategoryInfo> CreatePhraseClusters(
            Dictionary<string, (string category, int sales, string title, string brand, decimal price)> productData,
            Dictionary<string, HashSet<string>> productPhrases,
            int minClusterSize = 20)
        {
            var result = new List<ProductCategoryInfo>();

            // Group products by significant phrases
            var phraseToProducts = new Dictionary<string, List<(string asin, int sales)>>();

            foreach (var kv in productData.Where(kv => kv.Value.sales > 0))
            {
                if (!productPhrases.TryGetValue(kv.Key, out var phrases))
                    continue;

                foreach (var phrase in phrases.Where(p => p.Split(' ').Length >= 2))
                {
                    if (!phraseToProducts.TryGetValue(phrase, out var list))
                        phraseToProducts[phrase] = list = new List<(string asin, int sales)>();
                    list.Add((kv.Key, kv.Value.sales));
                }
            }

            // Take top phrases that appear in multiple products
            var topPhrases = phraseToProducts
                .Where(kvp => kvp.Value.Count >= minClusterSize)
                .OrderByDescending(kvp => kvp.Value.Count)
                .Take(200);

            foreach (var phraseGroup in topPhrases)
            {
                result.Add(new ProductCategoryInfo
                {
                    Type = CategoryType.PhraseCluster,
                    CategoryName = $"Phrase: {phraseGroup.Key}",
                    Products = phraseGroup.Value
                });
            }

            return result;
        }

        // Produces product-type categories by matching garment type + modifier combos across all titles.
        // e.g. "short sleeve t-shirt", "unisex hoodie", "crew neck sweatshirt"
        public List<ProductCategoryInfo> CreateProductTypeCategories(
            Dictionary<string, (string category, int sales, string title, string brand, decimal price)> productData,
            int minProducts = 10)
        {
            var garmentTypes = new[]
            {
                "t-shirt", "tshirt", "tee shirt", "tee",
                "hoodie", "hooded sweatshirt", "hooded pullover",
                "sweatshirt", "crewneck", "crew neck sweatshirt",
                "jumper", "pullover", "knitwear",
                "jacket", "zip up jacket", "bomber jacket", "track jacket",
                "cardigan",
                "dress", "maxi dress", "midi dress", "mini dress",
                "leggings", "yoga pants", "joggers", "sweatpants",
                "shorts", "bermuda shorts",
                "polo shirt", "polo",
                "tank top", "vest top", "sleeveless top",
                "long sleeve shirt", "long sleeve top",
                "button up shirt", "button down shirt",
                "flannel shirt",
                "crop top", "crop hoodie",
                "baseball tee", "raglan shirt",
                "blouse",
                "tunic",
                "socks", "crew socks", "ankle socks",
                "beanie", "beanie hat",
                "baseball cap", "trucker hat", "dad hat",
                "tote bag",
                "phone case",
                "mug", "coffee mug",
                "poster", "wall art",
                "throw pillow", "pillow case"
            };

            var modifiers = new[]
            {
                // fit
                "oversized", "slim fit", "slim", "relaxed fit", "relaxed",
                "regular fit", "boxy", "fitted",
                // sleeve
                "short sleeve", "long sleeve", "sleeveless", "3/4 sleeve",
                // neck
                "crew neck", "v-neck", "vneck", "round neck", "scoop neck",
                "turtleneck", "mock neck", "cowl neck", "high neck",
                // closure
                "zip up", "full zip", "quarter zip", "pullover", "button up",
                // gender/audience
                "unisex", "womens", "women's", "mens", "men's", "kids",
                "girls", "boys", "plus size", "maternity",
                // style
                "graphic", "printed", "plain", "blank", "solid",
                "tie dye", "tie-dye", "striped", "floral", "vintage",
                "retro", "classic", "minimal", "minimalist",
                // weight
                "lightweight", "heavyweight", "fleece", "thermal",
                // length
                "cropped", "crop", "longline", "oversized"
            };

            var results = new List<ProductCategoryInfo>();
            // key = normalised label, value = set of (asin, sales)
            var buckets = new Dictionary<string, List<(string asin, int sales)>>(StringComparer.OrdinalIgnoreCase);

            foreach (var kv in productData.Where(kv => kv.Value.sales > 0 && !string.IsNullOrEmpty(kv.Value.title)))
            {
                var titleLow = kv.Value.title.ToLowerInvariant();

                // standalone garment type
                foreach (var garment in garmentTypes)
                {
                    if (!titleLow.Contains(garment)) continue;

                    var label = CapitalizeLabel(garment);
                    AddToBucket(buckets, label, kv.Key, kv.Value.sales);

                    // modifier + garment
                    foreach (var mod in modifiers)
                    {
                        if (!titleLow.Contains(mod)) continue;
                        var comboLabel = CapitalizeLabel($"{mod} {garment}");
                        AddToBucket(buckets, comboLabel, kv.Key, kv.Value.sales);
                    }
                }
            }

            foreach (var bucket in buckets.Where(b => b.Value.Count >= minProducts)
                                          .OrderByDescending(b => b.Value.Count))
            {
                // Deduplicate: keep distinct ASINs (same product can match multiple combos)
                var distinct = bucket.Value
                    .GroupBy(x => x.asin)
                    .Select(g => (g.Key, g.Max(x => x.sales)))
                    .ToList();

                if (distinct.Count < minProducts) continue;

                results.Add(new ProductCategoryInfo
                {
                    Type = CategoryType.ProductType,
                    CategoryName = bucket.Key,
                    Products = distinct
                });
            }

            return results;
        }

        private static void AddToBucket(
            Dictionary<string, List<(string asin, int sales)>> buckets,
            string label, string asin, int sales)
        {
            if (!buckets.TryGetValue(label, out var list))
                buckets[label] = list = new List<(string asin, int sales)>();
            list.Add((asin, sales));
        }

        private static string CapitalizeLabel(string s)
        {
            if (string.IsNullOrEmpty(s)) return s;
            return char.ToUpperInvariant(s[0]) + s[1..];
        }
    }
}
