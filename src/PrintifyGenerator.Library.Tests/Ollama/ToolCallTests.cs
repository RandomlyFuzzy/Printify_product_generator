using System.Text.Json;
using PrintifyGenerator.Library.Ollama;

namespace PrintifyGenerator.Library.Tests.Ollama;

public class ToolCallTests
{
    [Fact]
    public void ParseList_SingleToolCall_ReturnsOne()
    {
        var json = JsonDocument.Parse("""
            [
              {
                "function": {
                  "name": "get_weather",
                  "arguments": {"location": "London"}
                }
              }
            ]
            """).RootElement;

        var calls = ToolCall.ParseList(json);
        Assert.Single(calls);
        Assert.Equal("get_weather", calls[0].Name);
        Assert.Equal("London", calls[0].Arguments["location"].GetString());
    }

    [Fact]
    public void ParseList_MultipleToolCalls_ReturnsAll()
    {
        var json = JsonDocument.Parse("""
            [
              {"function": {"name": "fn_a", "arguments": {"x": 1}}},
              {"function": {"name": "fn_b", "arguments": {"y": 2}}}
            ]
            """).RootElement;

        var calls = ToolCall.ParseList(json);
        Assert.Equal(2, calls.Count);
        Assert.Equal("fn_a", calls[0].Name);
        Assert.Equal("fn_b", calls[1].Name);
    }

    [Fact]
    public void ParseList_EmptyArray_ReturnsEmpty()
    {
        var json = JsonDocument.Parse("[]").RootElement;
        var calls = ToolCall.ParseList(json);
        Assert.Empty(calls);
    }

    [Fact]
    public void ParseList_NoArguments_ReturnsEmptyDict()
    {
        var json = JsonDocument.Parse("""
            [
              {"function": {"name": "no_args", "arguments": {}}}
            ]
            """).RootElement;

        var calls = ToolCall.ParseList(json);
        Assert.Single(calls);
        Assert.Empty(calls[0].Arguments);
    }

    [Fact]
    public void ParseList_MalformedArguments_DoesNotThrow()
    {
        var json = JsonDocument.Parse("""
            [
              {"function": {"name": "bad_args", "arguments": "not_an_object"}}
            ]
            """).RootElement;

        var calls = ToolCall.ParseList(json);
        Assert.Single(calls);
        Assert.Empty(calls[0].Arguments);
    }

    [Fact]
    public void Constructor_NullName_Throws()
    {
        Assert.Throws<ArgumentNullException>(() => new ToolCall(null!, []));
    }

    [Fact]
    public void Constructor_WithArguments_StoresThem()
    {
        var args = new Dictionary<string, JsonElement>
        {
            ["key"] = JsonDocument.Parse("\"value\"").RootElement
        };
        var call = new ToolCall("test", args);
        Assert.Equal("test", call.Name);
        Assert.Single(call.Arguments);
    }
}
