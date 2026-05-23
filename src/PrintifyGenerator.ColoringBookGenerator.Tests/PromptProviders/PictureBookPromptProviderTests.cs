using PrintifyGenerator.ColoringBookGenerator.Services.PromptProviders;

namespace PrintifyGenerator.ColoringBookGenerator.Tests.PromptProviders;

public class PictureBookPromptProviderTests
{
    private readonly PictureBookPromptProvider _sut = new();

    [Fact] public void BookType_ReturnsPictureBook() =>
        Assert.Equal("Picture Book", _sut.BookType);

    [Fact] public void BaseStyleTerms_ContainsThickBlackOutlines() =>
        Assert.Contains("thick black outlines", _sut.BaseStyleTerms);

    [Fact] public void BuildFrontCoverPrompt_ContainsTitle()
    {
        var result = _sut.BuildFrontCoverPrompt("My Book", "animals", "cute style");
        Assert.Contains("My Book", result);
    }

    [Fact] public void BuildFrontCoverPrompt_ContainsTheme()
    {
        var result = _sut.BuildFrontCoverPrompt("Title", "jungle safari", "detailed");
        Assert.Contains("jungle safari", result);
    }

    [Fact] public void BuildFrontCoverPrompt_ContainsStyleAddon()
    {
        var result = _sut.BuildFrontCoverPrompt("Title", "theme", "cute cartoon style");
        Assert.Contains("cute cartoon style", result);
    }

    [Fact] public void BuildFrontCoverPrompt_EnforcesBlackAndWhite()
    {
        var result = _sut.BuildFrontCoverPrompt("T", "t", "s");
        Assert.Contains("black and white", result);
        Assert.Contains("No grayscale", result);
    }

    [Fact] public void BuildBackCoverPrompt_ContainsTheme()
    {
        var result = _sut.BuildBackCoverPrompt("ocean theme", "simple");
        Assert.Contains("ocean theme", result);
    }

    [Fact] public void BuildBackCoverPrompt_ContainsStyleAddon()
    {
        var result = _sut.BuildBackCoverPrompt("t", "whimsical style");
        Assert.Contains("whimsical style", result);
    }

    [Fact] public void BuildPagePrompt_ContainsPageNumber()
    {
        var result = _sut.BuildPagePrompt(5, "rabbit garden", "detailed");
        Assert.Contains("PAGE 5", result);
    }

    [Fact] public void BuildPagePrompt_ContainsSubject()
    {
        var result = _sut.BuildPagePrompt(1, "two rabbits having tea", "style");
        Assert.Contains("two rabbits having tea", result);
    }

    [Fact] public void BuildPagePrompt_ContainsStyleAddon()
    {
        var result = _sut.BuildPagePrompt(3, "s", "playful whimsical");
        Assert.Contains("playful whimsical", result);
    }

    [Fact] public void BuildPagePrompt_EnforcesBlackAndWhite()
    {
        var result = _sut.BuildPagePrompt(1, "s", "s");
        Assert.Contains("black and white", result);
        Assert.Contains("No grayscale", result);
    }

    [Fact] public void BuildPageSubjectPrompt_ContainsTheme()
    {
        var result = _sut.BuildPageSubjectPrompt("jungle animals", 1);
        Assert.Contains("jungle animals", result);
    }

    [Fact] public void BuildPageSubjectPrompt_ContainsPageNumber()
    {
        var result = _sut.BuildPageSubjectPrompt("t", 7);
        Assert.Contains("page 7", result);
    }

    [Fact] public void BuildPageSubjectsFallback_Returns24Items()
    {
        var result = _sut.BuildPageSubjectsFallback("animals");
        Assert.Equal(24, result.Length);
    }

    [Fact] public void BuildPageSubjectsFallback_EachItemContainsTheme()
    {
        var result = _sut.BuildPageSubjectsFallback("space");
        Assert.All(result, item => Assert.Contains("space", item));
    }

    [Fact] public void BuildPageSubjectsFallback_AllItemsNonEmpty()
    {
        var result = _sut.BuildPageSubjectsFallback("ocean");
        Assert.All(result, item => Assert.False(string.IsNullOrWhiteSpace(item)));
    }

    [Fact] public void BuildThemeAndStylePrompt_ContainsTitle()
    {
        var result = _sut.BuildThemeAndStylePrompt("My Amazing Book");
        Assert.Contains("My Amazing Book", result);
    }

    [Fact] public void BuildThemeAndStylePrompt_ContainsBaseStyleTerms()
    {
        var result = _sut.BuildThemeAndStylePrompt("Title");
        Assert.Contains(_sut.BaseStyleTerms, result);
    }

    [Fact] public void BuildThemeAndStylePrompt_RequestsJsonFormat()
    {
        var result = _sut.BuildThemeAndStylePrompt("Title");
        Assert.Contains("\"theme\"", result);
        Assert.Contains("\"style\"", result);
    }

    [Fact] public void BuildTitleGenerationPrompt_ContainsCount()
    {
        var result = _sut.BuildTitleGenerationPrompt(5);
        Assert.Contains("5 title ideas", result);
        Assert.Contains("exactly 5", result);
    }

    [Fact] public void BuildTitleGenerationPrompt_RequestsColoringBookSuffix()
    {
        var result = _sut.BuildTitleGenerationPrompt(3);
        Assert.Contains("Coloring Book", result);
        Assert.Contains("Colouring Book", result);
    }

    [Fact] public void BuildFullStoryPrompt_ReturnsNull()
    {
        Assert.Null(_sut.BuildFullStoryPrompt("any theme"));
    }

    [Fact] public void BuildDescription_ContainsTheme()
    {
        var result = _sut.BuildDescription("fairy garden");
        Assert.Contains("fairy garden", result);
    }

    [Fact] public void BuildDescription_MentionsPictureBook()
    {
        var result = _sut.BuildDescription("t");
        Assert.Contains("picture book", result);
    }

    [Fact] public void BuildTags_ContainsTheme()
    {
        var result = _sut.BuildTags("jungle safari");
        Assert.Contains("jungle safari", result);
    }

    [Fact] public void BuildTags_ContainsPrintify()
    {
        var result = _sut.BuildTags("t");
        Assert.Contains("printify", result);
    }

    [Fact] public void BuildTags_ContainsPictureBook()
    {
        var result = _sut.BuildTags("t");
        Assert.Contains("picture book", result);
    }
}
