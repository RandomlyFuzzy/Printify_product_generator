using System.Text.Json;

namespace PrintifyGenerator.Library.Ollama;

public class ChatMessage
{
    public string Role { get; }
    public string? Content { get; }
    public List<ToolCall>? ToolCalls { get; }
    public string? ToolCallId { get; }

    public ChatMessage(string role, string? content = null, List<ToolCall>? toolCalls = null, string? toolCallId = null)
    {
        Role = role ?? throw new ArgumentNullException(nameof(role));
        Content = content;
        ToolCalls = toolCalls;
        ToolCallId = toolCallId;
    }

    public static ChatMessage User(string content) => new("user", content);
    public static ChatMessage Assistant(string? content = null, List<ToolCall>? toolCalls = null) => new("assistant", content, toolCalls);
    public static ChatMessage Tool(string toolCallId, string content) => new("tool", content, toolCallId: toolCallId);
    public static ChatMessage System(string content) => new("system", content);

    public Dictionary<string, object> ToDictionary()
    {
        var dict = new Dictionary<string, object> { ["role"] = Role };

        if (Content != null)
            dict["content"] = Content;

        if (ToolCalls is { Count: > 0 })
        {
            var calls = new List<Dictionary<string, object>>();
            foreach (var tc in ToolCalls)
            {
                var argsDict = new Dictionary<string, object>();
                foreach (var kvp in tc.Arguments)
                    argsDict[kvp.Key] = JsonSerializer.Deserialize<object>(kvp.Value.GetRawText()) ?? "";

                var argsJson = JsonSerializer.Serialize(argsDict);
                calls.Add(new Dictionary<string, object>
                {
                    ["type"] = "function",
                    ["function"] = new Dictionary<string, object>
                    {
                        ["name"] = tc.Name,
                        ["arguments"] = argsJson
                    }
                });
            }
            dict["tool_calls"] = calls;
        }

        if (ToolCallId != null)
            dict["tool_call_id"] = ToolCallId;

        return dict;
    }
}
