using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using PrintifyGenerator.Researcher.Interfaces;

namespace PrintifyGenerator.Researcher.Datasets
{
    public class WomenClothingSalesDataset : IDatasetProvider
    {
        public string Name => "Women Clothing Ecommerce Sales";
        public string Type => "WomenClothingSales";

        private readonly string _csvPath;
        private readonly Dictionary<string, SalesData> _orderData = new();
        private readonly Dictionary<string, List<SalesData>> _colorData = new();
        private readonly Dictionary<string, List<SalesData>> _sizeData = new();

        public class SalesData
        {
            public string OrderId { get; set; } = "";
            public DateTime OrderDate { get; set; }
            public string Sku { get; set; } = "";
            public string Color { get; set; } = "";
            public string Size { get; set; } = "";
            public decimal UnitPrice { get; set; }
            public int Quantity { get; set; }
            public decimal Revenue { get; set; }
        }

        public WomenClothingSalesDataset(string csvPath)
        {
            _csvPath = csvPath;
        }

        public async Task LoadAsync()
        {
            Console.WriteLine($"Loading Women Clothing Sales data from: {Path.GetFileName(_csvPath)}");
            int loaded = 0;

            await Task.Run(() =>
            {
                if (!File.Exists(_csvPath)) return;

                foreach (var line in File.ReadLines(_csvPath).Skip(1))
                {
                    var parts = line.Split(',');
                    if (parts.Length < 7) continue;

                    var data = new SalesData
                    {
                        OrderId = parts[0].Trim(),
                        OrderDate = ParseDateTime(parts[1].Trim()),
                        Sku = parts[2].Trim(),
                        Color = parts[3].Trim(),
                        Size = parts[4].Trim(),
                        UnitPrice = ParseDecimal(parts[5]),
                        Quantity = ParseInt(parts[6]),
                        Revenue = parts.Length > 7 ? ParseDecimal(parts[7]) : 0
                    };

                    _orderData[data.OrderId] = data;

                    if (!string.IsNullOrEmpty(data.Color))
                    {
                        if (!_colorData.TryGetValue(data.Color, out var list))
                            _colorData[data.Color] = list = new List<SalesData>();
                        list.Add(data);
                    }

                    if (!string.IsNullOrEmpty(data.Size))
                    {
                        if (!_sizeData.TryGetValue(data.Size, out var list))
                            _sizeData[data.Size] = list = new List<SalesData>();
                        list.Add(data);
                    }

                    loaded++;
                }
            });

            Console.WriteLine($"Loaded {loaded:N0} sales records");
        }

        public bool HasProductData(string key) =>
            _orderData.ContainsKey(key) || _colorData.ContainsKey(key) || _sizeData.ContainsKey(key);

        public T? GetProductData<T>(string key) where T : class
        {
            if (typeof(T) == typeof(SalesData) && _orderData.TryGetValue(key, out var data))
                return data as T;

            if (typeof(T) == typeof(List<SalesData>) && _colorData.TryGetValue(key, out var colorList))
                return colorList as T;

            return null;
        }

        public IEnumerable<string> GetAllAsins() => _orderData.Keys;

        public Dictionary<string, List<SalesData>> GetColorSales() => _colorData;
        public Dictionary<string, List<SalesData>> GetSizeSales() => _sizeData;

        private static DateTime ParseDateTime(string str)
        {
            DateTime.TryParse(str, out var result);
            return result;
        }

        private static decimal ParseDecimal(string str)
        {
            decimal.TryParse(str, out var result);
            return result;
        }

        private static int ParseInt(string str)
        {
            int.TryParse(str, out var result);
            return result;
        }
    }
}
