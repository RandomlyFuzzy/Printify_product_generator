using PrintifyGenerator.ColoringBookGenerator.Utilities;

namespace PrintifyGenerator.ColoringBookGenerator.Tests.Utilities;

public class PromptRecorderTests
{
    public PromptRecorderTests()
    {
        // Reset the recorder before each test by saving to a temp dir
        // (PromptRecorder is static, so tests share state)
    }

    [Fact]
    public async Task Record_StoresEntry()
    {
        PromptRecorder.Record("TestSource", "test_key", "test prompt content");

        var dir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(dir);
        await PromptRecorder.SaveToDirectoryAsync(dir);

        var filePath = Path.Combine(dir, "prompts.json");
        Assert.True(File.Exists(filePath));

        var json = File.ReadAllText(filePath);
        Assert.Contains("TestSource", json);
        Assert.Contains("test_key", json);
        Assert.Contains("test prompt content", json);

        Directory.Delete(dir, true);
    }

    [Fact]
    public async Task Record_HandlesNullPrompt()
    {
        PromptRecorder.Record("Source", "key", null!);

        var dir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(dir);
        await PromptRecorder.SaveToDirectoryAsync(dir);

        var filePath = Path.Combine(dir, "prompts.json");
        Assert.True(File.Exists(filePath));

        Directory.Delete(dir, true);
    }

    [Fact]
    public void Record_HandlesEmptySource()
    {
        PromptRecorder.Record("", "key", "prompt");
    }

    [Fact]
    public async Task SaveToDirectoryAsync_CreatesDirectoryIfNotExists()
    {
        var dir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString(), "subdir");

        await PromptRecorder.SaveToDirectoryAsync(dir);

        var filePath = Path.Combine(dir, "prompts.json");
        Assert.True(File.Exists(filePath));

        Directory.Delete(dir, true);
    }

    [Fact]
    public async Task SaveToDirectoryAsync_HandlesNullDir()
    {
        await PromptRecorder.SaveToDirectoryAsync(null!);
    }

    [Fact]
    public async Task SaveToDirectoryAsync_HandlesEmptyDir()
    {
        await PromptRecorder.SaveToDirectoryAsync("");
    }

    [Fact]
    public async Task MultipleRecords_AllAppearInOutput()
    {
        PromptRecorder.Record("Src1", "k1", "p1");
        PromptRecorder.Record("Src2", "k2", "p2");
        PromptRecorder.Record("Src3", "k3", "p3");

        var dir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(dir);
        await PromptRecorder.SaveToDirectoryAsync(dir);

        var json = File.ReadAllText(Path.Combine(dir, "prompts.json"));
        Assert.Contains("Src1", json);
        Assert.Contains("Src2", json);
        Assert.Contains("Src3", json);
        Assert.Contains("k1", json);
        Assert.Contains("k2", json);
        Assert.Contains("k3", json);

        Directory.Delete(dir, true);
    }
}
