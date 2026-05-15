namespace PrintifyGenerator.Library.Tests.DataStructures;

public class ImageSuitabilityTests
{
    [Fact]
    public void IsValid_WithDefaultValues_ReturnsTrue()
    {
        var sut = new ImageSuitability();
        Assert.True(sut.isValid());
    }

    [Fact]
    public void IsValid_WithNegativeSuitability_ReturnsFalse()
    {
        var sut = new ImageSuitability { suitability = -1.0f };
        Assert.False(sut.isValid());
    }

    [Fact]
    public void IsValid_WithSuitabilityAboveTen_ReturnsFalse()
    {
        var sut = new ImageSuitability { suitability = 11.0f };
        Assert.False(sut.isValid());
    }

    [Fact]
    public void HasIssues_WithDefaultList_ReturnsTrue()
    {
        var sut = new ImageSuitability();
        Assert.True(sut.HasIssues());
    }

    [Fact]
    public void HasIssues_WithEmptyList_ReturnsFalse()
    {
        var sut = new ImageSuitability { Issues = new List<string>() };
        Assert.False(sut.HasIssues());
    }

    [Fact]
    public void HasIssues_WithNullList_ReturnsFalse()
    {
        var sut = new ImageSuitability { Issues = null! };
        Assert.False(sut.HasIssues());
    }

    [Fact]
    public void IsSuitableForPrint_AllFlagsFalse_ReturnsTrue()
    {
        var sut = new ImageSuitability
        {
            DoesViolateLaw = false,
            DoesViolateIPRights = false,
            IsNSFW = false
        };
        Assert.True(sut.IsSuitableForPrint());
    }

    [Fact]
    public void IsSuitableForPrint_WithViolatesLaw_ReturnsFalse()
    {
        var sut = new ImageSuitability { DoesViolateLaw = true };
        Assert.False(sut.IsSuitableForPrint());
    }

    [Fact]
    public void IsSuitableForPrint_WithViolatesIPRights_ReturnsFalse()
    {
        var sut = new ImageSuitability { DoesViolateIPRights = true };
        Assert.False(sut.IsSuitableForPrint());
    }

    [Fact]
    public void IsSuitableForPrint_WithIsNSFW_ReturnsFalse()
    {
        var sut = new ImageSuitability { IsNSFW = true };
        Assert.False(sut.IsSuitableForPrint());
    }

    [Fact]
    public void OverallScore_WithAllMax_ReturnsTen()
    {
        var sut = new ImageSuitability
        {
            Scoring = new Scoring
            {
                commercialAppeal = 10.0f,
                printQuality = 10.0f,
                estimatedSalesViability = 10.0f,
                uniqueness = 10.0f,
                technicalSkill = 10.0f,
                creativity = 10.0f,
                composition = 10.0f,
                technique = 10.0f,
                originality = 10.0f
            }
        };
        Assert.Equal(10.0f, sut.OverallScore());
    }

    [Fact]
    public void OverallScore_WithAllMin_ReturnsZero()
    {
        var sut = new ImageSuitability
        {
            Scoring = new Scoring
            {
                commercialAppeal = 0.0f,
                printQuality = 0.0f,
                estimatedSalesViability = 0.0f,
                uniqueness = 0.0f,
                technicalSkill = 0.0f,
                creativity = 0.0f,
                composition = 0.0f,
                technique = 0.0f,
                originality = 0.0f
            }
        };
        Assert.Equal(0.0f, sut.OverallScore());
    }

    [Fact]
    public void OverallScore_WithMixedValues_ReturnsCorrectAverage()
    {
        var sut = new ImageSuitability
        {
            Scoring = new Scoring
            {
                commercialAppeal = 8.0f,
                printQuality = 6.0f,
                estimatedSalesViability = 7.0f,
                uniqueness = 5.0f,
                technicalSkill = 9.0f,
                creativity = 8.0f,
                composition = 7.0f,
                technique = 6.0f,
                originality = 5.0f
            }
        };
        var expected = (8.0f + 6.0f + 7.0f + 5.0f + 9.0f + 8.0f + 7.0f + 6.0f + 5.0f) / 9.0f;
        Assert.Equal(expected, sut.OverallScore());
    }

    [Fact]
    public void PrettyJsonString_ReturnsIndentedJson()
    {
        var sut = new ImageSuitability();
        var json = sut.PrettyJsonString();
        Assert.Contains("\n", json);
        Assert.Contains("\"suitability\"", json);
    }

    [Fact]
    public void ToJsonString_ReturnsCompactJson()
    {
        var sut = new ImageSuitability { suitability = 5.0f };
        var json = sut.ToJsonString();
        Assert.DoesNotContain("\n", json.Trim());
    }
}
