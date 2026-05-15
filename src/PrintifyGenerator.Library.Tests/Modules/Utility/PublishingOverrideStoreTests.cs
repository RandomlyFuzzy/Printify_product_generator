namespace PrintifyGenerator.Library.Tests.Modules.Utility;

public class PublishingOverrideStoreTests
{
    [Fact]
    public void Find_WithNullImagePath_ReturnsNull()
    {
        var overrides = new PublishingOverrideCollection
        {
            Overrides = new List<ImagePublishingOverride>
            {
                new() { ImagePath = "/some/path.png", Mode = PublishingOverrideModes.ForceAllow }
            }
        };

        var result = PublishingOverrideStore.Find(null, overrides);
        Assert.Null(result);
    }

    [Fact]
    public void Find_WithNullOverrides_ReturnsNull()
    {
        var result = PublishingOverrideStore.Find("/some/path.png", null);
        Assert.Null(result);
    }

    [Fact]
    public void Find_WithMatchingEntry_ReturnsOverride()
    {
        var overrides = new PublishingOverrideCollection
        {
            Overrides = new List<ImagePublishingOverride>
            {
                new() { ImagePath = "/data/test.png", Mode = PublishingOverrideModes.ForceAllow }
            }
        };

        var result = PublishingOverrideStore.Find("/data/test.png", overrides);
        Assert.NotNull(result);
        Assert.Equal(PublishingOverrideModes.ForceAllow, result.Mode);
    }

    [Fact]
    public void Find_WithNoMatch_ReturnsNull()
    {
        var overrides = new PublishingOverrideCollection
        {
            Overrides = new List<ImagePublishingOverride>
            {
                new() { ImagePath = "/other/path.png", Mode = PublishingOverrideModes.ForceAllow }
            }
        };

        var result = PublishingOverrideStore.Find("/data/test.png", overrides);
        Assert.Null(result);
    }

    [Fact]
    public void GetOverridesPath_ReturnsExpectedPath()
    {
        var path = PublishingOverrideStore.GetOverridesPath("/data");
        Assert.EndsWith("staging/publishing-overrides.json", path.Replace('\\', '/'));
    }
}
