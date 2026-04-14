using System;
using System.Collections.Generic;

public static class PublishingEligibilityEvaluator
{
    public static PublishingEligibilityResult Evaluate(
        string? imagePath,
        ImageSuitability? suitability,
        float minimumPublishScore,
        PublishingOverrideCollection? overrides)
    {
        var overrideEntry = PublishingOverrideStore.Find(imagePath, overrides);
        var overrideMode = PublishingOverrideModes.Normalize(overrideEntry?.Mode);
        var hasManualOverride = overrideMode != PublishingOverrideModes.Automatic;

        if (suitability is null)
        {
            return new PublishingEligibilityResult(
                overrideMode,
                hasManualOverride,
                false,
                false,
                false,
                false,
                minimumPublishScore,
                null,
                "QC pending: waiting for phase_3.json",
                overrideEntry?.UpdatedAtUtc);
        }

        var overallScore = suitability.OverallScore();
        var passesSafetyChecks = suitability.IsSuitableForPrint();
        var passesScoreThreshold = overallScore >= minimumPublishScore;

        if (overrideMode == PublishingOverrideModes.ForceBlock)
        {
            return new PublishingEligibilityResult(
                overrideMode,
                true,
                true,
                passesSafetyChecks,
                passesScoreThreshold,
                false,
                minimumPublishScore,
                overallScore,
                "Blocked manually",
                overrideEntry?.UpdatedAtUtc);
        }

        if (!passesSafetyChecks)
        {
            return new PublishingEligibilityResult(
                overrideMode,
                hasManualOverride,
                true,
                false,
                passesScoreThreshold,
                false,
                minimumPublishScore,
                overallScore,
                BuildSafetyFailureReason(suitability),
                overrideEntry?.UpdatedAtUtc);
        }

        if (overrideMode == PublishingOverrideModes.ForceAllow)
        {
            return new PublishingEligibilityResult(
                overrideMode,
                true,
                true,
                true,
                passesScoreThreshold,
                true,
                minimumPublishScore,
                overallScore,
                passesScoreThreshold
                    ? "Allowed manually"
                    : $"Allowed manually despite low score ({overallScore:0.0} / {minimumPublishScore:0.0})",
                overrideEntry?.UpdatedAtUtc);
        }

        if (!passesScoreThreshold)
        {
            return new PublishingEligibilityResult(
                overrideMode,
                false,
                true,
                true,
                false,
                false,
                minimumPublishScore,
                overallScore,
                $"Score too low ({overallScore:0.0} / {minimumPublishScore:0.0})",
                overrideEntry?.UpdatedAtUtc);
        }

        return new PublishingEligibilityResult(
            overrideMode,
            false,
            true,
            true,
            true,
            true,
            minimumPublishScore,
            overallScore,
            $"Passed safety checks and minimum score ({overallScore:0.0} / {minimumPublishScore:0.0})",
            overrideEntry?.UpdatedAtUtc);
    }

    private static string BuildSafetyFailureReason(ImageSuitability suitability)
    {
        var reasons = new List<string>();

        if (suitability.DoesViolateLaw)
            reasons.Add("law risk");

        if (suitability.DoesViolateIPRights)
            reasons.Add("IP risk");

        if (suitability.IsNSFW)
            reasons.Add("NSFW content");

        return reasons.Count == 0
            ? "Failed safety checks"
            : $"Failed safety checks: {string.Join(", ", reasons)}";
    }
}