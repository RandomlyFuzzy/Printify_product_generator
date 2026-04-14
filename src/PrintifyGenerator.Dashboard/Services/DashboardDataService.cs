using System.Text.Json;
using Microsoft.Extensions.Options;
using PrintifyGenerator.Dashboard.Models;

namespace PrintifyGenerator.Dashboard.Services;

public sealed class DashboardDataService
{
    private static readonly JsonSerializerOptions JsonOptions = new JsonSerializerOptions
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly IWebHostEnvironment _environment;
    private readonly IOptionsMonitor<DashboardOptions> _options;

    public DashboardDataService(IWebHostEnvironment environment, IOptionsMonitor<DashboardOptions> options)
    {
        _environment = environment;
        _options = options;
    }

    public DashboardSnapshot LoadSnapshot(string? statusFilter, int? itemLimitOverride = null, string? focusedImagePath = null)
    {
        var options = _options.CurrentValue;
        var dataRoot = ResolveDataRoot();
        var checkingRoot = Path.Combine(dataRoot, "Checking");
        var draftsRoot = Path.Combine(dataRoot, "staging", "drafts");
        var settings = OrchestrationSettingsStore.Load(dataRoot);
        var publishingOverrides = PublishingOverrideStore.Load(dataRoot);
        var draftsByImage = LoadDraftRecords(draftsRoot);
        var allImages = LoadGalleryItems(checkingRoot, draftsByImage, settings.MinimumPublishScore, publishingOverrides)
            .OrderByDescending(item => item.CapturedAtUtc)
            .ToList();

        var normalizedFilter = GalleryStatusFilter.Normalize(statusFilter);
        var normalizedFocusedImagePath = NormalizeOptionalPath(focusedImagePath);
        var filteredQuery = normalizedFocusedImagePath is null
            ? allImages.Where(item => GalleryStatusFilter.Matches(item.Status, normalizedFilter))
            : allImages.Where(item => string.Equals(item.ImagePath, normalizedFocusedImagePath, StringComparison.OrdinalIgnoreCase));
        var filteredImages = (itemLimitOverride.HasValue && itemLimitOverride.Value <= 0
                ? filteredQuery
                : filteredQuery.Take(itemLimitOverride ?? Math.Max(1, options.GalleryItemLimit)))
            .ToList();

        var reviewedImages = allImages.Where(item => item.HasSuitability && item.ReviewScore.HasValue).ToList();
        var dailyVolumes = allImages
            .GroupBy(item => DateOnly.FromDateTime(item.CapturedAtUtc.ToLocalTime().Date))
            .OrderByDescending(group => group.Key)
            .Take(14)
            .Select(group => new DailyVolumeEntry(group.Key, group.Count()))
            .ToList();

        var scoreBands = BuildScoreBands(reviewedImages);

        var summary = new DashboardSummary(
            TotalImages: allImages.Count,
            ReviewedImages: reviewedImages.Count,
            PendingReviewImages: allImages.Count(item => item.Status == GalleryStatusFilter.Pending),
            EligibleForPublishingImages: allImages.Count(item => item.IsEligibleForPublishing && item.DraftCount == 0),
            DraftedImages: allImages.Count(item => item.DraftCount > 0),
            BlockedImages: allImages.Count(item => item.HasSuitability && !item.IsEligibleForPublishing),
            ForceAllowedImages: allImages.Count(item => item.PublishOverrideMode == PublishingOverrideModes.ForceAllow),
            ForceBlockedImages: allImages.Count(item => item.PublishOverrideMode == PublishingOverrideModes.ForceBlock),
            TotalDrafts: allImages.Sum(item => item.DraftCount),
            EnabledOllamaNodes: settings.Ollama.Count(node => node.Enabled),
            EnabledComfyUiNodes: settings.ComfyUi.Count(node => node.Enabled),
            AverageReviewedScore: reviewedImages.Count == 0 ? 0 : reviewedImages.Average(item => item.ReviewScore ?? 0));

        return new DashboardSnapshot(
            summary,
            dailyVolumes,
            scoreBands,
            filteredImages,
            settings,
            dataRoot,
            checkingRoot,
            OrchestrationSettingsStore.GetSettingsPath(dataRoot),
            PublishingOverrideStore.GetOverridesPath(dataRoot));
    }

    public OrchestrationSettings LoadSettings()
    {
        return OrchestrationSettingsStore.Load(ResolveDataRoot());
    }

    public void SaveSettings(OrchestrationSettings settings)
    {
        OrchestrationSettingsStore.Save(ResolveDataRoot(), settings);
    }

    public void SetPublishingOverride(string imagePath, string mode)
    {
        if (string.IsNullOrWhiteSpace(imagePath))
            throw new InvalidOperationException("An image path is required.");

        var normalizedImagePath = Path.GetFullPath(imagePath);
        var checkingRoot = Path.GetFullPath(Path.Combine(ResolveDataRoot(), "Checking"));
        var relativePath = Path.GetRelativePath(checkingRoot, normalizedImagePath);
        if (relativePath.StartsWith("..", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Only generated images inside the checking folder can be overridden.");

        PublishingOverrideStore.SetOverride(ResolveDataRoot(), normalizedImagePath, mode);
    }

    private string ResolveDataRoot()
    {
        return DashboardOptions.ResolveDataRoot(_options.CurrentValue.DataRoot, _environment.ContentRootPath);
    }

    private static Dictionary<string, List<DraftRecordSummary>> LoadDraftRecords(string draftsRoot)
    {
        var lookup = new Dictionary<string, List<DraftRecordSummary>>(StringComparer.OrdinalIgnoreCase);
        if (!Directory.Exists(draftsRoot))
            return lookup;

        foreach (var filePath in Directory.EnumerateFiles(draftsRoot, "*.json", SearchOption.AllDirectories))
        {
            try
            {
                using var document = JsonDocument.Parse(File.ReadAllText(filePath));
                var root = document.RootElement;
                if (!root.TryGetProperty("local_image_path", out var imagePathProperty))
                    continue;

                var imagePath = imagePathProperty.GetString();
                if (string.IsNullOrWhiteSpace(imagePath))
                    continue;

                var normalizedImagePath = Path.GetFullPath(imagePath);
                var productId = root.TryGetProperty("product_id", out var productIdProperty)
                    ? productIdProperty.GetString() ?? Path.GetFileNameWithoutExtension(filePath)
                    : Path.GetFileNameWithoutExtension(filePath);
                var blueprintTitle = root.TryGetProperty("blueprint_title", out var blueprintProperty)
                    ? blueprintProperty.GetString() ?? "Draft"
                    : "Draft";
                var createdAtUtc = root.TryGetProperty("created_at", out var createdAtProperty)
                    ? ParseUtc(createdAtProperty.GetString(), File.GetLastWriteTimeUtc(filePath))
                    : File.GetLastWriteTimeUtc(filePath);

                if (!lookup.TryGetValue(normalizedImagePath, out var draftList))
                {
                    draftList = new List<DraftRecordSummary>();
                    lookup[normalizedImagePath] = draftList;
                }

                draftList.Add(new DraftRecordSummary(productId, blueprintTitle, createdAtUtc));
            }
            catch
            {
                // Ignore malformed draft records and keep the dashboard live.
            }
        }

        return lookup;
    }

    private static IEnumerable<GalleryItem> LoadGalleryItems(
        string checkingRoot,
        IReadOnlyDictionary<string, List<DraftRecordSummary>> draftsByImage,
        float minimumPublishScore,
        PublishingOverrideCollection publishingOverrides)
    {
        if (!Directory.Exists(checkingRoot))
            yield break;

        foreach (var imagePath in EnumerateImageFiles(checkingRoot))
        {
            var folderPath = Path.GetDirectoryName(imagePath);
            if (string.IsNullOrWhiteSpace(folderPath))
                continue;

            var normalizedImagePath = Path.GetFullPath(imagePath);
            var promptPath = Path.Combine(folderPath, "phase_1.json");
            var suitabilityPath = Path.Combine(folderPath, "phase_3.json");
            var prompt = TryDeserialize<Prompt>(promptPath);
            var suitability = TryDeserialize<ImageSuitability>(suitabilityPath);
            var draftList = draftsByImage.TryGetValue(normalizedImagePath, out var drafts)
                ? drafts.OrderByDescending(item => item.CreatedAtUtc).ToList()
                : new List<DraftRecordSummary>();
            var eligibility = PublishingEligibilityEvaluator.Evaluate(normalizedImagePath, suitability, minimumPublishScore, publishingOverrides);
            float? reviewScore = suitability is null ? null : suitability.OverallScore();
            var status = ResolveStatus(eligibility, draftList.Count);
            var relativeImagePath = Path.GetRelativePath(checkingRoot, normalizedImagePath);
            var relativeDirectory = Path.GetDirectoryName(relativeImagePath)?.Replace(Path.DirectorySeparatorChar, '/') ?? string.Empty;
            var promptSettingsLabel = prompt is null
                ? "Prompt missing"
                : $"{prompt.width} x {prompt.height} · {prompt.steps} steps · cfg {prompt.cfg:0.#}";

            yield return new GalleryItem(
                JobId: Path.GetFileNameWithoutExtension(normalizedImagePath),
                ImagePath: normalizedImagePath,
                PromptPath: promptPath,
                SuitabilityPath: suitabilityPath,
                ImageUrl: BuildGeneratedUrl(checkingRoot, normalizedImagePath),
                RelativeDirectory: relativeDirectory,
                PositivePromptPreview: PreviewPrompt(prompt?.positive),
                PositivePrompt: prompt?.positive?.Trim() ?? string.Empty,
                NegativePrompt: prompt?.negative?.Trim() ?? string.Empty,
                PromptSettingsLabel: promptSettingsLabel,
                Width: prompt?.width,
                Height: prompt?.height,
                Steps: prompt?.steps,
                Cfg: prompt?.cfg,
                CapturedAtUtc: File.GetLastWriteTimeUtc(normalizedImagePath),
                ReviewScore: reviewScore,
                SuitabilityScore: suitability?.suitability,
                HasSuitability: suitability is not null,
                PassesSafetyChecks: eligibility.PassesSafetyChecks,
                DoesViolateLaw: suitability?.DoesViolateLaw ?? false,
                DoesViolateIpRights: suitability?.DoesViolateIPRights ?? false,
                IsNsfw: suitability?.IsNSFW ?? false,
                IsEligibleForPublishing: eligibility.IsEligibleForPublishing,
                CanForceAllow: suitability is not null && eligibility.PassesSafetyChecks,
                MinimumPublishScore: eligibility.MinimumPublishScore,
                EligibilityReason: eligibility.Reason,
                PublishOverrideMode: eligibility.OverrideMode,
                PublishOverrideLabel: PublishingOverrideModes.ToDisplayName(eligibility.OverrideMode),
                PublishOverrideUpdatedAtUtc: eligibility.OverrideUpdatedAtUtc,
                DraftCount: draftList.Count,
                Drafts: draftList,
                Issues: suitability?.Issues?.Where(issue => !string.IsNullOrWhiteSpace(issue)).ToList() ?? new List<string>(),
                ScoreBreakdown: BuildScoreBreakdown(suitability),
                Status: status.Value,
                StatusLabel: status.Label,
                StatusCssClass: status.CssClass);
        }
    }

    private static IReadOnlyList<ScoreBandEntry> BuildScoreBands(IReadOnlyCollection<GalleryItem> reviewedImages)
    {
        return new[]
        {
            new ScoreBandEntry("8.0-10", reviewedImages.Count(item => (item.ReviewScore ?? -1) >= 8.0f)),
            new ScoreBandEntry("6.0-7.9", reviewedImages.Count(item => (item.ReviewScore ?? -1) >= 6.0f && (item.ReviewScore ?? -1) < 8.0f)),
            new ScoreBandEntry("4.0-5.9", reviewedImages.Count(item => (item.ReviewScore ?? -1) >= 4.0f && (item.ReviewScore ?? -1) < 6.0f)),
            new ScoreBandEntry("0.0-3.9", reviewedImages.Count(item => (item.ReviewScore ?? -1) >= 0.0f && (item.ReviewScore ?? -1) < 4.0f))
        };
    }

    private static IReadOnlyList<ScoreBreakdownEntry> BuildScoreBreakdown(ImageSuitability? suitability)
    {
        if (suitability is null)
            return Array.Empty<ScoreBreakdownEntry>();

        var scoring = suitability.Scoring ?? new Scoring();
        return new[]
        {
            new ScoreBreakdownEntry("Commercial", scoring.commercialAppeal),
            new ScoreBreakdownEntry("Print", scoring.printQuality),
            new ScoreBreakdownEntry("Sales", scoring.estimatedSalesViability),
            new ScoreBreakdownEntry("Unique", scoring.uniqueness),
            new ScoreBreakdownEntry("Technical", scoring.technicalSkill),
            new ScoreBreakdownEntry("Creative", scoring.creativity),
            new ScoreBreakdownEntry("Composition", scoring.composition),
            new ScoreBreakdownEntry("Technique", scoring.technique),
            new ScoreBreakdownEntry("Originality", scoring.originality)
        };
    }

    private static IEnumerable<string> EnumerateImageFiles(string checkingRoot)
    {
        var allowedPatterns = new[] { "*.png", "*.jpg", "*.jpeg", "*.webp" };

        return allowedPatterns
            .SelectMany(pattern => Directory.EnumerateFiles(checkingRoot, pattern, SearchOption.AllDirectories))
            .Distinct(StringComparer.OrdinalIgnoreCase);
    }

    private static T? TryDeserialize<T>(string filePath)
    {
        if (!File.Exists(filePath))
            return default;

        try
        {
            return JsonSerializer.Deserialize<T>(File.ReadAllText(filePath), JsonOptions);
        }
        catch
        {
            return default;
        }
    }

    private static string? NormalizeOptionalPath(string? filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath))
            return null;

        try
        {
            return Path.GetFullPath(filePath);
        }
        catch
        {
            return null;
        }
    }

    private static string BuildGeneratedUrl(string checkingRoot, string imagePath)
    {
        var relativePath = Path.GetRelativePath(checkingRoot, imagePath)
            .Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
            .Where(segment => !string.IsNullOrWhiteSpace(segment))
            .Select(Uri.EscapeDataString);

        return "/generated/" + string.Join('/', relativePath);
    }

    private static DateTime ParseUtc(string? rawValue, DateTime fallbackUtc)
    {
        if (DateTimeOffset.TryParse(rawValue, out var parsed))
            return parsed.UtcDateTime;

        return fallbackUtc;
    }

    private static string PreviewPrompt(string? prompt)
    {
        if (string.IsNullOrWhiteSpace(prompt))
            return "No prompt captured for this image.";

        var compact = prompt.Trim();
        return compact.Length <= 180 ? compact : compact[..177] + "...";
    }

    private static GalleryStatus ResolveStatus(PublishingEligibilityResult eligibility, int draftCount)
    {
        if (!eligibility.HasSuitability)
            return new GalleryStatus(GalleryStatusFilter.Pending, "Pending review", "status-pending");

        if (draftCount > 0)
            return new GalleryStatus(GalleryStatusFilter.Drafted, "Drafted", "status-drafted");

        if (eligibility.IsEligibleForPublishing)
            return new GalleryStatus(GalleryStatusFilter.Ready, "Ready", "status-ready");

        return new GalleryStatus(GalleryStatusFilter.Blocked, "Blocked", "status-blocked");
    }
}

public static class GalleryStatusFilter
{
    public const string All = "all";
    public const string Pending = "pending";
    public const string Ready = "ready";
    public const string Drafted = "drafted";
    public const string Blocked = "blocked";

    public static string Normalize(string? value)
    {
        return value?.Trim().ToLowerInvariant() switch
        {
            Ready => Ready,
            Pending => Pending,
            Drafted => Drafted,
            Blocked => Blocked,
            _ => All
        };
    }

    public static bool Matches(string itemStatus, string filter)
    {
        return Normalize(filter) == All || string.Equals(itemStatus, Normalize(filter), StringComparison.OrdinalIgnoreCase);
    }
}

public sealed record DashboardSnapshot(
    DashboardSummary Summary,
    IReadOnlyList<DailyVolumeEntry> DailyVolumes,
    IReadOnlyList<ScoreBandEntry> ScoreBands,
    IReadOnlyList<GalleryItem> Images,
    OrchestrationSettings Settings,
    string DataRoot,
    string CheckingRoot,
    string SettingsPath,
    string PublishingOverridesPath)
{
    public static DashboardSnapshot Empty { get; } = new(
        new DashboardSummary(0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0),
        Array.Empty<DailyVolumeEntry>(),
        Array.Empty<ScoreBandEntry>(),
        Array.Empty<GalleryItem>(),
        new OrchestrationSettings(),
        string.Empty,
        string.Empty,
        string.Empty,
        string.Empty);
}

public sealed record DashboardSummary(
    int TotalImages,
    int ReviewedImages,
    int PendingReviewImages,
    int EligibleForPublishingImages,
    int DraftedImages,
    int BlockedImages,
    int ForceAllowedImages,
    int ForceBlockedImages,
    int TotalDrafts,
    int EnabledOllamaNodes,
    int EnabledComfyUiNodes,
    double AverageReviewedScore)
{
    public int EnabledNodeCount => EnabledOllamaNodes + EnabledComfyUiNodes;
}

public sealed record DailyVolumeEntry(DateOnly Day, int Count);

public sealed record ScoreBandEntry(string Label, int Count);

public sealed record ScoreBreakdownEntry(string Label, float Value)
{
    public int Percentage => (int)Math.Clamp(Math.Round(Value / 10.0 * 100.0), 0, 100);
}

public sealed record GalleryItem(
    string JobId,
    string ImagePath,
    string PromptPath,
    string SuitabilityPath,
    string ImageUrl,
    string RelativeDirectory,
    string PositivePromptPreview,
    string PositivePrompt,
    string NegativePrompt,
    string PromptSettingsLabel,
    int? Width,
    int? Height,
    int? Steps,
    float? Cfg,
    DateTime CapturedAtUtc,
    float? ReviewScore,
    float? SuitabilityScore,
    bool HasSuitability,
    bool PassesSafetyChecks,
    bool DoesViolateLaw,
    bool DoesViolateIpRights,
    bool IsNsfw,
    bool IsEligibleForPublishing,
    bool CanForceAllow,
    float MinimumPublishScore,
    string EligibilityReason,
    string PublishOverrideMode,
    string PublishOverrideLabel,
    DateTime? PublishOverrideUpdatedAtUtc,
    int DraftCount,
    IReadOnlyList<DraftRecordSummary> Drafts,
    IReadOnlyList<string> Issues,
    IReadOnlyList<ScoreBreakdownEntry> ScoreBreakdown,
    string Status,
    string StatusLabel,
    string StatusCssClass);

public sealed record DraftRecordSummary(string ProductId, string BlueprintTitle, DateTime CreatedAtUtc);

public sealed record GalleryStatus(string Value, string Label, string CssClass);