using System.Text.Json;

namespace PrintifyGenerator.Library.Tests.Modules.Utility;

public class PublishingEligibilityEvaluatorTests
{
    private const float DefaultMinScore = 6.0f;

    [Fact]
    public void Evaluate_WithNullSuitability_ReturnsPendingResult()
    {
        var result = PublishingEligibilityEvaluator.Evaluate("/some/path.png", null, DefaultMinScore, null);

        Assert.Equal("automatic", result.OverrideMode);
        Assert.False(result.HasManualOverride);
        Assert.False(result.HasSuitability);
        Assert.False(result.PassesSafetyChecks);
        Assert.False(result.PassesScoreThreshold);
        Assert.False(result.IsEligibleForPublishing);
        Assert.Equal(DefaultMinScore, result.MinimumPublishScore);
        Assert.Null(result.OverallScore);
        Assert.Contains("pending", result.Reason, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Evaluate_WithSuitableImageAboveThreshold_ReturnsEligible()
    {
        var suitability = CreateSuitableImage(8.0f);

        var result = PublishingEligibilityEvaluator.Evaluate("/path.png", suitability, DefaultMinScore, null);

        Assert.True(result.HasSuitability);
        Assert.True(result.PassesSafetyChecks);
        Assert.True(result.PassesScoreThreshold);
        Assert.True(result.IsEligibleForPublishing);
        Assert.Equal(8.0f, result.OverallScore);
    }

    [Fact]
    public void Evaluate_WithSuitableImageBelowThreshold_ReturnsNotEligible()
    {
        var suitability = CreateSuitableImage(4.0f);

        var result = PublishingEligibilityEvaluator.Evaluate("/path.png", suitability, DefaultMinScore, null);

        Assert.True(result.HasSuitability);
        Assert.True(result.PassesSafetyChecks);
        Assert.False(result.PassesScoreThreshold);
        Assert.False(result.IsEligibleForPublishing);
        Assert.Equal(4.0f, result.OverallScore);
        Assert.Contains("Score too low", result.Reason);
    }

    [Fact]
    public void Evaluate_WithSafetyViolation_BlocksPublishing()
    {
        var suitability = CreateSuitableImage(8.0f);
        suitability.DoesViolateLaw = true;

        var result = PublishingEligibilityEvaluator.Evaluate("/path.png", suitability, DefaultMinScore, null);

        Assert.False(result.PassesSafetyChecks);
        Assert.False(result.IsEligibleForPublishing);
        Assert.Contains("law risk", result.Reason);
    }

    [Fact]
    public void Evaluate_WithNSFW_BlocksPublishing()
    {
        var suitability = CreateSuitableImage(8.0f);
        suitability.IsNSFW = true;

        var result = PublishingEligibilityEvaluator.Evaluate("/path.png", suitability, DefaultMinScore, null);

        Assert.False(result.PassesSafetyChecks);
        Assert.False(result.IsEligibleForPublishing);
        Assert.Contains("NSFW", result.Reason);
    }

    [Fact]
    public void Evaluate_WithIPViolation_BlocksPublishing()
    {
        var suitability = CreateSuitableImage(8.0f);
        suitability.DoesViolateIPRights = true;

        var result = PublishingEligibilityEvaluator.Evaluate("/path.png", suitability, DefaultMinScore, null);

        Assert.False(result.PassesSafetyChecks);
        Assert.False(result.IsEligibleForPublishing);
        Assert.Contains("IP", result.Reason);
    }

    [Fact]
    public void Evaluate_WithForceAllowOverride_AllowsEvenWhenBelowThreshold()
    {
        var suitability = CreateSuitableImage(3.0f);
        var overrides = CreateOverrides("/path.png", PublishingOverrideModes.ForceAllow);

        var result = PublishingEligibilityEvaluator.Evaluate("/path.png", suitability, DefaultMinScore, overrides);

        Assert.True(result.HasManualOverride);
        Assert.True(result.PassesSafetyChecks);
        Assert.False(result.PassesScoreThreshold);
        Assert.True(result.IsEligibleForPublishing);
        Assert.Contains("manually", result.Reason);
    }

    [Fact]
    public void Evaluate_WithForceBlockOverride_BlocksEvenWhenPassing()
    {
        var suitability = CreateSuitableImage(9.0f);
        var overrides = CreateOverrides("/path.png", PublishingOverrideModes.ForceBlock);

        var result = PublishingEligibilityEvaluator.Evaluate("/path.png", suitability, DefaultMinScore, overrides);

        Assert.True(result.HasManualOverride);
        Assert.False(result.IsEligibleForPublishing);
        Assert.Contains("Blocked manually", result.Reason);
    }

    [Fact]
    public void Evaluate_WithForceBlockAndNullSuitability_StillBlocks()
    {
        var overrides = CreateOverrides("/path.png", PublishingOverrideModes.ForceBlock);

        var result = PublishingEligibilityEvaluator.Evaluate("/path.png", null, DefaultMinScore, overrides);

        Assert.True(result.HasManualOverride);
        Assert.False(result.HasSuitability);
        Assert.False(result.IsEligibleForPublishing);
    }

    [Fact]
    public void Evaluate_WithOverrideForDifferentPath_DoesNotApply()
    {
        var suitability = CreateSuitableImage(8.0f);
        var overrides = CreateOverrides("/other/path.png", PublishingOverrideModes.ForceBlock);

        var result = PublishingEligibilityEvaluator.Evaluate("/path.png", suitability, DefaultMinScore, overrides);

        Assert.False(result.HasManualOverride);
        Assert.True(result.IsEligibleForPublishing);
    }

    [Fact]
    public void Evaluate_WithNullImagePath_ReturnsWithoutOverride()
    {
        var suitability = CreateSuitableImage(8.0f);
        var overrides = CreateOverrides("/path.png", PublishingOverrideModes.ForceAllow);

        var result = PublishingEligibilityEvaluator.Evaluate(null, suitability, DefaultMinScore, overrides);

        Assert.False(result.HasManualOverride);
    }

    [Fact]
    public void Evaluate_WithMultipleSafetyViolations_ListsAll()
    {
        var suitability = CreateSuitableImage(8.0f);
        suitability.DoesViolateLaw = true;
        suitability.DoesViolateIPRights = true;
        suitability.IsNSFW = true;

        var result = PublishingEligibilityEvaluator.Evaluate("/path.png", suitability, DefaultMinScore, null);

        Assert.Contains("law risk", result.Reason);
        Assert.Contains("IP risk", result.Reason);
        Assert.Contains("NSFW content", result.Reason);
    }

    [Fact]
    public void Evaluate_AtExactlyThreshold_IsEligible()
    {
        var suitability = CreateSuitableImage(6.0f);

        var result = PublishingEligibilityEvaluator.Evaluate("/path.png", suitability, 6.0f, null);

        Assert.True(result.PassesScoreThreshold);
        Assert.True(result.IsEligibleForPublishing);
    }

    private static ImageSuitability CreateSuitableImage(float overallScore)
    {
        var scoring = new Scoring
        {
            commercialAppeal = overallScore,
            printQuality = overallScore,
            estimatedSalesViability = overallScore,
            uniqueness = overallScore,
            technicalSkill = overallScore,
            creativity = overallScore,
            composition = overallScore,
            technique = overallScore,
            originality = overallScore
        };

        return new ImageSuitability
        {
            DoesViolateLaw = false,
            DoesViolateIPRights = false,
            IsNSFW = false,
            Scoring = scoring,
            Issues = new List<string>()
        };
    }

    private static PublishingOverrideCollection CreateOverrides(string imagePath, string mode)
    {
        return new PublishingOverrideCollection
        {
            Overrides = new List<ImagePublishingOverride>
            {
                new()
                {
                    ImagePath = imagePath,
                    Mode = mode,
                    UpdatedAtUtc = DateTime.UtcNow
                }
            }
        };
    }
}
