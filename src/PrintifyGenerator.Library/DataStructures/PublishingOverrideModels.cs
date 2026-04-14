using System;
using System.Collections.Generic;

public sealed class PublishingOverrideCollection
{
    public List<ImagePublishingOverride> Overrides { get; set; } = new List<ImagePublishingOverride>();
}

public sealed class ImagePublishingOverride
{
    public string ImagePath { get; set; } = string.Empty;
    public string Mode { get; set; } = PublishingOverrideModes.Automatic;
    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;
}

public static class PublishingOverrideModes
{
    public const string Automatic = "automatic";
    public const string ForceAllow = "force-allow";
    public const string ForceBlock = "force-block";

    public static string Normalize(string? mode)
    {
        return mode?.Trim().ToLowerInvariant() switch
        {
            ForceAllow => ForceAllow,
            ForceBlock => ForceBlock,
            _ => Automatic
        };
    }

    public static string ToDisplayName(string? mode)
    {
        return Normalize(mode) switch
        {
            ForceAllow => "Manual allow",
            ForceBlock => "Manual block",
            _ => "Automatic"
        };
    }
}

public sealed record PublishingEligibilityResult(
    string OverrideMode,
    bool HasManualOverride,
    bool HasSuitability,
    bool PassesSafetyChecks,
    bool PassesScoreThreshold,
    bool IsEligibleForPublishing,
    float MinimumPublishScore,
    float? OverallScore,
    string Reason,
    DateTime? OverrideUpdatedAtUtc);