using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using PrintifyGenerator.Researcher.Interfaces;

namespace PrintifyGenerator.Researcher.Datasets
{
    public class CtrData
    {
        public long Views { get; set; }
        public long Carts { get; set; }
        public long Purchases { get; set; }
    }

    public class CtrDataset : IDatasetProvider
    {
        public string Name => "CTR Event Dataset";
        public string Type => "CTR";

        private readonly string[] _csvPaths;
        private readonly Dictionary<string, CtrData> _productCtr = new();
        private readonly Dictionary<string, string> _titleToAsin = new(StringComparer.OrdinalIgnoreCase);

        public CtrDataset(string[] csvPaths)
        {
            _csvPaths = csvPaths;
        }

        public async Task LoadAsync()
        {
            Console.WriteLine("Loading CTR data...");
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
                            Console.WriteLine($"    Processed {lineNum:N0} CTR records...");

                        var parts = line.Split(',', 9);
                        if (parts.Length < 8) continue;

                        var eventType = parts[1].Trim();
                        var productId = parts[2].Trim();
                        if (string.IsNullOrEmpty(productId)) continue;

                        if (!_productCtr.TryGetValue(productId, out var ctr))
                            _productCtr[productId] = ctr = new CtrData();

                        if (eventType == "view") ctr.Views++;
                        else if (eventType == "cart") ctr.Carts++;
                        else if (eventType == "purchase") ctr.Purchases++;
                        
                        totalRecords++;
                    }
                }
            });

            Console.WriteLine($"Loaded CTR data: {_productCtr.Count:N0} products, {totalRecords:N0} total events");
        }

        public bool HasProductData(string asin)
        {
            return _productCtr.ContainsKey(asin) || 
                   (_titleToAsin.TryGetValue(asin, out var mappedAsin) && _productCtr.ContainsKey(mappedAsin));
        }

        public T? GetProductData<T>(string asin) where T : class
        {
            CtrData? ctr = null;
            if (_productCtr.TryGetValue(asin, out ctr))
                return ctr as T;

            if (_titleToAsin.TryGetValue(asin, out var mappedAsin) && _productCtr.TryGetValue(mappedAsin, out ctr))
                return ctr as T;

            return null;
        }

        public IEnumerable<string> GetAllAsins() => _productCtr.Keys.Concat(_titleToAsin.Keys).Distinct();

        public void BuildTitleMapping(string jsonPath)
        {
            if (!File.Exists(jsonPath)) return;

            Console.WriteLine("Building title→ASIN mapping for CTR matching...");
            int scanned = 0;

            foreach (var line in File.ReadLines(jsonPath))
            {
                scanned++;
                if (scanned % 100000 == 0) Console.WriteLine($"  Scanned {scanned:N0} products for title mapping...");
                try
                {
                    using var doc = JsonDocument.Parse(line);
                    var root = doc.RootElement;
                    string? asin = null;
                    if (root.TryGetProperty("parent_asin", out var pa)) asin = pa.GetString();
                    else if (root.TryGetProperty("asin", out var a)) asin = a.GetString();
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
    }
}
