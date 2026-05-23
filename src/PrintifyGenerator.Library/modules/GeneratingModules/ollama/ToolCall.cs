using System.Text.Json;

namespace PrintifyGenerator.Library.Ollama;

public class ToolCall
{
    public string Name { get; }
    public Dictionary<string, JsonElement> Arguments { get; }

    public ToolCall(string name, Dictionary<string, JsonElement> arguments)
    {
        Name = name ?? throw new ArgumentNullException(nameof(name));
        Arguments = arguments ?? [];
    }

    public static List<ToolCall> ParseList(JsonElement toolCallsElement)
    {
        var calls = new List<ToolCall>();

        foreach (var tc in toolCallsElement.EnumerateArray())
        {
            var fn = tc.GetProperty("function");
            var name = fn.GetProperty("name").GetString()!;
            var rawArgs = fn.GetProperty("arguments").GetRawText();

            Dictionary<string, JsonElement> args;
            try
            {
                using var argsDoc = JsonDocument.Parse(rawArgs);
                args = [];
                foreach (var prop in argsDoc.RootElement.EnumerateObject())
                    args[prop.Name] = prop.Value.Clone();
            }
            catch
            {
                args = [];
            }

            calls.Add(new ToolCall(name, args));
        }

        return calls;
    }
}
