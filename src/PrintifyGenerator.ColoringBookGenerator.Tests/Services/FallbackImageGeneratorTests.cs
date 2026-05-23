using PrintifyGenerator.ColoringBookGenerator.Models;
using PrintifyGenerator.ColoringBookGenerator.Services;
using PrintifyGenerator.ColoringBookGenerator.Services.PromptProviders;

namespace PrintifyGenerator.ColoringBookGenerator.Tests.Services;

public class FallbackImageGeneratorTests
{
    [Fact]
    public async Task GenerateFrontCoverAsync_UsesPrimary_WhenSuccessful()
    {
        var primary = new StubImageGenerator("primary_front");
        var fallback = new StubImageGenerator("fallback_front");
        var sut = new FallbackImageGenerator(primary, fallback);

        var result = await sut.GenerateFrontCoverAsync("/out", "title", "theme", "style");

        Assert.Equal("primary_front", result);
    }

    [Fact]
    public async Task GenerateFrontCoverAsync_FallsBack_WhenPrimaryThrows()
    {
        var primary = new StubImageGenerator(throwOnCall: true);
        var fallback = new StubImageGenerator("fallback_result");
        var sut = new FallbackImageGenerator(primary, fallback);

        var result = await sut.GenerateFrontCoverAsync("/out", "title", "theme", "style");

        Assert.Equal("fallback_result", result);
    }

    [Fact]
    public async Task GenerateFrontCoverAsync_FallsBack_WhenPrimaryReturnsEmpty()
    {
        var primary = new StubImageGenerator("");
        var fallback = new StubImageGenerator("fallback_result");
        var sut = new FallbackImageGenerator(primary, fallback);

        var result = await sut.GenerateFrontCoverAsync("/out", "title", "theme", "style");

        Assert.Equal("fallback_result", result);
    }

    [Fact]
    public async Task GenerateBackCoverAsync_UsesPrimary_WhenSuccessful()
    {
        var primary = new StubImageGenerator("primary_back");
        var fallback = new StubImageGenerator("fallback_back");
        var sut = new FallbackImageGenerator(primary, fallback);

        var result = await sut.GenerateBackCoverAsync("/out", "theme", "style");

        Assert.Equal("primary_back", result);
    }

    [Fact]
    public async Task GenerateBackCoverAsync_FallsBack_WhenPrimaryThrows()
    {
        var primary = new StubImageGenerator(throwOnCall: true);
        var fallback = new StubImageGenerator("fallback_back");
        var sut = new FallbackImageGenerator(primary, fallback);

        var result = await sut.GenerateBackCoverAsync("/out", "theme", "style");

        Assert.Equal("fallback_back", result);
    }

    [Fact]
    public async Task GeneratePageAsync_UsesPrimary_WhenSuccessful()
    {
        var primary = new StubImageGenerator("primary_page");
        var fallback = new StubImageGenerator("fallback_page");
        var sut = new FallbackImageGenerator(primary, fallback);

        var result = await sut.GeneratePageAsync("/out", 5, "subject", "style");

        Assert.Equal("primary_page", result);
    }

    [Fact]
    public async Task GeneratePageAsync_FallsBack_WhenPrimaryThrows()
    {
        var primary = new StubImageGenerator(throwOnCall: true);
        var fallback = new StubImageGenerator("fallback_page");
        var sut = new FallbackImageGenerator(primary, fallback);

        var result = await sut.GeneratePageAsync("/out", 5, "subject", "style");

        Assert.Equal("fallback_page", result);
    }

    [Fact]
    public async Task GeneratePageAsync_FallsBack_WhenPrimaryReturnsEmpty()
    {
        var primary = new StubImageGenerator("");
        var fallback = new StubImageGenerator("fallback_result");
        var sut = new FallbackImageGenerator(primary, fallback);

        var result = await sut.GeneratePageAsync("/out", 1, "subject", "style");

        Assert.Equal("fallback_result", result);
    }

    [Fact]
    public void Constructor_ThrowsOnNullPrimary()
    {
        Assert.Throws<ArgumentNullException>(() => new FallbackImageGenerator(null!, new StubImageGenerator("f")));
    }

    [Fact]
    public void Constructor_ThrowsOnNullFallback()
    {
        Assert.Throws<ArgumentNullException>(() => new FallbackImageGenerator(new StubImageGenerator("p"), null!));
    }

    [Fact]
    public async Task GenerateFrontCoverAsync_PassesCorrectArgsToPrimary()
    {
        var primary = new ArgRecordingStub();
        var fallback = new StubImageGenerator("f");
        var sut = new FallbackImageGenerator(primary, fallback);

        await sut.GenerateFrontCoverAsync("/output", "MyTitle", "MyTheme", "MyStyle");

        Assert.Equal("/output", primary.FrontCoverArgs?.outputDirectory);
        Assert.Equal("MyTitle", primary.FrontCoverArgs?.title);
        Assert.Equal("MyTheme", primary.FrontCoverArgs?.theme);
        Assert.Equal("MyStyle", primary.FrontCoverArgs?.styleAddon);
    }

    [Fact]
    public async Task GeneratePageAsync_PassesCorrectArgsToPrimary()
    {
        var primary = new ArgRecordingStub();
        var fallback = new StubImageGenerator("f");
        var sut = new FallbackImageGenerator(primary, fallback);

        await sut.GeneratePageAsync("/out", 7, "subject", "addon");

        Assert.Equal("/out", primary.PageArgs?.outputDirectory);
        Assert.Equal(7, primary.PageArgs?.pageNumber);
        Assert.Equal("subject", primary.PageArgs?.theme);
        Assert.Equal("addon", primary.PageArgs?.styleAddon);
    }
}

public class StubImageGenerator : IImageGenerator
{
    private readonly string? _returnValue;
    private readonly bool _throwOnCall;

    public StubImageGenerator(string? returnValue = null, bool throwOnCall = false)
    {
        _returnValue = returnValue;
        _throwOnCall = throwOnCall;
    }

    public Task<string> GenerateFrontCoverAsync(string outputDirectory, string title, string theme, string styleAddon)
    {
        if (_throwOnCall) throw new InvalidOperationException("Primary failed");
        return Task.FromResult(_returnValue ?? "stub_front");
    }
    public Task<string> GenerateFrontCoverAsync(string outputDirectory, string title, string theme, string styleAddon, string? promptPrefix = null)
    {
        if (_throwOnCall) throw new InvalidOperationException("Primary failed");
        return Task.FromResult(_returnValue ?? "stub_front");
    }

    public Task<string> GenerateBackCoverAsync(string outputDirectory, string theme, string styleAddon, string? promptPrefix = null)
    {
        if (_throwOnCall) throw new InvalidOperationException("Primary failed");
        return Task.FromResult(_returnValue ?? "stub_back");
    }

    public Task<string> GeneratePageAsync(string outputDirectory, int pageNumber, string theme, string styleAddon, string? promptPrefix = null)
    {
        if (_throwOnCall) throw new InvalidOperationException("Primary failed");
        return Task.FromResult(_returnValue ?? "stub_page");
    }

    public Task<string> GenerateImageFromJobAsync(string outputDirectory, GenerationJob job, string? promptPrefix = null)
    {
        if (_throwOnCall) throw new InvalidOperationException("Primary failed");
        job.OutputPath = Path.Combine(outputDirectory, job.OutputFileName);
        return Task.FromResult(job.OutputPath);
    }
}

public class ArgRecordingStub : IImageGenerator
{
    public (string outputDirectory, string title, string theme, string styleAddon)? FrontCoverArgs;
    public (string outputDirectory, string theme, string styleAddon)? BackCoverArgs;
    public (string outputDirectory, int pageNumber, string theme, string styleAddon)? PageArgs;

    public Task<string> GenerateFrontCoverAsync(string outputDirectory, string title, string theme, string styleAddon)
    {
        FrontCoverArgs = (outputDirectory, title, theme, styleAddon);
        return Task.FromResult("stub");
    }
    public Task<string> GenerateFrontCoverAsync(string outputDirectory, string title, string theme, string styleAddon, string? promptPrefix = null)
    {
        FrontCoverArgs = (outputDirectory, title, theme, styleAddon);
        return Task.FromResult("stub");
    }

    public Task<string> GenerateBackCoverAsync(string outputDirectory, string theme, string styleAddon, string? promptPrefix = null)
    {
        BackCoverArgs = (outputDirectory, theme, styleAddon);
        return Task.FromResult("stub");
    }

    public Task<string> GeneratePageAsync(string outputDirectory, int pageNumber, string theme, string styleAddon, string? promptPrefix = null)
    {
        PageArgs = (outputDirectory, pageNumber, theme, styleAddon);
        return Task.FromResult("stub");
    }

    public Task<string> GenerateImageFromJobAsync(string outputDirectory, GenerationJob job, string? promptPrefix = null)
    {
        job.OutputPath = Path.Combine(outputDirectory, job.OutputFileName);
        return Task.FromResult(job.OutputPath ?? "stub");
    }
}
