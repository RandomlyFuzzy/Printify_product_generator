using System.Text.Json;

namespace PrintifyGenerator.AnalyticsApi.Models;

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
