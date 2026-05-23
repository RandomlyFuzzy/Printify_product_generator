using System.Text.Json;

namespace PrintifyGenerator.Library.Ollama;

public class ToolDefinition
{
    public string Name { get; }
    public string Description { get; }
    public JsonElement Parameters { get; }

    public ToolDefinition(string name, string description, JsonElement parameters)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Tool name cannot be empty", nameof(name));
        Name = name;
        Description = description ?? "";
        Parameters = parameters;
    }

    public JsonElement ToJson()
    {
        var raw = $"{{\"type\":\"function\",\"function\":{{\"name\":\"{Name}\",\"description\":\"{Description}\",\"parameters\":{Parameters.GetRawText()}}}}}";
        using var doc = JsonDocument.Parse(raw);
        return doc.RootElement.Clone();
    }
}
