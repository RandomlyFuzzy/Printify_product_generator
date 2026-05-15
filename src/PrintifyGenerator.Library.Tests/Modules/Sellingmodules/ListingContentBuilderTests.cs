namespace PrintifyGenerator.Library.Tests.Modules.Sellingmodules;

public class ListingContentBuilderTests
{
    private static ListingContentContext CreateContext(string? jobId = null, string? imagePath = null)
    {
        return new ListingContentContext
        {
            JobId = jobId ?? "test-job-123",
            ImagePath = imagePath ?? "/data/checking/2026-04/17/test-image.png",
            BlueprintId = 123,
            BlueprintTitle = "Premium T-Shirt",
            PrintProviderId = 456,
            PrintProviderTitle = "Awesome Print Co",
            LlmReason = "Artwork fits well on a t-shirt"
        };
    }

    [Fact]
    public void Build_ReturnsBundleWithAllChannels()
    {
        var context = CreateContext();
        var bundle = ListingContentBuilder.Build(context);

        Assert.NotNull(bundle);
        Assert.NotNull(bundle.Lookup);
        Assert.NotNull(bundle.Channels);
        Assert.Contains("printify", bundle.Channels);
        Assert.Contains("etsy", bundle.Channels);
        Assert.Contains("ebay", bundle.Channels);
        Assert.Contains("shopify", bundle.Channels);
        Assert.Contains("woocommerce", bundle.Channels);
        Assert.Contains("generic", bundle.Channels);
    }

    [Fact]
    public void Build_LookupContainsReferenceCode()
    {
        var context = CreateContext();
        var bundle = ListingContentBuilder.Build(context);

        Assert.Contains("TEST-JOB", bundle.Lookup.ReferenceCode);
        Assert.Contains("B123", bundle.Lookup.ReferenceCode);
        Assert.Contains("P456", bundle.Lookup.ReferenceCode);
    }

    [Fact]
    public void Build_PrintifyTitleIsTruncatedTo180Chars()
    {
        var context = CreateContext();
        var bundle = ListingContentBuilder.Build(context);

        Assert.True(bundle.Channels["printify"].Title.Length <= 180);
    }

    [Fact]
    public void Build_EtsyTitleIsTruncatedTo140Chars()
    {
        var context = CreateContext();
        var bundle = ListingContentBuilder.Build(context);

        Assert.True(bundle.Channels["etsy"].Title.Length <= 140);
    }

    [Fact]
    public void Build_EbayTitleIsTruncatedTo80Chars()
    {
        var context = CreateContext();
        var bundle = ListingContentBuilder.Build(context);

        Assert.True(bundle.Channels["ebay"].Title.Length <= 80);
    }

    [Fact]
    public void Build_ChannelsContainBlueprintTitle()
    {
        var context = CreateContext();
        var bundle = ListingContentBuilder.Build(context);

        foreach (var channel in bundle.Channels.Values)
        {
            Assert.Contains("Premium T-Shirt", channel.Title);
        }
    }

    [Fact]
    public void Build_WithEmptyJobId_UsesImagePath()
    {
        var context = CreateContext(jobId: "", imagePath: "/path/to/my-custom-image.png");
        var bundle = ListingContentBuilder.Build(context);

        Assert.DoesNotContain("unknown-job", bundle.Lookup.JobId);
    }

    [Fact]
    public void Build_WithMinimalContext_DoesNotThrow()
    {
        var context = new ListingContentContext
        {
            JobId = "",
            ImagePath = "",
            BlueprintId = 0,
            BlueprintTitle = "",
            PrintProviderId = 0,
            PrintProviderTitle = "",
            LlmReason = ""
        };

        var bundle = ListingContentBuilder.Build(context);
        Assert.NotNull(bundle);
    }

    [Fact]
    public void Build_TagsContainBlueprintAndProviderRefs()
    {
        var context = CreateContext();
        var bundle = ListingContentBuilder.Build(context);

        var tags = bundle.Channels["printify"].Tags;
        Assert.Contains(tags, t => t.Contains("pgb-123"));
        Assert.Contains(tags, t => t.Contains("pgp-456"));
    }

    [Fact]
    public void ResolveChannel_WithNull_ReturnsGeneric()
    {
        var context = CreateContext();
        var bundle = ListingContentBuilder.Build(context);

        var channel = ListingContentBuilder.ResolveChannel(bundle, null);
        Assert.Equal(bundle.Channels["generic"].Title, channel.Title);
    }

    [Fact]
    public void ResolveChannel_WithUnknownPlatform_ReturnsGeneric()
    {
        var context = CreateContext();
        var bundle = ListingContentBuilder.Build(context);

        var channel = ListingContentBuilder.ResolveChannel(bundle, "unknown-platform");
        Assert.Equal(bundle.Channels["generic"].Title, channel.Title);
    }

    [Fact]
    public void ResolveChannel_WithExactMatch_ReturnsCorrectChannel()
    {
        var context = CreateContext();
        var bundle = ListingContentBuilder.Build(context);

        var channel = ListingContentBuilder.ResolveChannel(bundle, "etsy");
        Assert.Equal(bundle.Channels["etsy"].Title, channel.Title);
    }

    [Fact]
    public void Build_DescriptionContainsProductionPartner()
    {
        var context = CreateContext();
        var bundle = ListingContentBuilder.Build(context);

        foreach (var channel in bundle.Channels.Values)
        {
            Assert.Contains("Awesome Print Co", channel.Description);
        }
    }
}
