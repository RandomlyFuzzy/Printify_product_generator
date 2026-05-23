using System.Text.Json;
using PrintifyGenerator.Library.Ollama;

namespace PrintifyGenerator.Library.Tests.Ollama;

public class ChatMessageTests
{
    [Fact]
    public void User_CreatesCorrectRole()
    {
        var msg = ChatMessage.User("hello");
        Assert.Equal("user", msg.Role);
        Assert.Equal("hello", msg.Content);
        Assert.Null(msg.ToolCalls);
    }

    [Fact]
    public void Assistant_CreatesCorrectRole()
    {
        var msg = ChatMessage.Assistant("response");
        Assert.Equal("assistant", msg.Role);
        Assert.Equal("response", msg.Content);
    }

    [Fact]
    public void Assistant_WithToolCalls_StoresThem()
    {
        var calls = new List<ToolCall>
        {
            new("test_fn", [])
        };
        var msg = ChatMessage.Assistant(null, calls);
        Assert.Equal("assistant", msg.Role);
        Assert.Null(msg.Content);
        Assert.Single(msg.ToolCalls!);
    }

    [Fact]
    public void Tool_CreatesCorrectRole()
    {
        var msg = ChatMessage.Tool("call_123", "result data");
        Assert.Equal("tool", msg.Role);
        Assert.Equal("call_123", msg.ToolCallId);
        Assert.Equal("result data", msg.Content);
    }

    [Fact]
    public void System_CreatesCorrectRole()
    {
        var msg = ChatMessage.System("You are helpful");
        Assert.Equal("system", msg.Role);
        Assert.Equal("You are helpful", msg.Content);
    }

    [Fact]
    public void ToDictionary_UserMessage_HasCorrectFields()
    {
        var msg = ChatMessage.User("hello");
        var dict = msg.ToDictionary();

        Assert.Equal("user", dict["role"]);
        Assert.Equal("hello", dict["content"]);
        Assert.False(dict.ContainsKey("tool_calls"));
    }

    [Fact]
    public void ToDictionary_AssistantWithToolCalls_HasToolCalls()
    {
        var args = new Dictionary<string, JsonElement>
        {
            ["query"] = JsonDocument.Parse("\"cats\"").RootElement
        };
        var calls = new List<ToolCall> { new("search", args) };
        var msg = ChatMessage.Assistant(null, calls);
        var dict = msg.ToDictionary();

        Assert.Equal("assistant", dict["role"]);
        Assert.True(dict.ContainsKey("tool_calls"));
        var tcList = (List<Dictionary<string, object>>)dict["tool_calls"]!;
        Assert.Single(tcList);
        var fn = (Dictionary<string, object>)tcList[0]["function"];
        Assert.Equal("search", fn["name"]);
    }

    [Fact]
    public void ToDictionary_ToolMessage_HasToolCallId()
    {
        var msg = ChatMessage.Tool("call_1", "done");
        var dict = msg.ToDictionary();

        Assert.Equal("tool", dict["role"]);
        Assert.Equal("call_1", dict["tool_call_id"]);
        Assert.Equal("done", dict["content"]);
    }
}
