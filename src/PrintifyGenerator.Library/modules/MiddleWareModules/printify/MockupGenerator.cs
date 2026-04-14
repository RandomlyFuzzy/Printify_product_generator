using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

/// <summary>
/// Orchestrates the end-to-end mockup creation pipeline:
///
///   1. Resolve or upload the source image to Printify
///      - SHA-256 dedup via src/data/staging/lookup.json
///   2. Download / cache Printify blueprints
///      - Stored in src/data/staged/blueprints/blueprints.json
///   3. Ask a multimodal LLM (Ollama) which blueprint best suits the image
///   4. Create an unpublished draft product on Printify
///   5. Persist the draft record to src/data/staging/drafts/{productId}.json
/// </summary>
public class MockupGenerator
{
    // ── Configuration ──────────────────────────────────────────────

    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNameCaseInsensitive = true,
        WriteIndented = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    private readonly PrintifyClient  _printify;
    private readonly OllamaClient    _ollama;
    private readonly int             _shopId;
    private readonly string          _visionModel;

    // Paths derived from dataBasePath
    private readonly string _lookupFilePath;     // staging/lookup.json
    private readonly string _blueprintCachePath; // staged/blueprints/blueprints.json
    private readonly string _draftsPath;         // staging/drafts/
    private readonly string _blueprintDetailsPath; // Cached/blueprint_details/

    // ── Constructor ────────────────────────────────────────────────

    /// <param name="printify">Authenticated Printify API client.</param>
    /// <param name="ollama">Ollama client pointing at a multimodal model host.</param>
    /// <param name="shopId">Printify shop ID to create drafts under.</param>
    /// <param name="dataBasePath">
    ///   Root for all data files. Defaults to <c>./src/data</c>.
    /// </param>
    /// <param name="visionModel">
    ///   Ollama model name that supports image input (e.g. <c>llava</c>).
    /// </param>
    public MockupGenerator(
        PrintifyClient printify,
        OllamaClient   ollama,
        int            shopId,
        string         dataBasePath = "./src/data",
        string         visionModel  = "llava")
    {
        _printify     = printify;
        _ollama       = ollama;
        _shopId       = shopId;
        _visionModel  = visionModel;

        _lookupFilePath     = Path.Combine(dataBasePath, "staging",  "lookup.json");
        _blueprintCachePath = Path.Combine(dataBasePath, "staged",   "blueprints", "blueprints.json");
        _draftsPath         = Path.Combine(dataBasePath, "staging",  "drafts");
        _blueprintDetailsPath = Path.Combine(dataBasePath, "Cached", "blueprint_details");
    }

    // ── Public API ─────────────────────────────────────────────────

    /// <summary>
    /// Runs the full mockup pipeline for a single image file.
    /// </summary>
    /// <param name="imagePath">Absolute or relative path to a local image file.</param>
    /// <returns>A <see cref="MockupResult"/> describing the outcome.</returns>
    public async IAsyncEnumerable<MockupResult> ProcessImageAsync(string imagePath)
    {
        List<MockupResult> mockupresults = new();
        try
        {
            // 1 ── Resolve / upload image
            var lookup = await ResolveOrUploadImageAsync(imagePath);

            // 2 ── Load / download blueprint catalogue
            var blueprints = await GetOrDownloadBlueprintsAsync();

            // 3 ── Ask LLM which blueprint suits this image best
            var suggestions = await AskLlmForBlueprintAsync(imagePath, blueprints);

            foreach(var suggestion in suggestions)
            {
                Console.WriteLine($"[MockupGenerator] LLM suggestion: blueprint {suggestion.BlueprintId} – {suggestion.BlueprintTitle} (reason: {suggestion.Reason})");
                // 4 ── Fetch first available print provider and its variants
                var providers = await _printify.GetBlueprintPrintProvidersAsync((suggestion.BlueprintId));
                if (providers.Count == 0)
                {
                    continue;
                }

                

                var provider = providers[0];
                var variantResponse = await _printify.GetBlueprintVariantsAsync((suggestion.BlueprintId), provider.Id);
                var variants = variantResponse.Variants;
                // if (variants.Count == 0)
                // {
                //     continue;
                // }
                // //filter variants by price so i can keep cost to a minimum
                // //find the mid price of each sku and keep all variants that are below a certain threshold (e.g. $15)
                // var affordableVariants = new List<Variant>();
                // foreach(var variant in variants)                {
                //     var prices = variant.Prices;
                //     if(prices == null || prices.Count == 0)                    {
                //         continue;
                //     }
                //     var midPrice = prices.Average(p => p.Price);
                //     if(midPrice <= 1500) // $15.00 in cents                    {
                //         affordableVariants.Add(variant);
                // }
                // if(affordableVariants.Count == 0)                {
                //     Console.WriteLine($"[MockupGenerator] No affordable variants found for blueprint {suggestion.BlueprintId} with provider {provider.Id}. Skipping.");
                //     continue;
                // }
                // variants = affordableVariants;


                if(variants.Count > 100)
                {
                    Console.WriteLine($"[MockupGenerator] Warning: blueprint {suggestion.BlueprintId} has {variants.Count} variants; only processing the first 100.");
                    variants = variants.Take(100).ToList();
                }
                // 5 ── Create the draft product on Printify (not published)
                var product = await CreateDraftProductAsync(lookup, suggestion, provider, variants);

                
                // 6 ── Persist draft record for later inspection
                var record = new MockupDraftRecord
                {
                    ProductId               = product.Id,
                    LocalImagePath          = imagePath,
                    PrintifyImageId         = lookup.PrintifyImageId,
                    PrintifyImagePreviewUrl = lookup.PreviewUrl,
                    BlueprintId             = (suggestion.BlueprintId),
                    BlueprintTitle          = suggestion.BlueprintTitle,
                    LlmReason               = suggestion.Reason,
                    PrintProviderId         = provider.Id,
                    PrintProviderTitle      = provider.Title,
                    CreatedAt               = DateTime.UtcNow.ToString("O"),
                    MockupUrls              = product.Images?.Select(i => i.Src).ToList() ?? new List<string>()
                };
                mockupresults.Add(new MockupResult
                {
                    Success         = true,
                    PrintifyImageId = lookup.PrintifyImageId,
                    Draft           = record
                });

                await SaveDraftRecordAsync(record);

                Console.WriteLine($"[MockupGenerator] Done. Draft product ID: {product.Id}");

            }
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"[MockupGenerator] Error: {ex}");
        }
        foreach(var result in mockupresults)
            yield return result;
    }

    // ── Step 1 – Image resolve / upload ───────────────────────────

    private async Task<ImageLookupEntry> ResolveOrUploadImageAsync(string imagePath)
    {
        if (!File.Exists(imagePath))
            throw new FileNotFoundException($"Image file not found: {imagePath}");

        var hash   = await ComputeSha256Async(imagePath);
        var lookup = LoadLookup();

        if (lookup.TryGetValue(hash, out var existing))
        {
            Console.WriteLine($"[MockupGenerator] Image already on Printify (ID: {existing.PrintifyImageId}), skipping upload.");
            return existing;
        }

        Console.WriteLine($"[MockupGenerator] Uploading image: {Path.GetFileName(imagePath)}");
        var uploaded = await _printify.UploadImageFromFileAsync(imagePath);

        var entry = new ImageLookupEntry
        {
            Hash            = hash,
            LocalPath       = imagePath,
            PrintifyImageId = uploaded.Id,
            FileName        = uploaded.FileName,
            PreviewUrl      = uploaded.PreviewUrl,
            UploadedAt      = DateTime.UtcNow.ToString("O")
        };

        lookup[hash] = entry;
        SaveLookup(lookup);

        Console.WriteLine($"[MockupGenerator] Upload complete. Printify ID: {entry.PrintifyImageId}");
        return entry;
    }

    private static async Task<string> ComputeSha256Async(string filePath)
    {
        using var sha    = SHA256.Create();
        await using var stream = File.OpenRead(filePath);
        var hashBytes    = await sha.ComputeHashAsync(stream);
        return Convert.ToHexString(hashBytes).ToLowerInvariant();
    }

    private Dictionary<string, ImageLookupEntry> LoadLookup()
    {
        if (!File.Exists(_lookupFilePath))
            return new Dictionary<string, ImageLookupEntry>();

        try
        {
            var json = File.ReadAllText(_lookupFilePath);
            return JsonSerializer.Deserialize<Dictionary<string, ImageLookupEntry>>(json, JsonOpts)
                   ?? new Dictionary<string, ImageLookupEntry>();
        }
        catch
        {
            return new Dictionary<string, ImageLookupEntry>();
        }
    }

    private void SaveLookup(Dictionary<string, ImageLookupEntry> lookup)
    {
        var dir = Path.GetDirectoryName(_lookupFilePath)!;
        Directory.CreateDirectory(dir);
        File.WriteAllText(_lookupFilePath, JsonSerializer.Serialize(lookup, JsonOpts));
    }

    // ── Step 2 – Blueprint catalogue ──────────────────────────────

    private async Task<List<Blueprint>> GetOrDownloadBlueprintsAsync()
    {
        if (File.Exists(_blueprintCachePath))
        {
            Console.WriteLine("[MockupGenerator] Loading blueprints from cache.");
            try
            {
                var cached = File.ReadAllText(_blueprintCachePath);
                var list   = JsonSerializer.Deserialize<List<Blueprint>>(cached, JsonOpts);
                if (list is { Count: > 0 }) return list;
            }
            catch { /* fall through to download */ }
        }

        Console.WriteLine("[MockupGenerator] Downloading blueprints from Printify API...");
        var blueprints = await _printify.GetBlueprintsAsync();

        var dir = Path.GetDirectoryName(_blueprintCachePath)!;
        Directory.CreateDirectory(dir);
        File.WriteAllText(_blueprintCachePath, JsonSerializer.Serialize(blueprints, JsonOpts));

        Console.WriteLine($"[MockupGenerator] Cached {blueprints.Count} blueprints to {_blueprintCachePath}");
        return blueprints;
    }

    // ── Step 3 – LLM blueprint selection ──────────────────────────

    // Common product-type keywords used to pre-filter the 1000+ blueprint
    // catalogue down to a manageable size for small vision LLMs.
    private static readonly string[] PopularKeywords = new[]
    {
        "t-shirt", "tee", "hoodie", "sweatshirt", "tank top",
        "mug", "poster", "canvas", "tote bag", "phone case",
        "pillow", "blanket", "sticker", "mousepad", "coaster",
        "hat", "cap", "apron", "flag", "towel", "puzzle",
        "notebook", "journal", "backpack", "socks"
    };

    private async Task<List<BlueprintSuggestion>> AskLlmForBlueprintAsync(
        string imagePath, List<Blueprint> blueprints)
    {
        // Pre-filter to popular product types so the prompt stays small
        // enough for lightweight vision models like moondream.

        //TODO implement a more intelligent filtering strategy that considers the image content 
        //an inital filter to check how much can fit onto the print area of each blueprint and filter out those that are too small for the image, then use the popular keywords filter and if there are still too many blueprints then i can use a more advanced strategy like clustering the blueprints by their print area dimensions and picking a few from each cluster to ensure diversity in the catalogue sent to the LLM
        var filtered = blueprints.Select(b => new
        {
            id = b.Id,
            title = b.Title,
        })
        // .OrderBy(b => b.id)
        //randomize 
        .OrderBy(_ => Guid.NewGuid())
        .Take(100)
        .ToList();


        Console.WriteLine($"[MockupGenerator] Sending {filtered.Count} candidate blueprints to LLM (from {blueprints.Count} total).");

        var catalogue = filtered.ToList();

        string catalogueJson = JsonSerializer.Serialize(catalogue, JsonOpts);

        string prompt = $$"""
            You look upon the image and think about which type of product it would best fit on, 
            such as a t-shirt, mug, poster, etc. As you are a creative curator, 
            you also consider the style and composition of the image to determine the most suitable product type.

            And as your immediate assistant, I give you a catalogue of blueprints with their ids and titles, 
            which represent the products you can put the image on.


            Available blueprints name and ids are as following:
            {{catalogueJson}}

            being as profesional and concise as possible, you pick the top 3 best fitting blueprints for this image and explain in one sentence why you picked each one.
            Now that you know what you want to put the image on, as you very well know i need you to tell me in a very concise manner only using the following JSON format:
            [
            {
                "id": "<integer>", 
                "title": "<string>", 
                "reason": "<one sentence>"
            },
            ... 
            ]
            """;


        // Console.WriteLine($"[MockupGenerator] Prompting LLM with image and catalogue... {prompt}");
        Console.WriteLine($"[MockupGenerator] Querying LLM ({_visionModel}) for blueprint recommendation with image {imagePath}...");
        
        string rawResponse ="";

        //it keeps saying that their is no image input even tho there is, so maybe i can just send the image in the prompt and not as an input and see if it can understand that
        await foreach(var data in _ollama.GenerateWithImageStreamAsync(_visionModel, prompt, imagePath))
        {
            Console.Write($"{data}");
            rawResponse += data;
        }
        rawResponse = rawResponse.Trim().Substring(rawResponse.IndexOf('['));
        rawResponse = rawResponse.Substring(0, rawResponse.LastIndexOf(']') + 1);

        Console.WriteLine($"[MockupGenerator] Raw LLM response: {rawResponse}");


        try
        {
            var suggestionArray = JsonSerializer.Deserialize<List<BlueprintSuggestion>>(rawResponse, JsonOpts);
            Console.WriteLine($"[MockupGenerator] Parsed LLM suggestions: {JsonSerializer.Serialize(suggestionArray, JsonOpts)}");
            return suggestionArray ?? new List<BlueprintSuggestion>();
            
        }
        catch (JsonException ex)
        {
            Console.Error.WriteLine($"[MockupGenerator] Failed to parse LLM response as JSON: {ex}");
        }

        // Fallback: first blueprint in catalogue
        var fallback = blueprints[0];
        Console.WriteLine($"[MockupGenerator] LLM response could not be parsed; falling back to blueprint {fallback.Id}: {fallback.Title}");
        return new List<BlueprintSuggestion>
        {
            new BlueprintSuggestion
            {
                BlueprintId    = fallback.Id,
                BlueprintTitle = fallback.Title,
                Reason         = ""
            }
        };
    }

    /// <summary>Extracts the first balanced {...} block from a string.</summary>
    private static string ExtractFirstJsonObject(string text)
    {
        int start = text.IndexOf('{');
        int end   = text.IndexOf('}', start + 1);
        if (start >= 0 && end > start)
            return text[start..(end + 1)];
        return text;
    }

    /// <summary>
    /// Builds a <see cref="BlueprintCatalogueEntry"/> for a blueprint,
    /// enriching it with print-location dimensions from the cached
    /// blueprint_details JSON files when available.
    /// </summary>
    private BlueprintCatalogueEntry BuildCatalogueEntry(Blueprint blueprint)
    {
        var entry = new BlueprintCatalogueEntry
        {
            Id    = blueprint.Id,
            Title = blueprint.Title
        };

        var detailFile = Path.Combine(_blueprintDetailsPath, $"{blueprint.Id}.json");
        if (!File.Exists(detailFile))
            return entry;

        try
        {
            var json   = File.ReadAllText(detailFile);
            var doc    = JsonDocument.Parse(json);
            var root   = doc.RootElement;

            // Collect unique positions and their max dimensions across all
            // providers / variants so the LLM sees the full print surface.
            var locations = new Dictionary<string, PrintLocationSize>(StringComparer.OrdinalIgnoreCase);

            if (root.TryGetProperty("print_providers", out var providers))
            {
                foreach (var pp in providers.EnumerateArray())
                {
                    if (!pp.TryGetProperty("variants", out var variantsWrapper))
                        continue;
                    if (!variantsWrapper.TryGetProperty("variants", out var variants))
                        continue;

                    foreach (var v in variants.EnumerateArray())
                    {
                        if (!v.TryGetProperty("placeholders", out var placeholders))
                            continue;

                        foreach (var ph in placeholders.EnumerateArray())
                        {
                            var position = ph.GetProperty("position").GetString() ?? "";
                            var width    = ph.TryGetProperty("width",  out var w) ? w.GetInt32() : 0;
                            var height   = ph.TryGetProperty("height", out var h) ? h.GetInt32() : 0;

                            if (locations.TryGetValue(position, out var existing))
                            {
                                if (width  > existing.SizeX) existing.SizeX = width;
                                if (height > existing.SizeY) existing.SizeY = height;
                            }
                            else
                            {
                                locations[position] = new PrintLocationSize { SizeX = width, SizeY = height };
                            }
                        }

                        // One variant per provider is enough to capture the positions.
                        break;
                    }
                }
            }

            entry.PrintLocations = locations.Keys.ToList();
            entry.Locations      = locations;
        }
        catch
        {
            // Cached file is corrupt or unexpected format – return basic entry.
        }

        return entry;
    }

    // ── Step 4+5 – Draft product creation ─────────────────────────

    private async Task<Product> CreateDraftProductAsync(
        ImageLookupEntry     image,
        BlueprintSuggestion  suggestion,
        BlueprintPrintProvider provider,
        List<Variant>        variants)
    {
        var variantIds = variants.Select(v => v.Id).ToList();

        // Place the design on the "front" position of every variant
        var printAreaImage = new PrintAreaImage
        {
            Id    = image.PrintifyImageId,
            X     = 0.5,
            Y     = 0.5,
            Scale = 1.0,
            Angle = 0.0,
            Width  = 1.0,
            Height = 1.0
        };

        var printArea = new PrintArea
        {
            VariantIds   = variantIds,
            Placeholders = new List<PrintAreaPlaceholder>
            {
                new()
                {
                    Position = "front",
                    Images   = new List<PrintAreaImage> { printAreaImage }
                }
            }
        };
        //get variant price to produce 

        var productVariants = variants.Select(v => new CreateProductVariant
        {
            Id        = v.Id,
            Price     = 2000,  // $20.00 (Printify stores prices in cents)
            IsEnabled = true
        }).ToList();

        var request = new CreateProductRequest
        {
            Title           = $"Design – {suggestion.BlueprintTitle}",
            Description     = $"AI-generated design. Blueprint: {suggestion.BlueprintTitle}. {suggestion.Reason}",
            BlueprintId     = suggestion.BlueprintId,
            PrintProviderId = provider.Id,
            Variants        = productVariants,
            PrintAreas      = new List<PrintArea> { printArea }
        };

        Console.WriteLine($"[MockupGenerator] Creating draft product (blueprint {suggestion.BlueprintId}, provider {provider.Id}, {productVariants.Count} variants)...");
        return await _printify.CreateProductAsync(_shopId, request);
    }

    // ── Step 6 – Persist draft record ─────────────────────────────

    private async Task SaveDraftRecordAsync(MockupDraftRecord record)
    {
        Directory.CreateDirectory(_draftsPath);
        string filePath = Path.Combine(_draftsPath, $"{record.ProductId}.json");
        await File.WriteAllTextAsync(filePath, JsonSerializer.Serialize(record, JsonOpts));
        Console.WriteLine($"[MockupGenerator] Draft record saved → {filePath}");
    }
}
