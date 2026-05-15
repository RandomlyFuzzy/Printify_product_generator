namespace PrintifyGenerator.Library.Tests.DataStructures;

public class PromptTests
{
    [Fact]
    public void IsValid_WithValidValues_ReturnsTrue()
    {
        var sut = new Prompt
        {
            positive = "a cat",
            negative = "blurry",
            width = 512,
            height = 512,
            steps = 20,
            cfg = 3.5f
        };
        Assert.True(sut.isValid());
    }

    [Fact]
    public void IsValid_WithEmptyPositive_ReturnsFalse()
    {
        var sut = new Prompt { positive = "", negative = "bad", width = 512, height = 512, steps = 20, cfg = 3.5f };
        Assert.False(sut.isValid());
    }

    [Fact]
    public void IsValid_WithEmptyNegative_ReturnsFalse()
    {
        var sut = new Prompt { positive = "cat", negative = "", width = 512, height = 512, steps = 20, cfg = 3.5f };
        Assert.False(sut.isValid());
    }

    [Fact]
    public void IsValid_WithZeroWidth_ReturnsFalse()
    {
        var sut = new Prompt { positive = "cat", negative = "bad", width = 0, height = 512, steps = 20, cfg = 3.5f };
        Assert.False(sut.isValid());
    }

    [Fact]
    public void IsValid_WithZeroHeight_ReturnsFalse()
    {
        var sut = new Prompt { positive = "cat", negative = "bad", width = 512, height = 0, steps = 20, cfg = 3.5f };
        Assert.False(sut.isValid());
    }

    [Fact]
    public void IsValid_WithZeroSteps_ReturnsFalse()
    {
        var sut = new Prompt { positive = "cat", negative = "bad", width = 512, height = 512, steps = 0, cfg = 3.5f };
        Assert.False(sut.isValid());
    }

    [Fact]
    public void IsValid_WithZeroCfg_ReturnsFalse()
    {
        var sut = new Prompt { positive = "cat", negative = "bad", width = 512, height = 512, steps = 20, cfg = 0 };
        Assert.False(sut.isValid());
    }

    [Fact]
    public void ToJsonString_ReturnsValidJson()
    {
        var sut = new Prompt { positive = "test", negative = "bad" };
        var json = sut.ToJsonString();
        Assert.Contains("\"positive\"", json);
        Assert.Contains("\"test\"", json);
    }

    [Fact]
    public void ToPrettyJsonString_ReturnsIndentedJson()
    {
        var sut = new Prompt { positive = "test", negative = "bad" };
        var json = sut.ToPrettyJsonString();
        Assert.Contains("\n", json);
    }
}
