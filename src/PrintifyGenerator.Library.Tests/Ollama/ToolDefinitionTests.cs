using System.Text.Json;
using PrintifyGenerator.Library.Ollama;

namespace PrintifyGenerator.Library.Tests.Ollama;

public class ToolDefinitionTests
{
    [Fact]
    public void Constructor_WithValidArgs_SetsProperties()
    {
        var parameters = JsonDocument.Parse("{\"type\":\"object\",\"properties\":{}}").RootElement;
        var def = new ToolDefinition("test_tool", "A test tool", parameters);

        Assert.Equal("test_tool", def.Name);
        Assert.Equal("A test tool", def.Description);
    }

    [Fact]
    public void Constructor_EmptyName_Throws()
    {
        var parameters = JsonDocument.Parse("{}").RootElement;
        Assert.Throws<ArgumentException>(() => new ToolDefinition("", "desc", parameters));
    }

    [Fact]
    public void Constructor_NullName_Throws()
    {
        var parameters = JsonDocument.Parse("{}").RootElement;
        Assert.Throws<ArgumentException>(() => new ToolDefinition("   ", "desc", parameters));
    }

    [Fact]
    public void ToJson_ContainsExpectedFields()
    {
        var schema = JsonDocument.Parse("""
            {"type":"object","properties":{"location":{"type":"string"}},"required":["location"]}
            """).RootElement;

        var def = new ToolDefinition("get_weather", "Get weather for a location", schema);
        var json = def.ToJson();

        Assert.Equal("function", json.GetProperty("type").GetString());
        Assert.Equal("get_weather", json.GetProperty("function").GetProperty("name").GetString());
        Assert.Equal("Get weather for a location", json.GetProperty("function").GetProperty("description").GetString());
        Assert.True(json.GetProperty("function").GetProperty("parameters").GetProperty("required").EnumerateArray().Any());
    }

    [Fact]
    public void ToJson_MinimalParameters_StillValid()
    {
        var schema = JsonDocument.Parse("{\"type\":\"object\"}").RootElement;
        var def = new ToolDefinition("minimal", "", schema);
        var json = def.ToJson();

        Assert.Equal("minimal", json.GetProperty("function").GetProperty("name").GetString());
    }
}
