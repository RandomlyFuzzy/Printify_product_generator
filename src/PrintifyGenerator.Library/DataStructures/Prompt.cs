
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
public record Prompt
{
    public string positive { get; set; } = string.Empty;
    public string negative { get; set; } = string.Empty;
    public int width { get; set; } = 512;
    public int height { get; set; }= 512;
    public int steps { get; set; } = 20;
    public float cfg { get; set; } = 3.5f;

    public bool isValid()
    {
        return !string.IsNullOrWhiteSpace(positive) &&
               !string.IsNullOrWhiteSpace(negative) &&
               width > 0 && height > 0 &&
               steps > 0 && cfg > 0;
    }
    public string ToJsonString(JsonSerializerOptions? options = null)
    {
        return JsonSerializer.Serialize(this, options);
    }
    public string ToPrettyJsonString()
    {
        return JsonSerializer.Serialize(this, new JsonSerializerOptions { WriteIndented = true });
    }
}