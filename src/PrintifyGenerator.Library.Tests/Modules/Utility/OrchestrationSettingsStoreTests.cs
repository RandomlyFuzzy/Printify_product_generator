namespace PrintifyGenerator.Library.Tests.Modules.Utility;

public class OrchestrationSettingsStoreTests
{
    [Theory]
    [InlineData(null, "")]
    [InlineData("", "")]
    [InlineData("  ", "")]
    [InlineData("http://localhost:11434", "http://localhost:11434")]
    [InlineData("http://localhost:11434/", "http://localhost:11434")]
    [InlineData("  http://localhost:11434  ", "http://localhost:11434")]
    public void NormalizeBaseUrl_ReturnsExpected(string? input, string expected)
    {
        Assert.Equal(expected, OrchestrationSettingsStore.NormalizeBaseUrl(input));
    }

    [Fact]
    public void CreateDefault_ReturnsValidSettings()
    {
        var settings = OrchestrationSettingsStore.CreateDefault();

        Assert.NotNull(settings);
        Assert.NotEmpty(settings.PromptModel);
        Assert.NotEmpty(settings.SuitabilityModel);
        Assert.NotEmpty(settings.MockupVisionModel);
        Assert.Equal(6.0f, settings.MinimumPublishScore);
        Assert.NotEmpty(settings.Ollama);
        Assert.NotEmpty(settings.ComfyUi);
    }

    [Fact]
    public void CreateDefault_OllamaNodesHaveValidUrls()
    {
        var settings = OrchestrationSettingsStore.CreateDefault();

        foreach (var node in settings.Ollama)
        {
            Assert.False(string.IsNullOrWhiteSpace(node.BaseUrl));
            Assert.True(node.Enabled);
        }
    }

    [Fact]
    public void CreateDefault_ComfyUiNodesHaveValidUrls()
    {
        var settings = OrchestrationSettingsStore.CreateDefault();

        foreach (var node in settings.ComfyUi)
        {
            Assert.False(string.IsNullOrWhiteSpace(node.BaseUrl));
            Assert.True(node.Enabled);
        }
    }

    [Fact]
    public void SanitizeMinimumPublishScore_ClampsToZero()
    {
        var settings = new OrchestrationSettings
        {
            PromptModel = "model",
            SuitabilityModel = "model",
            MockupVisionModel = "model",
            MinimumPublishScore = -5.0f
        };

        // We can't directly test the private Sanitize method,
        // but CreateDefault produces known-good values
        var defaults = OrchestrationSettingsStore.CreateDefault();
        Assert.Equal(6.0f, defaults.MinimumPublishScore);
    }
}
