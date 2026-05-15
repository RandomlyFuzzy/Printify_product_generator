namespace PrintifyGenerator.Library.Tests.DataStructures;

public class PublishingOverrideModesTests
{
    [Theory]
    [InlineData(null, "automatic")]
    [InlineData("", "automatic")]
    [InlineData("  ", "automatic")]
    [InlineData("automatic", "automatic")]
    [InlineData("AUTOMATIC", "automatic")]
    [InlineData("force-allow", "force-allow")]
    [InlineData("FORCE-ALLOW", "force-allow")]
    [InlineData("Force-Allow", "force-allow")]
    [InlineData("force-block", "force-block")]
    [InlineData("FORCE-BLOCK", "force-block")]
    [InlineData("Force-Block", "force-block")]
    [InlineData("unknown", "automatic")]
    [InlineData("invalid-value", "automatic")]
    public void Normalize_ReturnsExpected(string? input, string expected)
    {
        Assert.Equal(expected, PublishingOverrideModes.Normalize(input));
    }

    [Theory]
    [InlineData(null, "Automatic")]
    [InlineData("", "Automatic")]
    [InlineData("automatic", "Automatic")]
    [InlineData("force-allow", "Manual allow")]
    [InlineData("force-block", "Manual block")]
    [InlineData("unknown", "Automatic")]
    public void ToDisplayName_ReturnsExpected(string? input, string expected)
    {
        Assert.Equal(expected, PublishingOverrideModes.ToDisplayName(input));
    }
}
