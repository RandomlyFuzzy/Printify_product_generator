using System.Net;
using System.Text;
using System.Text.Json;
using PrintifyGenerator.Library.Ollama;

namespace PrintifyGenerator.Library.Tests.Ollama;

public class OllamaToolClientTests : IDisposable
{
    private readonly MockHttpHandler _handler;
    private readonly OllamaClient _client;

    public OllamaToolClientTests()
    {
        _handler = new MockHttpHandler();
        _client = new OllamaClient("http://localhost:11434", _handler);
    }

    public void Dispose()
    {
        _client.Dispose();
    }

    [Fact]
    public async Task ChatWithTools_NoToolCall_ReturnsText()
    {
        _handler.Responses.Enqueue(JsonSerializer.Serialize(new
        {
            message = new { role = "assistant", content = "Hello!" }
        }));

        var messages = new List<ChatMessage> { ChatMessage.User("Hi") };
        var tools = new List<ToolDefinition>();

        var result = await _client.ChatWithToolsAsync("test-model", messages, tools, _ => Task.FromResult(""));

        Assert.Equal("Hello!", result.Content);
    }

    [Fact]
    public async Task ChatWithTools_SingleToolCall_ExecutesAndReturns()
    {
        _handler.Responses.Enqueue(JsonSerializer.Serialize(new
        {
            message = new
            {
                role = "assistant",
                content = (string?)null,
                tool_calls = new[]
                {
                    new
                    {
                        function = new { name = "test_tool", arguments = "{}" }
                    }
                }
            }
        }));

        _handler.Responses.Enqueue(JsonSerializer.Serialize(new
        {
            message = new { role = "assistant", content = "Tool result: done" }
        }));

        var messages = new List<ChatMessage> { ChatMessage.User("Run tool") };
        var schema = JsonDocument.Parse("{\"type\":\"object\"}").RootElement;
        var tools = new List<ToolDefinition>
        {
            new("test_tool", "A test tool", schema)
        };

        bool toolExecuted = false;
        var result = await _client.ChatWithToolsAsync("test-model", messages, tools, call =>
        {
            toolExecuted = true;
            Assert.Equal("test_tool", call.Name);
            return Task.FromResult("success");
        });

        Assert.True(toolExecuted);
        Assert.Equal("Tool result: done", result.Content);
    }

    [Fact]
    public async Task ChatWithTools_MultipleToolCalls_ExecutesAll()
    {
        _handler.Responses.Enqueue(JsonSerializer.Serialize(new
        {
            message = new
            {
                role = "assistant",
                content = (string?)null,
                tool_calls = new[]
                {
                    new { function = new { name = "tool_a", arguments = "{}" } },
                    new { function = new { name = "tool_b", arguments = "{}" } }
                }
            }
        }));

        _handler.Responses.Enqueue(JsonSerializer.Serialize(new
        {
            message = new { role = "assistant", content = "All done" }
        }));

        var messages = new List<ChatMessage> { ChatMessage.User("Both") };
        var schema = JsonDocument.Parse("{\"type\":\"object\"}").RootElement;
        var tools = new List<ToolDefinition>
        {
            new("tool_a", "Tool A", schema),
            new("tool_b", "Tool B", schema)
        };

        var executed = new List<string>();
        var result = await _client.ChatWithToolsAsync("test-model", messages, tools, call =>
        {
            executed.Add(call.Name);
            return Task.FromResult($"result_{call.Name}");
        });

        Assert.Equal(2, executed.Count);
        Assert.Contains("tool_a", executed);
        Assert.Contains("tool_b", executed);
        Assert.Equal("All done", result.Content);
    }

    [Fact]
    public async Task ChatWithTools_ToolError_ReturnsErrorMessage()
    {
        _handler.Responses.Enqueue(JsonSerializer.Serialize(new
        {
            message = new
            {
                role = "assistant",
                content = (string?)null,
                tool_calls = new[]
                {
                    new { function = new { name = "fail_tool", arguments = "{}" } }
                }
            }
        }));

        _handler.Responses.Enqueue(JsonSerializer.Serialize(new
        {
            message = new { role = "assistant", content = "Continuing" }
        }));

        var messages = new List<ChatMessage> { ChatMessage.User("Fail") };
        var schema = JsonDocument.Parse("{\"type\":\"object\"}").RootElement;
        var tools = new List<ToolDefinition>
        {
            new("fail_tool", "Failing tool", schema)
        };

        var result = await _client.ChatWithToolsAsync("test-model", messages, tools, call =>
        {
            throw new InvalidOperationException("Something broke");
        });

        Assert.Equal("Continuing", result.Content);
    }

    [Fact]
    public async Task ChatWithTools_MaxTurns_ReturnsMessage()
    {
        var schema = JsonDocument.Parse("{\"type\":\"object\"}").RootElement;
        var tools = new List<ToolDefinition>
        {
            new("loop_tool", "Loops", schema)
        };

        for (int i = 0; i < 11; i++)
        {
            _handler.Responses.Enqueue(JsonSerializer.Serialize(new
            {
                message = new
                {
                    role = "assistant",
                    content = (string?)null,
                    tool_calls = new[]
                    {
                        new { function = new { name = "loop_tool", arguments = "{}" } }
                    }
                }
            }));
        }

        var messages = new List<ChatMessage> { ChatMessage.User("Loop") };
        var result = await _client.ChatWithToolsAsync("test-model", messages, tools,
            _ => Task.FromResult("still going"), maxTurns: 5);

        Assert.Equal("Max turns reached", result.Content);
    }

    [Fact]
    public async Task ChatWithTools_PreservesMessageHistory()
    {
        _handler.Responses.Enqueue(JsonSerializer.Serialize(new
        {
            message = new { role = "assistant", content = "First response" }
        }));

        var messages = new List<ChatMessage> { ChatMessage.User("Start") };
        var tools = new List<ToolDefinition>();

        var result = await _client.ChatWithToolsAsync("test-model", messages, tools, _ => Task.FromResult(""));

        Assert.Equal(2, result.MessageHistory.Count);
        Assert.Equal("user", result.MessageHistory[0]["role"]);
        Assert.Equal("assistant", result.MessageHistory[1]["role"]);
    }

    private class MockHttpHandler : HttpMessageHandler
    {
        public Queue<string> Responses = new();

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
        {
            var body = Responses.Dequeue();
            var response = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(body, Encoding.UTF8, "application/json")
            };
            return Task.FromResult(response);
        }
    }
}
