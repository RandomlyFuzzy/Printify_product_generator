using System;
using System.Collections.Generic;

namespace PrintifyGenerator.Researcher.Models
{
    public class SentimentStatResult
    {
        public Dictionary<string, (int Positive, int Total)> WordSentiment { get; set; } = new();
        public Dictionary<string, (int Positive, int Total)> PhraseSentiment { get; set; } = new();
    }

    public class SalesStatResult
    {
        public Dictionary<string, (long Sales, int Count)> WordSales { get; set; } = new();
        public Dictionary<string, (long Sales, int Count)> PhraseSales { get; set; } = new();
        public Dictionary<string, int> WordTopFreq { get; set; } = new();
        public Dictionary<string, int> WordBottomFreq { get; set; } = new();
        public Dictionary<string, int> PhraseTopFreq { get; set; } = new();
        public Dictionary<string, int> PhraseBottomFreq { get; set; } = new();
    }

    public class CtrStatResult
    {
        public Dictionary<string, (long Views, long Carts, long Purchases, int ProductCount)> WordCtr { get; set; } = new();
        public Dictionary<string, (long Views, long Carts, long Purchases, int ProductCount)> PhraseCtr { get; set; } = new();
    }

    public class PriceStatResult
    {
        public Dictionary<string, (decimal TotalPrice, int Count, decimal MinPrice, decimal MaxPrice)> WordPrice { get; set; } = new();
        public Dictionary<string, (decimal TotalPrice, int Count, decimal MinPrice, decimal MaxPrice)> PhrasePrice { get; set; } = new();
        public Dictionary<string, (decimal AvgPrice, decimal PriceVsCategoryAvg, int Count)> WordPriceCompetitiveness { get; set; } = new();
        public Dictionary<string, (decimal AvgPrice, decimal PriceVsCategoryAvg, int Count)> PhrasePriceCompetitiveness { get; set; } = new();
    }

    public class ImageStatResult
    {
        public Dictionary<string, (int TotalImages, int ProductCount)> WordImages { get; set; } = new();
        public Dictionary<string, (int TotalImages, int ProductCount)> PhraseImages { get; set; } = new();
    }

    public class SeasonalityStatResult
    {
        public Dictionary<string, (int[] MonthlyCount, int TotalCount)> WordSeasonality { get; set; } = new();
        public Dictionary<string, (int[] MonthlyCount, int TotalCount)> PhraseSeasonality { get; set; } = new();
    }

    public class ColorStatResult
    {
        public Dictionary<string, (int Count, double AvgSentiment, long TotalSales)> ColorStats { get; set; } = new();
        public Dictionary<string, (int Count, double AvgSentiment)> ColorWordSentiment { get; set; } = new();
    }

    public class MaterialStatResult
    {
        public Dictionary<string, (int Count, double AvgSentiment, long TotalSales, decimal AvgPrice)> MaterialStats { get; set; } = new();
    }

    public class BrandStatResult
    {
        public Dictionary<string, (int Count, double AvgSentiment, long TotalSales, int TopCount, int BottomCount)> BrandStats { get; set; } = new();
    }
}
