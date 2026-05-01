namespace PrintifyGenerator.AnalyticsApi.Models;

// ─── Category listing ────────────────────────────────────────────────────────

public sealed record CategoryListItem(
    string Name,
    string Type,
    int ProductCount);

// ─── Per-category detail ─────────────────────────────────────────────────────

public sealed record CategoryKeywordEntry(
    string Term,
    double Score,
    double CtrLift,
    double PositivePct,
    int TopHits,
    int BottomHits);

public sealed record CategoryColorEntry(
    string Name,
    double Score,
    int ProductCount,
    long TotalSales,
    double Sentiment);

public sealed record CategoryPriceEntry(
    string Term,
    double AvgPrice,
    double Ratio,
    bool IsCheaper,
    int ProductCount);

public sealed record CategoryDetailResponse(
    string Name,
    string Type,
    int ProductCount,
    CategoryKeywordEntry[] Keywords,
    CategoryColorEntry[] Colors,
    CategoryColorEntry[] Materials,
    CategoryPriceEntry[] PriceTerms,
    double MedianCompetitivePrice,
    double MinPrice,
    double MaxPrice);

// ─── Product scoring ─────────────────────────────────────────────────────────

public sealed record ProductScoreRequest(
    string Title,
    string Description,
    string Category,
    string Color,
    string Material,
    string[] Keywords);

public sealed record SignalDetail(
    string Signal,
    double Contribution,
    string Note);

public sealed record ProductScoreResponse(
    double Score,
    string Band,
    SignalDetail[] Signals,
    string[] RecommendedKeywords,
    string[] RecommendedColors,
    string[] RecommendedMaterials,
    double MedianMarketPrice,
    string[] Notes);

// ─── Cross-category overlap ──────────────────────────────────────────────────

public sealed record OverlappingFeature(
    string Term,
    string FeatureType,
    int CategoryCount,
    string[] Categories,
    double AverageScore,
    double MaxScore,
    int TotalProductCount,
    long TotalSales);

public sealed record CrossCategoryOverlapResponse(
    int MinCategoryThreshold,
    OverlappingFeature[] Keywords,
    OverlappingFeature[] Colors,
    OverlappingFeature[] Materials,
    DateTime GeneratedUtc);
