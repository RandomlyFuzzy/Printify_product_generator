using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

public class MockupGenerator
{
    private readonly PrintifyClient _printify;
    private readonly OllamaClient _ollama;
    private readonly int _shopId;

    const string dataBasePath = @"./src/data";
    const string _blueprintDetailsPath = dataBasePath+"/Cached"+ "/blueprint_details";

    private readonly Dictionary<string, int> _stats = new();

    public MockupGenerator(PrintifyClient printify, OllamaClient ollama, int shopId)
    {
        _printify = printify;
        _ollama = ollama;
        _shopId = shopId;
    }

    // =========================
    // ENTRY
    // =========================
    public async Task<List<MockupResult>> ProcessImageAsync(string imagePath)
    {
        var results = new List<MockupResult>();

        var analysis = ImageAnalyzer.Analyze(imagePath);

        var uploaded = await _printify.UploadImageFromFileAsync(imagePath);
        var blueprints = await _printify.GetBlueprintsAsync();

        var trimmedBlueprints = TrimBlueprints(blueprints, analysis);
        // Console.WriteLine($"Trimmed to {trimmedBlueprints.Count} from {blueprints.Count} blueprints after heuristic filtering.");

        var selected = await SelectBlueprints(imagePath, trimmedBlueprints);
        foreach (var bp in selected)
        {
            Console.WriteLine($"Blueprint {bp.BlueprintId}: {bp.BlueprintTitle} | Reason: {bp.Reason}");
        }

        if (!selected.Any())
        {
            Log("LLM returned no blueprints");
            PrintStats();
            return results;
        }

        foreach (var bp in selected)
        {
            var providers = await _printify.GetBlueprintPrintProvidersAsync(bp.BlueprintId);

            if (!providers.Any())
            {
                Log("No providers available");
                continue;
            }

            var provider = providers.First();

            var variantResponse =
                await _printify.GetBlueprintVariantsAsync(bp.BlueprintId, provider.Id);

            var variants = variantResponse.Variants;

            if (!variants.Any())
            {
                Log("No variants returned");
                continue;
            }

            var filtered = ScoreAndFilterVariants(variants, analysis);

            if (!filtered.Any())
            {
                Log("No suitable variants after scoring");
                continue;
            }

            var printAreas = BuildPrintAreas(filtered, uploaded.Id, analysis);

            var product = await CreateProduct(bp, provider, filtered, printAreas);

            results.Add(new MockupResult
            {
                Success = true,
                PrintifyImageId = uploaded.Id,
                Draft = new MockupDraftRecord
                {
                    ProductId = product.Id,
                    JobId = Path.GetFileNameWithoutExtension(imagePath) + "_" + bp.BlueprintId,
                    LookupKey = $"{bp.BlueprintId}:{provider.Id}",
                    GroupKey = bp.BlueprintId.ToString(),
                    AssetKey = uploaded.Id,
                    ReferenceCode = Guid.NewGuid().ToString(),
                    LocalImagePath = imagePath,
                    PrintifyImageId = uploaded.Id,
                    PrintifyImagePreviewUrl = uploaded.PreviewUrl,
                    BlueprintId = bp.BlueprintId,
                    BlueprintTitle = bp.BlueprintTitle,
                    LlmReason = bp.Reason,
                    PrintProviderId = provider.Id,
                    PrintProviderTitle = provider.Title,
                    CreatedAt = DateTime.UtcNow.ToString("o"),
                    MockupUrls = product.Images.Select(i => i.Src).ToList()
                }
            });
        }

        PrintStats();
        return results;
    }


    // =========================
    // BLUEPRINT Filter
    // =========================
    private List<Blueprint> TrimBlueprints(List<Blueprint> blueprints, ImageAnalysis analysis)
    {
        var scored = new List<(Blueprint bp, double score)>();

        foreach (var bp in blueprints)
        {
            double score = 0;

            var title = bp.Title?.ToLowerInvariant() ?? "";

            // =========================
            // 1. CATEGORY PRIORITY (strong signal)
            // =========================
            if (title.Contains("t-shirt") || title.Contains("tee")) score += 6;
            if (title.Contains("hoodie")) score += 6;
            if (title.Contains("sweatshirt")) score += 5;
            if (title.Contains("poster")) score += 5;
            if (title.Contains("canvas")) score += 5;
            if (title.Contains("mug")) score += 5;
            if (title.Contains("sticker")) score += 4;
            if (title.Contains("tote")) score += 4;
            if (title.Contains("phone case")) score += 4;
            if (title.Contains("hat") || title.Contains("cap")) score += 3;

            // penalise awkward / low-quality merch types
            if (title.Contains("glass")) score -= 10;
            if (title.Contains("ornament")) score -= 5;
            if (title.Contains("keychain")) score -= 2;

            // =========================
            // 2. PRINT AREA ASPECT RATIO FIT (IMPORTANT)
            // =========================
            try
            {
                var entry = BuildCatalogueEntry(bp);

                if (entry.Locations != null && entry.Locations.Count > 0)
                {
                    // FIRST print location only (your rule)
                    var first = entry.Locations.First();

                    var printRatio = (double)first.Value.SizeX / first.Value.SizeY;

                    score *= AspectRatioMultiplier(analysis.AspectRatio, printRatio);
                }
            }
            catch
            {
                // ignore broken blueprint metadata
                score *= 0.5;
            }

            // =========================
            // 3. IMAGE COMPATIBILITY BOOSTS
            // =========================
            if (analysis.AspectRatio > 1.5 && (title.Contains("poster") || title.Contains("landscape") || title.Contains("horizontal")))
                score += 1;
            else if (analysis.AspectRatio < 0.8 && (title.Contains("portrait") || title.Contains("vertical")))
                score += 1;
            else if (analysis.DetailScore > 0.8 && title.Contains("square"))
                score += 1;


            // =========================
            // 4. BASELINE (prevents zero collapse)
            // =========================
            score += 0.1;

            scored.Add((bp, score));
        }

        // =========================
        // FINAL SELECTION
        // =========================
        return scored
            .OrderByDescending(x => x.score)
            .Take(150)
            .Select(x => x.bp)
            .ToList();
    }
    /// <summary> /// Builds a <see cref="BlueprintCatalogueEntry"/> for a blueprint, 
    /// /// enriching it with print-location dimensions from the cached 
    /// /// blueprint_details JSON files when available. 
    /// /// </summary> 
    private BlueprintCatalogueEntry BuildCatalogueEntry(Blueprint blueprint)
    {
        var entry = new BlueprintCatalogueEntry { Id = blueprint.Id, Title = blueprint.Title }; 
        var detailFile = Path.Combine(_blueprintDetailsPath, $"{blueprint.Id}.json"); 
        if (!File.Exists(detailFile)) 
            return entry; 
        try
        {
            var json = File.ReadAllText(detailFile); 
            var doc = JsonDocument.Parse(json); 
            var root = doc.RootElement;
            // Collect unique positions and their max dimensions across all 
            // // providers / variants so the LLM sees the full print surface. 
            var locations = new Dictionary<string, PrintLocationSize>(StringComparer.OrdinalIgnoreCase); if (root.TryGetProperty("print_providers", out var providers))
            {
                foreach (var pp in providers.EnumerateArray())
                {
                    if (!pp.TryGetProperty("variants", out var variantsWrapper)) continue; if (!variantsWrapper.TryGetProperty("variants", out var variants)) continue; foreach (var v in variants.EnumerateArray())
                    {
                        if (!v.TryGetProperty("placeholders", out var placeholders)) 
                            continue;

                        foreach (var ph in placeholders.EnumerateArray())
                        {
                            var position = ph.GetProperty("position").GetString() ?? ""; var width = ph.TryGetProperty("width", out var w) ? w.GetInt32() : 0; var height = ph.TryGetProperty("height", out var h) ? h.GetInt32() : 0; if (locations.TryGetValue(position, out var existing)) { if (width > existing.SizeX) existing.SizeX = width; if (height > existing.SizeY) existing.SizeY = height; } else { locations[position] = new PrintLocationSize { SizeX = width, SizeY = height }; }
                        } // One variant per provider is enough to capture the positions. 
                        break;
                    }
                }
            }
            entry.PrintLocations = locations.Keys.ToList(); entry.Locations = locations;
        }
        catch
        { // Cached file is corrupt or unexpected format – return basic entry. 
        } 
        return entry; 
    }
    private static double AspectRatioMultiplier(double imageRatio, double printRatio)
    {
        if (imageRatio <= 0 || printRatio <= 0)
            return 0.1;

        var diff = Math.Abs(imageRatio - printRatio) / imageRatio;

        // every 10% difference halves the score
        var steps = Math.Floor(diff / 0.1);

        return 2.0 * Math.Pow(0.5, steps);
    }

    // =========================
    // LLM SELECTION
    // =========================
    private async Task<List<BlueprintSuggestion>> SelectBlueprints(string imagePath, List<Blueprint> blueprints)
    {
        try
        {

            string response = "";

            await foreach (var chunk in _ollama.GenerateWithImageStreamAsync(
                "gemma4:e4b",
                "Pick 3 best product types. Return JSON [{id,title}] only. "+BuildLlmBlueprintPayload(blueprints) ,
                imagePath))
            {
                response += chunk;
                // Console.Write($"{chunk}");
            }

            return System.Text.Json.JsonSerializer.Deserialize<List<BlueprintSuggestion>>(
                       ExtractJson(response))
                   ?? new List<BlueprintSuggestion>();
        }
        catch
        {
            Log("LLM parsing failed");
            return new List<BlueprintSuggestion>();
        }
    }
    private static string BuildLlmBlueprintPayload(List<Blueprint> blueprints)
    {
        var simplified = blueprints.Select(b => new
        {
            id = b.Id,
            title = b.Title
        });

        return System.Text.Json.JsonSerializer.Serialize(simplified);
    }
    // =========================
    // VARIANT SELECTION (NO COST)
    // =========================
    private List<Variant> ScoreAndFilterVariants(List<Variant> variants, ImageAnalysis analysis)
    {
        var scored = new List<(Variant v, double score)>();
        foreach (var v in variants)
        {
            if (v.Placeholders == null || v.Placeholders.Count == 0)
                continue;

            // 🚫 REMOVE EMBROIDERY PLACEHOLDERS
            v.Placeholders = v.Placeholders
                .Where(p =>
                    !string.Equals(p.DecorationMethod, "embroidery", StringComparison.OrdinalIgnoreCase)
                )
                .ToList();

            // if nothing usable remains, drop variant entirely
            if (!v.Placeholders.Any())
            {
                Log("Variant removed (embroidery only)");
                continue;
            }

            var ph = v.Placeholders.First();

            if (ph.Width <= 0 || ph.Height <= 0)
            {
                Log("Invalid placeholder size");
                continue;
            }

            double score = 0;

            // print area size is KING
            score += (ph.Width * ph.Height) / 1_000_000.0;

            // image complexity fit
            if (analysis.DetailScore > 0.7 && ph.Width < 2000)
                score -= 2;

            // aspect harmony
            var ratio = (double)ph.Width / ph.Height;
            var diff = Math.Abs(ratio - analysis.AspectRatio) / analysis.AspectRatio;

            if (diff < 0.2)
                score += 1;
            else if (diff > 0.5)
                score -= 1;

            scored.Add((v, score));
        }

        return scored
            .OrderByDescending(x => x.score)
            .Take(30)
            .Select(x => x.v)
            .ToList();
    }

    // =========================
    // PRINT AREAS
    // =========================
    private List<PrintArea> BuildPrintAreas(List<Variant> variants, string imageId, ImageAnalysis analysis)
    {
        return variants
            .GroupBy(v => v.Placeholders.First().Position)
            .Select(g =>
            {
                var ph = g.First().Placeholders.First();

                return new PrintArea
                {
                    VariantIds = g.Select(v => v.Id).ToList(),
                    Placeholders = new List<PrintAreaPlaceholder>
                    {
                        new PrintAreaPlaceholder
                        {
                            Position = ph.Position,
                            Images = new List<PrintAreaImage>
                            {
                                new PrintAreaImage
                                {
                                    Id = imageId,
                                    X = 0.5,
                                    Y = 0.5,
                                    Scale = analysis.DetailScore > 0.7 ? 0.7 : 0.85,
                                    Width = 1,
                                    Height = 1
                                }
                            }
                        }
                    }
                };
            })
            .ToList();
    }

    // =========================
    // PRODUCT CREATION
    // =========================
    private async Task<Product> CreateProduct(
        BlueprintSuggestion bp,
        BlueprintPrintProvider provider,
        List<Variant> variants,
        List<PrintArea> printAreas)
    {
        var productVariants = variants.Select(v => new CreateProductVariant
        {
            Id = v.Id,
            Price = 2000, // fixed pre-publish placeholder
            IsEnabled = true
        }).ToList();

        return await _printify.CreateProductAsync(_shopId, new CreateProductRequest
        {
            Title = bp.BlueprintTitle,
            BlueprintId = bp.BlueprintId,
            PrintProviderId = provider.Id,
            Variants = productVariants,
            PrintAreas = printAreas
        });
    }

    // =========================
    // DEBUGGING
    // =========================
    private void Log(string reason)
    {
        if (!_stats.ContainsKey(reason))
            _stats[reason] = 0;

        _stats[reason]++;
    }

    private void PrintStats()
    {
        int total = _stats.Values.Sum();
        if(total == 0)
        {
            return;
        }
        Console.WriteLine("\n===== MOCKUP REJECTION SUMMARY =====");

        foreach (var item in _stats.OrderByDescending(x => x.Value))
        {
            Console.WriteLine($"{item.Key,-40} x {item.Value}");
        }

        Console.WriteLine("====================================\n");
    }

    private static string ExtractJson(string text)
    {
        int start = text.IndexOf('[');
        int end = text.LastIndexOf(']');

        return (start >= 0 && end > start)
            ? text.Substring(start, end - start + 1)
            : "[]";
    }

    // =========================
    // IMAGE ANALYSIS
    // =========================
    private class ImageAnalysis
    {
        public double AspectRatio { get; set; }
        public double DetailScore { get; set; }
    }

    private static class ImageAnalyzer
    {
        public static ImageAnalysis Analyze(string path)
        {
            using var img = Image.Load<Rgba32>(path);

            double total = 0;
            int count = 0;

            for (int x = 0; x < img.Width; x += 10)
            for (int y = 0; y < img.Height; y += 10)
            {
                var p = img[x, y];
                total += (p.R + p.G + p.B) / 3.0;
                count++;
            }

            return new ImageAnalysis
            {
                AspectRatio = (double)img.Width / img.Height,
                DetailScore = 0.5
            };
        }
    }
}