using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using PrintifyGenerator.Researcher.Interfaces;

namespace PrintifyGenerator.Researcher.Datasets
{
    public class OnlineSalesDataset : IDatasetProvider
    {
        public string Name => "Online Sales Data";
        public string Type => "OnlineSales";

        private readonly string _csvPath;
        private readonly List<OnlineSaleData> _salesData = new();
        private readonly Dictionary<string, List<OnlineSaleData>> _categoryData = new();
        private readonly Dictionary<string, List<OnlineSaleData>> _regionData = new();

        public class OnlineSaleData
        {
            public string TransactionId { get; set; } = "";
            public DateTime Date { get; set; }
            public string ProductCategory { get; set; } = "";
            public string ProductName { get; set; } = "";
            public int UnitsSold { get; set; }
            public decimal UnitPrice { get; set; }
            public decimal TotalRevenue { get; set; }
            public string Region { get; set; } = "";
            public string PaymentMethod { get; set; } = "";
        }

        public OnlineSalesDataset(string csvPath)
        {
            _csvPath = csvPath;
        }

        public async Task LoadAsync()
        {
            Console.WriteLine($"Loading Online Sales data from: {Path.GetFileName(_csvPath)}");
            int loaded = 0;

            await Task.Run(() =>
            {
                if (!File.Exists(_csvPath)) return;

                foreach (var line in File.ReadLines(_csvPath).Skip(1))
                {
                    var parts = line.Split(',');
                    if (parts.Length < 8) continue;

                    var data = new OnlineSaleData
                    {
                        TransactionId = parts[0].Trim(),
                        Date = ParseDateTime(parts[1].Trim()),
                        ProductCategory = parts[2].Trim(),
                        ProductName = parts[3].Trim(),
                        UnitsSold = ParseInt(parts[4]),
                        UnitPrice = ParseDecimal(parts[5]),
                        TotalRevenue = ParseDecimal(parts[6]),
                        Region = parts[7].Trim(),
                        PaymentMethod = parts.Length > 8 ? parts[8].Trim() : ""
                    };

                    _salesData.Add(data);

                    if (!string.IsNullOrEmpty(data.ProductCategory))
                    {
                        if (!_categoryData.TryGetValue(data.ProductCategory, out var list))
                            _categoryData[data.ProductCategory] = list = new List<OnlineSaleData>();
                        list.Add(data);
                    }

                    if (!string.IsNullOrEmpty(data.Region))
                    {
                        if (!_regionData.TryGetValue(data.Region, out var list))
                            _regionData[data.Region] = list = new List<OnlineSaleData>();
                        list.Add(data);
                    }

                    loaded++;
                }
            });

            Console.WriteLine($"Loaded {loaded:N0} online sales records");
        }

        public bool HasProductData(string key) =>
            _salesData.Any(s => s.ProductName.Equals(key, StringComparison.OrdinalIgnoreCase)) ||
            _categoryData.ContainsKey(key) || _regionData.ContainsKey(key);

        public T? GetProductData<T>(string key) where T : class
        {
            if (typeof(T) == typeof(OnlineSaleData))
            {
                var data = _salesData.FirstOrDefault(s => s.TransactionId == key);
                return data as T;
            }

            return null;
        }

        public IEnumerable<string> GetAllAsins() => _salesData.Select(s => s.TransactionId);

        public List<OnlineSaleData> GetAllSales() => _salesData;
        public Dictionary<string, List<OnlineSaleData>> GetCategorySales() => _categoryData;
        public Dictionary<string, List<OnlineSaleData>> GetRegionSales() => _regionData;

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
