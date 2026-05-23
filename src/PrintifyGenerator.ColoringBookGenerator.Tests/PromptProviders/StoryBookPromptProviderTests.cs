using PrintifyGenerator.ColoringBookGenerator.Services.PromptProviders;

namespace PrintifyGenerator.ColoringBookGenerator.Tests.PromptProviders;

public class StoryBookPromptProviderTests
{
    private readonly StoryBookPromptProvider _sut = new();

    [Fact] public void BookType_ReturnsStoryBook() =>
        Assert.Equal("Story Book", _sut.BookType);

    [Fact] public void BaseStyleTerms_ContainsWarmColors() =>
        Assert.Contains("warm colors", _sut.BaseStyleTerms);

    [Fact] public void BaseStyleTerms_ContainsWatercolor() =>
        Assert.Contains("watercolor", _sut.BaseStyleTerms);

    [Fact] public void BuildFrontCoverPrompt_ContainsTitle()
    {
        var result = _sut.BuildFrontCoverPrompt("The Brave Rabbit", "adventure", "warm style");
        Assert.Contains("The Brave Rabbit", result);
    }

    [Fact] public void BuildFrontCoverPrompt_ContainsTheme()
    {
        var result = _sut.BuildFrontCoverPrompt("T", "fairy forest", "s");
        Assert.Contains("fairy forest", result);
    }

    [Fact] public void BuildFrontCoverPrompt_ContainsStyleAddon()
    {
        var result = _sut.BuildFrontCoverPrompt("T", "t", "whimsical watercolor");
        Assert.Contains("whimsical watercolor", result);
    }

    [Fact] public void BuildFrontCoverPrompt_RequiresFullColor()
    {
        var result = _sut.BuildFrontCoverPrompt("T", "t", "s");
        Assert.Contains("Full-color", result);
        Assert.Contains("warm", result);
    }

    [Fact] public void BuildBackCoverPrompt_ContainsTheme()
    {
        var result = _sut.BuildBackCoverPrompt("space adventure", "soft");
        Assert.Contains("space adventure", result);
    }

    [Fact] public void BuildBackCoverPrompt_RequiresFullColor()
    {
        var result = _sut.BuildBackCoverPrompt("t", "s");
        Assert.Contains("Full-color", result);
        Assert.Contains("warm", result);
    }

    [Fact] public void BuildPagePrompt_ContainsPageNumber()
    {
        var result = _sut.BuildPagePrompt(3, "brave rabbit explores", "style");
        Assert.Contains("PAGE 3", result);
    }

    [Fact] public void BuildPagePrompt_ContainsSubject()
    {
        var result = _sut.BuildPagePrompt(1, "Pip discovers a hidden door", "warm");
        Assert.Contains("Pip discovers a hidden door", result);
    }

    [Fact] public void BuildPagePrompt_MentionsTextSpace()
    {
        var result = _sut.BuildPagePrompt(1, "s", "s");
        Assert.Contains("full-color", result);
        Assert.Contains("storybook", result);
    }

    [Fact] public void BuildPagePrompt_RequiresFullColor()
    {
        var result = _sut.BuildPagePrompt(1, "s", "s");
        Assert.Contains("Full-color", result);
        Assert.Contains("warm", result);
    }

    [Fact] public void BuildPageSubjectPrompt_ContainsTheme()
    {
        var result = _sut.BuildPageSubjectPrompt("enchanted forest", 5);
        Assert.Contains("enchanted forest", result);
    }

    [Fact] public void BuildPageSubjectPrompt_ContainsPageNumber()
    {
        var result = _sut.BuildPageSubjectPrompt("t", 12);
        Assert.Contains("page 12", result);
    }

    [Fact] public void BuildPageSubjectPrompt_AsksForNarrative()
    {
        var result = _sut.BuildPageSubjectPrompt("t", 1);
        Assert.Contains("narrative", result);
    }

    [Fact] public void BuildPageSubjectsFallback_Returns13Items()
    {
        var result = _sut.BuildPageSubjectsFallback("magical");
        Assert.Equal(13, result.Length);
    }

    [Fact] public void BuildPageSubjectsFallback_EachItemContainsTheme()
    {
        var result = _sut.BuildPageSubjectsFallback("ocean");
        Assert.All(result, item => Assert.Contains("ocean", item));
    }

    [Fact] public void BuildPageSubjectsFallback_AllItemsNonEmpty()
    {
        var result = _sut.BuildPageSubjectsFallback("fairy");
        Assert.All(result, item => Assert.False(string.IsNullOrWhiteSpace(item)));
    }

    [Fact] public void BuildFullStoryPrompt_ReturnsPrompt()
    {
        var result = _sut.BuildFullStoryPrompt("brave rabbit");
        Assert.NotNull(result);
    }

    [Fact] public void BuildFullStoryPrompt_ContainsTheme()
    {
        var result = _sut.BuildFullStoryPrompt("enchanted forest");
        Assert.Contains("enchanted forest", result);
    }

    [Fact] public void BuildFullStoryPrompt_Requests13Segments()
    {
        var result = _sut.BuildFullStoryPrompt("t");
        Assert.Contains("13 sequential segments", result);
        Assert.Contains("exactly 13", result);
    }

    [Fact] public void BuildFullStoryPrompt_RequestsJsonArray()
    {
        var result = _sut.BuildFullStoryPrompt("t");
        Assert.Contains("JSON array", result);
    }

    [Fact] public void BuildFullStoryPrompt_DescribesSegments()
    {
        var result = _sut.BuildFullStoryPrompt("t");
        Assert.Contains("Segment 1", result);
        Assert.Contains("Segment 13", result);
    }

    [Fact] public void BuildThemeAndStylePrompt_ContainsTitle()
    {
        var result = _sut.BuildThemeAndStylePrompt("The Brave Rabbit");
        Assert.Contains("The Brave Rabbit", result);
    }

    [Fact] public void BuildThemeAndStylePrompt_ContainsBaseStyleTerms()
    {
        var result = _sut.BuildThemeAndStylePrompt("Title");
        Assert.Contains(_sut.BaseStyleTerms, result);
    }

    [Fact] public void BuildTitleGenerationPrompt_ContainsCount()
    {
        var result = _sut.BuildTitleGenerationPrompt(3);
        Assert.Contains("3 title ideas", result);
        Assert.Contains("exactly 3", result);
    }

    [Fact] public void BuildTitleGenerationPrompt_AsksForStoryStyle()
    {
        var result = _sut.BuildTitleGenerationPrompt(1);
        Assert.Contains("story", result);
    }

    [Fact] public void BuildDescription_ContainsTheme()
    {
        var result = _sut.BuildDescription("fairy tales");
        Assert.Contains("fairy tales", result);
    }

    [Fact] public void BuildDescription_MentionsStoryBook()
    {
        var result = _sut.BuildDescription("t");
        Assert.Contains("story book", result);
    }

    [Fact] public void BuildTags_ContainsTheme()
    {
        var result = _sut.BuildTags("dinosaur adventure");
        Assert.Contains("dinosaur adventure", result);
    }

    [Fact] public void BuildTags_ContainsStoryBook()
    {
        var result = _sut.BuildTags("t");
        Assert.Contains("story book", result);
    }
}
