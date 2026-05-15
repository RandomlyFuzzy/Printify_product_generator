using System.Text.Json.Nodes;

namespace PrintifyGenerator.Library.Tests.Modules.Utility;

public class JsonXPathTests
{
    private static JsonNode SampleJson()
    {
        return JsonNode.Parse("""
        {
            "store": {
                "name": "MyShop",
                "address": {
                    "city": "London",
                    "country": "UK"
                }
            },
            "products": [
                { "id": 1, "title": "Hat" },
                { "id": 2, "title": "Shirt" }
            ]
        }
        """)!;
    }

    [Fact]
    public void Select_WithSimplePath_ReturnsValue()
    {
        var root = SampleJson();
        var results = JsonXPath.Select(root, "store/name");
        Assert.Single(results);
        Assert.Equal("MyShop", results[0]!.ToString());
    }

    [Fact]
    public void Select_WithNestedPath_ReturnsValue()
    {
        var root = SampleJson();
        var results = JsonXPath.Select(root, "store/address/city");
        Assert.Single(results);
        Assert.Equal("London", results[0]!.ToString());
    }

    [Fact]
    public void Select_WithWildcard_ReturnsAllMatching()
    {
        var root = SampleJson();
        var results = JsonXPath.Select(root, "store/*");
        Assert.Equal(2, results.Count);
    }

    [Fact]
    public void Select_WithRecursiveDescent_ReturnsNestedMatches()
    {
        var root = SampleJson();
        var results = JsonXPath.Select(root, "//city");
        Assert.Single(results);
        Assert.Equal("London", results[0]!.ToString());
    }

    [Fact]
    public void Select_WithRecursiveDescentWildcard_ReturnsAll()
    {
        var root = SampleJson();
        var results = JsonXPath.Select(root, "//*");
        Assert.True(results.Count > 0);
    }

    [Fact]
    public void Select_WithNonexistentPath_ReturnsEmpty()
    {
        var root = SampleJson();
        var results = JsonXPath.Select(root, "store/nonexistent");
        Assert.Empty(results);
    }

    [Fact]
    public void Set_OverwritesExistingValue()
    {
        var root = SampleJson();
        JsonXPath.Set(root, "store/name", JsonNode.Parse("\"NewName\"")!);
        var results = JsonXPath.Select(root, "store/name");
        Assert.Equal("NewName", results[0]!.ToString());
    }

    [Fact]
    public void Set_WithWildcard_UpdatesAllMatching()
    {
        var root = JsonNode.Parse("""{ "a": { "x": 1 }, "b": { "x": 2 } }""")!;
        JsonXPath.Set(root, "*/x", JsonNode.Parse("99")!);
        Assert.Equal("99", root["a"]!["x"]!.ToString());
        Assert.Equal("99", root["b"]!["x"]!.ToString());
    }

    [Fact]
    public void Select_WithRecursiveDescentAtStart_FindsNested()
    {
        var root = JsonNode.Parse("""{ "a": { "b": { "c": "found" } } }""")!;
        var results = JsonXPath.Select(root, "//c");
        Assert.Single(results);
        Assert.Equal("found", results[0]!.ToString());
    }

    [Fact]
    public void Select_WithRecursiveDescent_DeepNestedObject()
    {
        var root = JsonNode.Parse("""{ "a": { "b": { "c": "deep" }, "d": { "c": "also" } } }""")!;
        var results = JsonXPath.Select(root, "//c");
        Assert.Equal(2, results.Count);
    }
}
