using PrintifyGenerator.ColoringBookGenerator.Models;
using PrintifyGenerator.ColoringBookGenerator.Utilities;

namespace PrintifyGenerator.ColoringBookGenerator.Tests.Utilities;

public class BlueprintLoaderTests
{
    [Fact]
    public async Task LoadAsync_DeserializesMinimalJson()
    {
        var json = """
        {
            "blueprint": {
                "id": 2721,
                "title": "Coloring Book",
                "brand": "District Photo",
                "model": "8.5\" x 11\"",
                "images": ["https://example.com/img.png"]
            },
            "print_providers": [
                {
                    "provider": { "id": 28, "title": "District Photo" },
                    "variants": {
                        "id": 1,
                        "title": "Standard",
                        "variants": [
                            {
                                "id": 148586,
                                "title": "8.5\" x 11\" Coloring Book",
                                "placeholders": [
                                    {
                                        "position": "cover",
                                        "decoration_method": "digital-printing",
                                        "height": 3375,
                                        "width": 5175
                                    }
                                ]
                            }
                        ]
                    }
                }
            ]
        }
        """;

        var path = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString() + ".json");
        try
        {
            await File.WriteAllTextAsync(path, json);
            var result = await BlueprintLoader.LoadAsync(path);

            Assert.NotNull(result);
            Assert.NotNull(result.Blueprint);
            Assert.Equal(2721, result.Blueprint.Id);
            Assert.Equal("Coloring Book", result.Blueprint.Title);
            Assert.Equal("District Photo", result.Blueprint.Brand);
            Assert.Single(result.Blueprint.Images);
        }
        finally
        {
            if (File.Exists(path)) File.Delete(path);
        }
    }

    [Fact]
    public async Task LoadAsync_DeserializesPrintProviders()
    {
        var json = """
        {
            "blueprint": { "id": 1, "title": "T", "brand": "B", "model": "M", "images": [] },
            "print_providers": [
                {
                    "provider": { "id": 28, "title": "District Photo" },
                    "variants": { "id": 1, "title": "Std", "variants": [] }
                }
            ]
        }
        """;

        var path = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString() + ".json");
        try
        {
            await File.WriteAllTextAsync(path, json);
            var result = await BlueprintLoader.LoadAsync(path);

            Assert.NotNull(result.PrintProviders);
            Assert.Single(result.PrintProviders);
            Assert.Equal(28, result.PrintProviders[0].Provider.Id);
            Assert.Equal("District Photo", result.PrintProviders[0].Provider.Title);
        }
        finally
        {
            if (File.Exists(path)) File.Delete(path);
        }
    }

    [Fact]
    public async Task LoadAsync_DeserializesVariants()
    {
        var json = """
        {
            "blueprint": { "id": 1, "title": "T", "brand": "B", "model": "M", "images": [] },
            "print_providers": [
                {
                    "provider": { "id": 28, "title": "DP" },
                    "variants": {
                        "id": 1,
                        "title": "Standard",
                        "variants": [
                            {
                                "id": 148586,
                                "title": "8.5x11",
                                "placeholders": [
                                    { "position": "cover", "decoration_method": "digital-printing", "height": 3375, "width": 5175 }
                                ]
                            }
                        ]
                    }
                }
            ]
        }
        """;

        var path = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString() + ".json");
        try
        {
            await File.WriteAllTextAsync(path, json);
            var result = await BlueprintLoader.LoadAsync(path);

            var variants = result.PrintProviders[0].Variants.Variants;
            Assert.Single(variants);
            Assert.Equal(148586, variants[0].Id);
            Assert.Equal("8.5x11", variants[0].Title);
            Assert.Single(variants[0].Placeholders);
            Assert.Equal("cover", variants[0].Placeholders[0].Position);
        }
        finally
        {
            if (File.Exists(path)) File.Delete(path);
        }
    }
}
