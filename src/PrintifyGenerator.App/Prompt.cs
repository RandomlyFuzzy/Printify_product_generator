
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
public record Prompt
{
    public string positive { get; set; }
    public string negative { get; set; }
    public int width { get; set; }
    public int height { get; set; }
    public int steps { get; set; }
    public int cfg { get; set; }

    public bool isValid()
    {
        return !string.IsNullOrWhiteSpace(positive) &&
               !string.IsNullOrWhiteSpace(negative) &&
               width > 0 && height > 0 &&
               steps > 0 && cfg > 0;
    }
    public string ToJsonString(JsonSerializerOptions options = null)
    {
        return JsonSerializer.Serialize(this, options);
    }
    public string ToPrityJsonString()
    {
        return JsonSerializer.Serialize(this, new JsonSerializerOptions { WriteIndented = true });
    }
}