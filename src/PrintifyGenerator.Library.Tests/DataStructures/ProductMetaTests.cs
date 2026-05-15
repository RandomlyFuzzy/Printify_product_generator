namespace PrintifyGenerator.Library.Tests.DataStructures;

public class ProductMetaTests
{
    [Fact]
    public void PromptExample_ReturnsNonEmptyTitle()
    {
        var example = ProductMeta.PromptExample();
        Assert.False(string.IsNullOrWhiteSpace(example.Title));
    }

    [Fact]
    public void PromptExample_ReturnsNonEmptyDescription()
    {
        var example = ProductMeta.PromptExample();
        Assert.False(string.IsNullOrWhiteSpace(example.Description));
    }

    [Fact]
    public void PromptExample_ReturnsTags()
    {
        var example = ProductMeta.PromptExample();
        Assert.NotEmpty(example.Tags);
    }

    [Fact]
    public void PromptExample_TagSpellingIsCorrect()
    {
        var example = ProductMeta.PromptExample();
        Assert.Contains(example.Tags, t => t.Equals("Phoenix", StringComparison.Ordinal));
    }
}
