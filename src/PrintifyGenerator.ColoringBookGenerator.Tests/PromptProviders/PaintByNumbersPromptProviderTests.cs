using PrintifyGenerator.ColoringBookGenerator.Services.PromptProviders;

namespace PrintifyGenerator.ColoringBookGenerator.Tests.PromptProviders;

public class PaintByNumbersPromptProviderTests
{
    private readonly PaintByNumbersPromptProvider _sut = new();

    [Fact] public void BookType_ReturnsPaintByNumbers() =>
        Assert.Equal("Paint by Numbers", _sut.BookType);

    [Fact] public void BaseStyleTerms_ContainsThickBlackOutlines() =>
        Assert.Contains("thick black outlines", _sut.BaseStyleTerms);

    [Fact] public void BaseStyleTerms_ContainsNumberedSections() =>
        Assert.Contains("numbered sections", _sut.BaseStyleTerms);

    [Fact] public void BuildFrontCoverPrompt_ContainsTitle()
    {
        var result = _sut.BuildFrontCoverPrompt("Safari Animals", "animals", "geometric");
        Assert.Contains("Safari Animals", result);
    }

    [Fact] public void BuildFrontCoverPrompt_ContainsTheme()
    {
        var result = _sut.BuildFrontCoverPrompt("T", "underwater world", "s");
        Assert.Contains("underwater world", result);
    }

    [Fact] public void BuildFrontCoverPrompt_ContainsStyleAddon()
    {
        var result = _sut.BuildFrontCoverPrompt("T", "t", "geometric regions");
        Assert.Contains("geometric regions", result);
    }

    [Fact] public void BuildFrontCoverPrompt_MentionsNumberedSections()
    {
        var result = _sut.BuildFrontCoverPrompt("T", "t", "s");
        Assert.Contains("numbered sections", result);
        Assert.Contains("1-10", result);
    }

    [Fact] public void BuildBackCoverPrompt_ContainsTheme()
    {
        var result = _sut.BuildBackCoverPrompt("space scene", "clear");
        Assert.Contains("space scene", result);
    }

    [Fact] public void BuildBackCoverPrompt_MentionsNumberedRegions()
    {
        var result = _sut.BuildBackCoverPrompt("t", "s");
        Assert.Contains("numbered sections", result);
    }

    [Fact] public void BuildPagePrompt_ContainsPageNumber()
    {
        var result = _sut.BuildPagePrompt(8, "sailboat scene", "style");
        Assert.Contains("PAGE 8", result);
    }

    [Fact] public void BuildPagePrompt_ContainsSubject()
    {
        var result = _sut.BuildPagePrompt(1, "sailboat on calm water", "polygonal");
        Assert.Contains("sailboat on calm water", result);
    }

    [Fact] public void BuildPagePrompt_ContainsStyleAddon()
    {
        var result = _sut.BuildPagePrompt(3, "s", "clear polygonal regions");
        Assert.Contains("clear polygonal regions", result);
    }

    [Fact] public void BuildPagePrompt_MentionsNumberedSections()
    {
        var result = _sut.BuildPagePrompt(1, "s", "s");
        Assert.Contains("numbered sections", result);
    }

    [Fact] public void BuildPagePrompt_MentionsNumbersInsideRegions()
    {
        var result = _sut.BuildPagePrompt(1, "s", "s");
        Assert.Contains("number (1-10)", result);
        Assert.Contains("inside each", result);
    }

    [Fact] public void BuildPagePrompt_MentionsThickOutlines()
    {
        var result = _sut.BuildPagePrompt(1, "s", "s");
        Assert.Contains("Thick black outlines", result);
    }

    [Fact] public void BuildPageSubjectPrompt_ContainsTheme()
    {
        var result = _sut.BuildPageSubjectPrompt("safari animals", 3);
        Assert.Contains("safari animals", result);
    }

    [Fact] public void BuildPageSubjectPrompt_ContainsPageNumber()
    {
        var result = _sut.BuildPageSubjectPrompt("t", 10);
        Assert.Contains("page 10", result);
    }

    [Fact] public void BuildPageSubjectPrompt_AsksForNumberedSections()
    {
        var result = _sut.BuildPageSubjectPrompt("t", 1);
        Assert.Contains("numbered painting sections", result);
    }

    [Fact] public void BuildPageSubjectsFallback_Returns24Items()
    {
        var result = _sut.BuildPageSubjectsFallback("ocean");
        Assert.Equal(24, result.Length);
    }

    [Fact] public void BuildPageSubjectsFallback_EachItemContainsTheme()
    {
        var result = _sut.BuildPageSubjectsFallback("dinosaur");
        Assert.All(result, item => Assert.Contains("dinosaur", item));
    }

    [Fact] public void BuildPageSubjectsFallback_AllItemsNonEmpty()
    {
        var result = _sut.BuildPageSubjectsFallback("space");
        Assert.All(result, item => Assert.False(string.IsNullOrWhiteSpace(item)));
    }

    [Fact] public void BuildThemeAndStylePrompt_ContainsTitle()
    {
        var result = _sut.BuildThemeAndStylePrompt("Safari Paint by Numbers");
        Assert.Contains("Safari Paint by Numbers", result);
    }

    [Fact] public void BuildThemeAndStylePrompt_ContainsBaseStyleTerms()
    {
        var result = _sut.BuildThemeAndStylePrompt("Title");
        Assert.Contains(_sut.BaseStyleTerms, result);
    }

    [Fact] public void BuildTitleGenerationPrompt_ContainsCount()
    {
        var result = _sut.BuildTitleGenerationPrompt(4);
        Assert.Contains("4 title ideas", result);
        Assert.Contains("exactly 4", result);
    }

    [Fact] public void BuildTitleGenerationPrompt_RequiresPaintByNumbers()
    {
        var result = _sut.BuildTitleGenerationPrompt(2);
        Assert.Contains("Paint by Numbers", result);
    }

    [Fact] public void BuildFullStoryPrompt_ReturnsNull()
    {
        Assert.Null(_sut.BuildFullStoryPrompt("any theme"));
    }

    [Fact] public void BuildDescription_ContainsTheme()
    {
        var result = _sut.BuildDescription("underwater");
        Assert.Contains("underwater", result);
    }

    [Fact] public void BuildDescription_MentionsPaintByNumbers()
    {
        var result = _sut.BuildDescription("t");
        Assert.Contains("paint-by-numbers", result);
    }

    [Fact] public void BuildTags_ContainsTheme()
    {
        var result = _sut.BuildTags("safari");
        Assert.Contains("safari", result);
    }

    [Fact] public void BuildTags_ContainsPaintByNumbers()
    {
        var result = _sut.BuildTags("t");
        Assert.Contains("paint by numbers", result);
    }
}
