using System.Text.Json;

public sealed record AnalyticsApiServiceStatus(
    string Service,
    string Status,
    string Docs);

public sealed record PhaseBundleSummary(
    Guid BundleId,
    string BundlePath,
    string[] CompletedPhases,
    DateTime LastUpdatedUtc);

public sealed record PhaseArtifactResult(
    Guid BundleId,
    string FileName,
    string Phase,
    string ContentType,
    JsonElement? Json,
    string? Text,
    DateTime LastUpdatedUtc);

public sealed record PhaseArtifactReference(
    Guid BundleId,
    string Phase,
    string FilePath,
    DateTime LastUpdatedUtc);

public sealed record PhaseOverviewItem(
    string Phase,
    int ArtifactCount,
    DateTime? LastUpdatedUtc);

public sealed record IngestProductDefinitionRequest(
    Guid? BundleId,
    string Title,
    string Description,
    string MainCategory,
    string SubCategory,
    string Audience,
    string PrimaryColor,
    string Material,
    string UseCase,
    string[] Keywords);

public sealed record IngestPhaseDataRequest(
    Guid? BundleId,
    string Phase,
    string FileName,
    JsonElement Payload);

public sealed record IngestResult(
    Guid BundleId,
    string FilePath,
    string Message);

public sealed record MarketKeywordMetric(
    string Term,
    double Score,
    double AvgCtrLift,
    double AvgPositivePct,
    int TopHits,
    int BottomHits);

public sealed record MarketColorMetric(
    string Name,
    double Score,
    int ProductCount,
    long TotalSales,
    double Sentiment);

public sealed record MarketSummaryResponse(
    int CategoryFileCount,
    int ParsedCategoryCount,
    MarketKeywordMetric[] TopKeywords,
    MarketColorMetric[] TopColors,
    MarketColorMetric[] TopMaterials,
    DateTime GeneratedUtc);

public sealed record CategoryFeatureIngestRequest(
    string FileName,
    string Content);

public sealed record FileIngestResult(
    string FilePath,
    string Message);

public sealed record CategoryListItem(
    string Name,
    string Type,
    int ProductCount);

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