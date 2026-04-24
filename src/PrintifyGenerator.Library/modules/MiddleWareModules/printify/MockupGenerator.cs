using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

public class MockupGenerator
{
    private readonly PrintifyClient _printify;
    private readonly OllamaClient _ollama;
    private readonly int _shopId;

    public MockupGenerator(PrintifyClient printify, OllamaClient ollama, int shopId)
    {
        _printify = printify;
        _ollama = ollama;
        _shopId = shopId;
    }

    // =========================
    // ENTRY POINT
    // =========================
    public async Task<List<MockupResult>> ProcessImageAsync(string imagePath)
    {
        var results = new List<MockupResult>();

        var analysis = ImageAnalyzer.Analyze(imagePath);

        var mainImage = await _printify.UploadImageFromFileAsync(imagePath);
        var blueprints = await _printify.GetBlueprintsAsync();

        var selectedBlueprints = await SelectBlueprints(imagePath, blueprints);

        foreach (var bp in selectedBlueprints)
        {
            var providers = await _printify.GetBlueprintPrintProvidersAsync(bp.BlueprintId);
            if (!providers.Any()) continue;

            var provider = providers.First();

            var variantResponse = await _printify.GetBlueprintVariantsAsync(bp.BlueprintId, provider.Id);
            var variants = BuildScoredVariants(variantResponse.Variants, analysis);

            if (!variants.Any()) continue;

            var printAreas = BuildPrintAreas(variants, mainImage.Id, analysis);

            var product = await CreateProduct(bp, provider, variants, printAreas);

            results.Add(new MockupResult
            {
                Success = true,
                PrintifyImageId = mainImage.Id,
                Draft = new MockupDraftRecord
                {
                    ProductId = product.Id,
                    BlueprintId = bp.BlueprintId,
                    BlueprintTitle = bp.BlueprintTitle
                }
            });
        }

        return results;
    }

    // =========================
    // IMAGE ANALYSIS
    // =========================
    private class ImageAnalysis
    {
        public double AspectRatio { get; set; }
        public bool IsDark { get; set; }
        public double DetailScore { get; set; }
    }

    private static class ImageAnalyzer
    {
        public static ImageAnalysis Analyze(string path)
        {
            using var image = Image.Load<Rgba32>(path);

            double brightness = 0;
            double variance = 0;
            int count = 0;

            int step = Math.Max(1, image.Width / 100);

            for (int x = 0; x < image.Width; x += step)
            for (int y = 0; y < image.Height; y += step)
            {
                var p = image[x, y];
                double b = (p.R + p.G + p.B) / 3.0;
                brightness += b;
                count++;
            }

            double avg = brightness / count;

            for (int x = 0; x < image.Width; x += step)
            for (int y = 0; y < image.Height; y += step)
            {
                var p = image[x, y];
                double b = (p.R + p.G + p.B) / 3.0;
                variance += Math.Pow(b - avg, 2);
            }

            return new ImageAnalysis
            {
                AspectRatio = (double)image.Width / image.Height,
                IsDark = avg < 128,
                DetailScore = Math.Min(1.0, variance / 5000.0)
            };
        }
    }

    // =========================
    // BLUEPRINT SELECTION
    // =========================
    private async Task<List<BlueprintSuggestion>> SelectBlueprints(string imagePath, List<Blueprint> blueprints)
    {
        var sample = blueprints.OrderBy(_ => Guid.NewGuid()).Take(30).ToList();

        string prompt = "Pick best 3 products for this image.";

        string response = await _ollama.GenerateAsync("llava", prompt, imagePath);

        try
        {
            return System.Text.Json.JsonSerializer.Deserialize<List<BlueprintSuggestion>>(response)
                   ?? new List<BlueprintSuggestion>();
        }
        catch
        {
            return sample.Take(3).Select(b => new BlueprintSuggestion
            {
                BlueprintId = b.Id,
                BlueprintTitle = b.Title
            }).ToList();
        }
    }

    // =========================
    // VARIANT SCORING
    // =========================
    private List<Variant> BuildScoredVariants(List<Variant> variants, ImageAnalysis analysis)
    {
        return variants
            .Where(v => v.Placeholders != null && v.Placeholders.Count > 0)
            .Select(v => new
            {
                Variant = v,
                Score = ScoreVariant(v, analysis)
            })
            .OrderByDescending(x => x.Score)
            .Take(20)
            .Select(x => x.Variant)
            .ToList();
    }

    private double ScoreVariant(Variant v, ImageAnalysis analysis)
    {
        double score = 0;

        var ph = v.Placeholders.First();

        // Bigger print area = better
        score += (ph.Width * ph.Height) / 1_000_000.0;

        // Dark/light contrast
        if (analysis.IsDark)
            score += 1;
        else
            score += 1;

        // Penalize small areas for detailed images
        if (analysis.DetailScore > 0.7 && ph.Width < 2000)
            score -= 2;

        return score;
    }

    // =========================
    // PRINT AREAS
    // =========================
    private List<PrintArea> BuildPrintAreas(List<Variant> variants, string imageId, ImageAnalysis analysis)
    {
        return variants
            .GroupBy(v => v.Placeholders.First().Position)
            .Select(group =>
            {
                var ph = group.First().Placeholders.First();

                return new PrintArea
                {
                    VariantIds = group.Select(v => v.Id).ToList(),
                    Placeholders = new List<PrintAreaPlaceholder>
                    {
                        new PrintAreaPlaceholder
                        {
                            Position = ph.Position,
                            Images = new List<PrintAreaImage>
                            {
                                CreateSmartImage(imageId, analysis, ph)
                            }
                        }
                    }
                };
            })
            .ToList();
    }

    private PrintAreaImage CreateSmartImage(string id, ImageAnalysis analysis, VariantPlaceholder ph)
    {
        double scale = 0.85;

        if (analysis.DetailScore > 0.7)
            scale = 0.7;

        var ratio = (double)ph.Width / ph.Height;
        var diff = Math.Abs(ratio - analysis.AspectRatio) / analysis.AspectRatio;

        if (diff > 0.25)
            scale *= 0.9;

        return new PrintAreaImage
        {
            Id = id,
            X = 0.5,
            Y = 0.5,
            Scale = scale,
            Width = 1,
            Height = 1
        };
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
            Price = CalculatePrice(v),
            IsEnabled = true
        }).ToList();

        return await _printify.CreateProductAsync(_shopId, new CreateProductRequest
        {
            Title = bp.BlueprintTitle,
            Description = "Auto-generated design",
            BlueprintId = bp.BlueprintId,
            PrintProviderId = provider.Id,
            Variants = productVariants,
            PrintAreas = printAreas
        });
    }

    private int CalculatePrice(Variant v)
    {
        double multiplier = v.Price < 1000 ? 2.5 : 2.2;
        return (int)(v.Price * multiplier);
    }
}