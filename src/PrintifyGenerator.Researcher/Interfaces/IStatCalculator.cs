using System;
using System.Collections.Generic;
using System.Text.Json;

namespace PrintifyGenerator.Researcher.Interfaces
{
    public interface IStatCalculator
    {
        string Name { get; }
        ISet<string> RequiredDatasetTypes { get; }
        void InitializeForCategory(string category);
        void ProcessWord(string word, ProductContext context);
        void ProcessPhrase(string phrase, ProductContext context);
        object GetResults();
        void WriteResults(StreamWriter writer, string category, Dictionary<string, object> additionalData);
    }

    public class ProductContext
    {
        public string Asin { get; set; } = "";
        public string Category { get; set; } = "";
        public double Sentiment { get; set; }
        public int Sales { get; set; }
        public bool IsTopSeller { get; set; }
        public bool IsBottomSeller { get; set; }
        public decimal Price { get; set; }
        public int ImageCount { get; set; }
        public JsonElement ProductJson { get; set; }
        public Dictionary<string, object> DatasetData { get; set; } = new();
    }
}
