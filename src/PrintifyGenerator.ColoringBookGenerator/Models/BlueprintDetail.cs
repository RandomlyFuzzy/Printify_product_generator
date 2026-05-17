using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace PrintifyGenerator.ColoringBookGenerator.Models
{
    public class BlueprintDetail
    {
        [JsonPropertyName("blueprint")]
        public Blueprint Blueprint { get; set; }

        [JsonPropertyName("print_providers")]
        public List<PrintProvider> PrintProviders { get; set; }
    }

    public class Blueprint
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }
        [JsonPropertyName("title")]
        public string Title { get; set; }
        [JsonPropertyName("brand")]
        public string Brand { get; set; }
        [JsonPropertyName("model")]
        public string Model { get; set; }
        [JsonPropertyName("images")]
        public List<string> Images { get; set; }
    }

    public class PrintProvider
    {
        [JsonPropertyName("provider")]
        public Provider Provider { get; set; }

        [JsonPropertyName("variants")]
        public VariantsContainer Variants { get; set; }
    }

    public class Provider
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }
        [JsonPropertyName("title")]
        public string Title { get; set; }
    }

    public class VariantsContainer
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }
        [JsonPropertyName("title")]
        public string Title { get; set; }
        [JsonPropertyName("variants")]
        public List<Variant> Variants { get; set; }
    }

    public class Variant
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }

        [JsonPropertyName("title")]
        public string Title { get; set; }

        [JsonPropertyName("placeholders")]
        public List<Placeholder> Placeholders { get; set; }
    }

    public class Placeholder
    {
        [JsonPropertyName("position")]
        public string Position { get; set; }
        [JsonPropertyName("decoration_method")]
        public string DecorationMethod { get; set; }
        [JsonPropertyName("height")]
        public int Height { get; set; }
        [JsonPropertyName("width")]
        public int Width { get; set; }
    }
}
