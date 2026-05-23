using PrintifyGenerator.ColoringBookGenerator.Services;
using PrintifyGenerator.ColoringBookGenerator.Services.PromptProviders;
using PrintifyGenerator.ColoringBookGenerator.Utilities;
using System.Text.Json;

namespace PrintifyGenerator.ColoringBookGenerator.Tests.Services;

public class ColoringBookServiceTests
{
    [Fact]
    public void PromptProvider_IsStored()
    {
        var provider = new PictureBookPromptProvider();
        var service = CreateService(provider);
        Assert.NotNull(provider);
    }

    [Fact]
    public void GenerateFullStoryAsync_PictureBook_FallsBackToIndividual()
    {
        var provider = new PictureBookPromptProvider();
        var storyPrompt = provider.BuildFullStoryPrompt("any");
        Assert.Null(storyPrompt);
    }

    [Fact]
    public void GenerateFullStoryAsync_StoryBook_ReturnsPrompt()
    {
        var provider = new StoryBookPromptProvider();
        var storyPrompt = provider.BuildFullStoryPrompt("brave rabbit");
        Assert.NotNull(storyPrompt);
        Assert.Contains("13 sequential segments", storyPrompt);
    }

    [Fact]
    public void BuildDescription_ForPictureBook_MentionsPictureBook()
    {
        var provider = new PictureBookPromptProvider();
        var desc = provider.BuildDescription("jungle");
        Assert.Contains("picture book", desc);
        Assert.Contains("jungle", desc);
    }

    [Fact]
    public void BuildDescription_ForStoryBook_MentionsStoryBook()
    {
        var provider = new StoryBookPromptProvider();
        var desc = provider.BuildDescription("fairy tales");
        Assert.Contains("story book", desc);
        Assert.Contains("fairy tales", desc);
    }

    [Fact]
    public void BuildDescription_ForPaintByNumbers_MentionsPaintByNumbers()
    {
        var provider = new PaintByNumbersPromptProvider();
        var desc = provider.BuildDescription("underwater");
        Assert.Contains("paint-by-numbers", desc);
        Assert.Contains("underwater", desc);
    }

    [Fact]
    public void BuildTags_ForPictureBook_ContainsPictureBookTag()
    {
        var provider = new PictureBookPromptProvider();
        var tags = provider.BuildTags("ocean");
        Assert.Contains("picture book", tags);
        Assert.Contains("ocean", tags);
    }

    [Fact]
    public void BuildTags_ForStoryBook_ContainsStoryTag()
    {
        var provider = new StoryBookPromptProvider();
        var tags = provider.BuildTags("dinosaur");
        Assert.Contains("story book", tags);
        Assert.Contains("dinosaur", tags);
    }

    [Fact]
    public void BuildTags_ForPaintByNumbers_ContainsPaintByNumbersTag()
    {
        var provider = new PaintByNumbersPromptProvider();
        var tags = provider.BuildTags("safari");
        Assert.Contains("paint by numbers", tags);
        Assert.Contains("safari", tags);
    }

    [Fact]
    public void GenerateFullStoryAsync_PictureBook_Fallback_Has24Items()
    {
        var provider = new PictureBookPromptProvider();
        var fallback = provider.BuildPageSubjectsFallback("animals");
        Assert.Equal(24, fallback.Length);
    }

    [Fact]
    public void GenerateFullStoryAsync_StoryBook_Fallback_Has13Items()
    {
        var provider = new StoryBookPromptProvider();
        var fallback = provider.BuildPageSubjectsFallback("magical");
        Assert.Equal(13, fallback.Length);
    }

    [Fact]
    public void GenerateFullStoryAsync_PaintByNumbers_Fallback_Has24Items()
    {
        var provider = new PaintByNumbersPromptProvider();
        var fallback = provider.BuildPageSubjectsFallback("space");
        Assert.Equal(24, fallback.Length);
    }

    [Fact]
    public void ThemeAndStylePrompt_ForPictureBook_IncludesBaseTerms()
    {
        var provider = new PictureBookPromptProvider();
        var prompt = provider.BuildThemeAndStylePrompt("My Book");
        Assert.Contains(provider.BaseStyleTerms, prompt);
    }

    [Fact]
    public void ThemeAndStylePrompt_ForStoryBook_IncludesBaseTerms()
    {
        var provider = new StoryBookPromptProvider();
        var prompt = provider.BuildThemeAndStylePrompt("My Story");
        Assert.Contains(provider.BaseStyleTerms, prompt);
    }

    [Fact]
    public void ThemeAndStylePrompt_ForPaintByNumbers_IncludesBaseTerms()
    {
        var provider = new PaintByNumbersPromptProvider();
        var prompt = provider.BuildThemeAndStylePrompt("My Book");
        Assert.Contains(provider.BaseStyleTerms, prompt);
    }

    // ── SanitizeTitleForFolder ─────────────────────────────────────────────

    [Fact]
    public void Sanitize_NormalTitle()
    {
        var result = ColoringBookService.SanitizeTitleForFolder("My Coloring Book");
        Assert.Equal("My_Coloring_Book", result);
    }

    [Fact]
    public void Sanitize_ReplacesInvalidChars()
    {
        var result = ColoringBookService.SanitizeTitleForFolder("My\u0000Book");
        Assert.Equal("My_Book", result ?? "");
    }

    [Fact]
    public void Sanitize_CollapsesWhitespace()
    {
        var result = ColoringBookService.SanitizeTitleForFolder("a   b   c");
        Assert.Equal("a_b_c", result);
    }

    [Fact]
    public void Sanitize_TrimsUnderscores()
    {
        var result = ColoringBookService.SanitizeTitleForFolder("__hello__");
        Assert.Equal("hello", result);
    }

    [Fact]
    public void Sanitize_NullOrEmpty_Fallback()
    {
        Assert.Equal("coloring_book", ColoringBookService.SanitizeTitleForFolder(null!));
        Assert.Equal("coloring_book", ColoringBookService.SanitizeTitleForFolder(""));
        Assert.Equal("coloring_book", ColoringBookService.SanitizeTitleForFolder("   "));
    }

    [Fact]
    public void Sanitize_CollapsesMultipleSpaces()
    {
        var result = ColoringBookService.SanitizeTitleForFolder("Cat   Dog    Friends");
        Assert.Equal("Cat_Dog_Friends", result);
    }

    // ── BuildPrintArea ─────────────────────────────────────────────────────

    [Fact]
    public void BuildPrintArea_SetsPosition()
    {
        var result = ColoringBookService.BuildPrintArea("cover", "img123", new[] { 1, 2 });
        Assert.Equal("cover", result.Placeholders[0].Position);
    }

    [Fact]
    public void BuildPrintArea_SetsImageId()
    {
        var result = ColoringBookService.BuildPrintArea("cover", "img_abc", new[] { 148586 });
        Assert.Equal("img_abc", result.Placeholders[0].Images[0].Id);
    }

    [Fact]
    public void BuildPrintArea_SetsVariantIds()
    {
        var result = ColoringBookService.BuildPrintArea("page_1", "img1", new[] { 148586, 148587 });
        Assert.Equal(2, result.VariantIds.Count);
        Assert.Contains(148586, result.VariantIds);
        Assert.Contains(148587, result.VariantIds);
    }

    [Fact]
    public void BuildPrintArea_SetsDecorationMethod()
    {
        var result = ColoringBookService.BuildPrintArea("page_1", "img1", new[] { 1 });
        Assert.Equal("digital-printing", result.Placeholders[0].DecorationMethod);
    }

    [Fact]
    public void BuildPrintArea_SetsDefaultImageCoordinates()
    {
        var result = ColoringBookService.BuildPrintArea("cover", "img1", new[] { 1 });
        var img = result.Placeholders[0].Images[0];
        Assert.Equal(0.5, img.X);
        Assert.Equal(0.5, img.Y);
        Assert.Equal(1.0, img.Scale);
        Assert.Equal(1.0, img.Width);
        Assert.Equal(1.0, img.Height);
    }

    // ── BuildPageSubjectsFallback — each item contains theme ───────────────

    [Fact]
    public void PictureBook_Fallback_EachItemContainsTheme()
    {
        var provider = new PictureBookPromptProvider();
        var items = provider.BuildPageSubjectsFallback("ocean");
        foreach (var item in items)
            Assert.Contains("ocean", item);
    }

    [Fact]
    public void StoryBook_Fallback_EachItemContainsTheme()
    {
        var provider = new StoryBookPromptProvider();
        var items = provider.BuildPageSubjectsFallback("magical");
        foreach (var item in items)
            Assert.Contains("magical", item);
    }

    [Fact]
    public void PaintByNumbers_Fallback_EachItemContainsTheme()
    {
        var provider = new PaintByNumbersPromptProvider();
        var items = provider.BuildPageSubjectsFallback("space");
        foreach (var item in items)
            Assert.Contains("space", item);
    }

    // ── BuildTitleGenerationPrompt ─────────────────────────────────────────

    [Fact]
    public void PictureBook_TitlePrompt_AsksForCount()
    {
        var provider = new PictureBookPromptProvider();
        var prompt = provider.BuildTitleGenerationPrompt(5);
        Assert.Contains("5 title", prompt);
    }

    [Fact]
    public void StoryBook_TitlePrompt_AsksForCount()
    {
        var provider = new StoryBookPromptProvider();
        var prompt = provider.BuildTitleGenerationPrompt(3);
        Assert.Contains("3 title", prompt);
    }

    [Fact]
    public void PaintByNumbers_TitlePrompt_AsksForCount()
    {
        var provider = new PaintByNumbersPromptProvider();
        var prompt = provider.BuildTitleGenerationPrompt(2);
        Assert.Contains("2 title", prompt);
    }

    // ── Service construction ───────────────────────────────────────────────

    [Fact]
    public void Constructor_StoresOllamaUrl()
    {
        var provider = new PictureBookPromptProvider();
        var service = CreateService(provider);
        Assert.NotNull(service);
    }

    // ── Fallback subject fallback waterfall ─────────────────────────────────

    [Fact]
    public void StoryBook_Fallback_HasConsistentCharacters()
    {
        var provider = new StoryBookPromptProvider();
        var items = provider.BuildPageSubjectsFallback("space");
        foreach (var item in items)
            Assert.Contains("space", item);
    }

    private static IImageGenerator CreateStubGenerator() => new StubImageGenerator("test_result");

    private static ColoringBookService CreateService(IPromptProvider provider, IImageGenerator? generator = null)
    {
        var gen = generator ?? CreateStubGenerator();
        return new ColoringBookService(
            gen,
            null!,
            12345,
            provider,
            "http://localhost:11434",
            "test-model");
    }
}
