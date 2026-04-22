using System;
using System.Collections.Generic;
using System.Globalization;
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
    private readonly string          _dataBasePath;

    // Paths derived from dataBasePath
    private readonly string _lookupFilePath;     // staging/lookup.json
    private readonly string _blueprintCachePath; // staged/blueprints/blueprints.json
    private readonly string _draftsPath;         // staging/drafts/
    private readonly string _logoImagePath;      // staging/Logo/Logo.png
    private readonly string _blueprintDetailsPath; // Cached/blueprint_details/
    private readonly PrintifyBlueprintQueryApi _blueprintQueryApi;

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
        _dataBasePath = dataBasePath;

        _lookupFilePath     = Path.Combine(dataBasePath, "staging",  "lookup.json");
        _blueprintCachePath = Path.Combine(dataBasePath, "staged",   "blueprints", "blueprints.json");
        _draftsPath         = Path.Combine(dataBasePath, "staging",  "drafts");
        _logoImagePath      = Path.Combine(dataBasePath, "staging",  "Logo", "Logo.png");
        _blueprintDetailsPath = Path.Combine(dataBasePath, "Cached", "blueprint_details");
        _blueprintQueryApi  = PrintifyBlueprintDatabase.CreateQueryApi(_blueprintDetailsPath);
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
            var jobId = TryExtractJobId(imagePath);

            // 1 ── Resolve / upload image
            var lookup = await ResolveOrUploadImageAsync(imagePath);
            var p1path = Path.GetDirectoryName(imagePath)+"/phase_1.json";
            var imageInformation = JsonSerializer.Deserialize<Prompt>(File.ReadAllText(p1path), JsonOpts)
                ?? throw new InvalidDataException($"Prompt metadata file '{p1path}' could not be deserialized.");
            var logoLookup = await ResolveOrUploadImageAsync(_logoImagePath);

            // 2 ── Load / download blueprint catalogue
            var blueprints = await GetOrDownloadBlueprintsAsync();

            float max_tolerance = 0.20f; // 20% aspect ratio tolerance for blueprint suitability

            //filter blueprints by those that can fit within a ceratin aspect ratio.
            blueprints = blueprints.Where(b => 
            {
                var entry = BuildCatalogueEntry(b);
                if (entry.Locations is null || entry.Locations.Count == 0)
                    return false;

                // Check if any print location can accommodate the image's aspect ratio within a reasonable tolerance
                var imageAspectRatio = (double)imageInformation.width / imageInformation.height;
                foreach (var location in entry.Locations)
                {
                    var size = location.Value;
                    if (size.SizeX == 0 || size.SizeY == 0)
                        continue;

                    var locationAspectRatio = (double)size.SizeX / size.SizeY;
                    if (Math.Abs(locationAspectRatio - imageAspectRatio) / imageAspectRatio <= max_tolerance) // 20% tolerance
                        return true;
                }

                return false;
            }).ToList();



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
                var variants = variantResponse.Variants
                    .Where(variant => variant.Placeholders is { Count: > 0 })
                    .ToList();
                if (variants.Count == 0)
                {
                    Console.WriteLine($"[MockupGenerator] No printable variants found for blueprint {suggestion.BlueprintId} with provider {provider.Id}. Skipping.");
                    continue;
                }

                variants = FilterVariantsByPreferredColors(suggestion.BlueprintId, provider.Id, variants);
                if (variants.Count == 0)
                {
                    Console.WriteLine($"[MockupGenerator] No black or white color variants found for blueprint {suggestion.BlueprintId} with provider {provider.Id}. Skipping.");
                    continue;
                }

                float ratio = 0.05f;
                string bptitle = suggestion.BlueprintTitle.ToLower();

                if(bptitle.Contains("phone case"))
                {
                    ratio = 0.15f; // Phone cases often have very different aspect ratios, so be more lenient
                }else if(bptitle.Contains("poster") || bptitle.Contains("canvas"))
                {
                    ratio = 0.05f; // Posters and canvases can also have a wider range of aspect ratios
                }
                else if(bptitle.Contains("mug"))
                {
                    ratio = 0.10f; // Mugs can be a bit tricky with aspect ratios, so allow some flexibility
                }
                else if(bptitle.Contains("t-shirt") || bptitle.Contains("hoodie") || bptitle.Contains("sweatshirt") || bptitle.Contains("tank top") || bptitle.Contains("tee"))
                {
                    ratio = 1f; // Apparel generally should be close to the image aspect ratio to look good
                }else if(bptitle.Contains("hat") || bptitle.Contains("cap"))
                {
                    ratio = 0.20f; // Hats can vary widely in print area aspect ratio, so be more lenient
                }else if(bptitle.Contains("tote bag") || bptitle.Contains("backpack"))
                {
                    ratio = 0.10f; // Bags can also have a wider range of aspect ratios
                }else if(bptitle.Contains("square"))
                {
                    ratio = 0.01f; 
                }
                


                //i want to only keep varients that aspect ratio is within 5% of the aspect ratio of the image to ensure that the design fits well on the product and doesn't get stretched or squished in an unappealing way, to do this i can calculate the aspect ratio of the image and each variant's print area and filter out those that are too far off from the image's aspect ratio
                var imageAspectRatio = (double)imageInformation.width / imageInformation.height;
                variants = variants.Where(variant => 
                {
                    var primaryPlaceholder = variant.Placeholders?.FirstOrDefault();
                    if (primaryPlaceholder is null || primaryPlaceholder.Width == 0 || primaryPlaceholder.Height == 0)
                    {
                        return false;
                    }

                    var variantAspectRatio = (double)primaryPlaceholder.Width / primaryPlaceholder.Height;
                    return Math.Abs(variantAspectRatio - imageAspectRatio) / imageAspectRatio <= ratio;
                }).ToList();

                if(variants.Count == 0)
                {
                    Console.WriteLine($"[MockupGenerator] No variants with suitable aspect ratio found for blueprint {suggestion.BlueprintId}. Skipping.");
                    continue;
                }

                if(variants.Count > 100)
                {
                    Console.WriteLine($"[MockupGenerator] Warning: blueprint {suggestion.BlueprintId} has {variants.Count} variants; only processing the first 100.");
                    variants = variants.Take(100).ToList();
                }

                var listingContent = ListingContentBuilder.Build(new ListingContentContext
                {
                    JobId = jobId ?? string.Empty,
                    ImagePath = imagePath,
                    BlueprintId = suggestion.BlueprintId,
                    BlueprintTitle = suggestion.BlueprintTitle,
                    PrintProviderId = provider.Id,
                    PrintProviderTitle = provider.Title,
                    LlmReason = suggestion.Reason
                });

                // 5 ── Create the draft product on Printify (not published)
                var product = await CreateDraftProductAsync(lookup, logoLookup, suggestion, provider, variants, listingContent);

                
                // 6 ── Persist draft record for later inspection
                var record = new MockupDraftRecord
                {
                    ProductId               = product.Id,
                    JobId                   = jobId ?? string.Empty,
                    LookupKey               = listingContent.Lookup.LookupKey,
                    GroupKey                = listingContent.Lookup.GroupKey,
                    AssetKey                = listingContent.Lookup.AssetKey,
                    ReferenceCode           = listingContent.Lookup.ReferenceCode,
                    LocalImagePath          = imagePath,
                    PrintifyImageId         = lookup.PrintifyImageId,
                    PrintifyImagePreviewUrl = lookup.PreviewUrl,
                    BlueprintId             = (suggestion.BlueprintId),
                    BlueprintTitle          = suggestion.BlueprintTitle,
                    LlmReason               = suggestion.Reason,
                    PrintProviderId         = provider.Id,
                    PrintProviderTitle      = provider.Title,
                    LookupTags              = new List<string>(listingContent.Lookup.Tags),
                    ChannelContent          = new Dictionary<string, ListingChannelContent>(listingContent.Channels, StringComparer.OrdinalIgnoreCase),
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

            being as profesional and concise as possible, you pick the top 5 best fitting blueprints for this image and explain in one sentence why you picked each one.
            Also exclude that include the word "Glass" in the title as the image is not suitable for glass products. 
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

    private List<Variant> FilterVariantsByPreferredColors(int blueprintId, int providerId, List<Variant> variants)
    {
        var optionsByVariantId = _blueprintQueryApi
            .GetSubvariants(blueprintId, providerId)
            .GroupBy(subvariant => subvariant.VariantId)
            .ToDictionary(group => group.Key, group => group.First().Options);

        var variantsWithColor = variants
            .Select(variant => new
            {
                Variant = variant,
                Color = ResolveVariantColor(variant, optionsByVariantId)
            })
            .ToList();

        if (!variantsWithColor.Any(item => !string.IsNullOrWhiteSpace(item.Color)))
        {
            return variants;
        }

        var filteredVariants = variantsWithColor
            .Where(item => IsPreferredVariantColor(item.Color))
            .Select(item => item.Variant)
            .ToList();

        if (filteredVariants.Count > 0 && filteredVariants.Count != variants.Count)
        {
            Console.WriteLine($"[MockupGenerator] Reduced variants to {filteredVariants.Count} black/white color options for blueprint {blueprintId} with provider {providerId}.");
        }

        return filteredVariants;
    }

    private static string? ResolveVariantColor(
        Variant variant,
        IReadOnlyDictionary<int, IReadOnlyDictionary<string, string>> optionsByVariantId)
    {
        if (optionsByVariantId.TryGetValue(variant.Id, out var optionMap))
        {
            var cachedColor = TryGetVariantColor(optionMap);
            if (!string.IsNullOrWhiteSpace(cachedColor))
            {
                return cachedColor;
            }
        }

        return TryGetVariantColor(NormalizeVariantOptions(variant.Options));
    }

    private static IReadOnlyDictionary<string, string> NormalizeVariantOptions(Dictionary<string, object>? options)
    {
        if (options == null || options.Count == 0)
        {
            return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        }

        var normalized = new Dictionary<string, string>(options.Count, StringComparer.OrdinalIgnoreCase);

        foreach (var option in options)
        {
            normalized[option.Key] = option.Value switch
            {
                null => string.Empty,
                JsonElement element when element.ValueKind == JsonValueKind.Null || element.ValueKind == JsonValueKind.Undefined => string.Empty,
                JsonElement element when element.ValueKind == JsonValueKind.String => element.GetString() ?? string.Empty,
                JsonElement element => element.ToString(),
                _ => option.Value.ToString() ?? string.Empty
            };
        }

        return normalized;
    }

    private static string? TryGetVariantColor(IReadOnlyDictionary<string, string> options)
    {
        if (options.TryGetValue("color", out var color) && !string.IsNullOrWhiteSpace(color))
        {
            return color.Trim();
        }

        if (options.TryGetValue("colour", out var colour) && !string.IsNullOrWhiteSpace(colour))
        {
            return colour.Trim();
        }

        return null;
    }

    private static bool IsPreferredVariantColor(string? color)
    {
        if (string.IsNullOrWhiteSpace(color))
        {
            return false;
        }

        return color.Contains("white", StringComparison.OrdinalIgnoreCase)
            || color.Contains("black", StringComparison.OrdinalIgnoreCase);
    }

    private static string BuildPlaceholderSignature(IReadOnlyList<VariantPlaceholder> placeholders)
    {
        return string.Join(
            "||",
            placeholders
                .OrderBy(placeholder => placeholder.Position, StringComparer.OrdinalIgnoreCase)
                .ThenBy(placeholder => placeholder.DecorationMethod, StringComparer.OrdinalIgnoreCase)
                .ThenBy(placeholder => placeholder.Width)
                .ThenBy(placeholder => placeholder.Height)
                .Select(placeholder =>
                    $"{placeholder.Position.Trim()}|{(placeholder.DecorationMethod ?? string.Empty).Trim()}|{placeholder.Width.ToString(CultureInfo.InvariantCulture)}|{placeholder.Height.ToString(CultureInfo.InvariantCulture)}"));
    }

    private static IReadOnlyList<VariantPlaceholder> SelectDraftPlaceholders(IReadOnlyList<VariantPlaceholder> placeholders)
    {
        if (placeholders.Count == 0)
        {
            return Array.Empty<VariantPlaceholder>();
        }

        var selectedPlaceholders = new List<VariantPlaceholder>
        {
            placeholders[0]
        };

        var neckPlaceholder = placeholders.FirstOrDefault(placeholder =>
            placeholder.Position.Contains("neck", StringComparison.OrdinalIgnoreCase));

        if (neckPlaceholder is not null &&
            !EqualityComparer<VariantPlaceholder>.Default.Equals(neckPlaceholder, selectedPlaceholders[0]))
        {
            selectedPlaceholders.Add(neckPlaceholder);
        }

        return selectedPlaceholders;
    }

    private static PrintAreaImage CreateCenteredPrintAreaImage(string imageId)
    {
        return new PrintAreaImage
        {
            Id     = imageId,
            X      = 0.5,
            Y      = 0.5,
            Scale  = 1.0,
            Angle  = 0.0,
            Width  = 1.0,
            Height = 1.0
        };
    }

    private static string ResolveImageId(ImageLookupEntry image, string imageRole)
    {
        if (string.IsNullOrWhiteSpace(image.PrintifyImageId))
        {
            throw new InvalidOperationException($"{imageRole} image did not resolve to a Printify image ID.");
        }

        return image.PrintifyImageId;
    }

    private List<PrintArea> BuildDraftPrintAreas(
        IReadOnlyList<Variant> variants,
        string mainImageId,
        string logoImageId)
    {
        return variants
            .GroupBy(
                variant => BuildPlaceholderSignature(variant.Placeholders ?? new List<VariantPlaceholder>()),
                StringComparer.Ordinal)
            .Select(group =>
            {
                var exemplar = group.First();
                var selectedPlaceholders = SelectDraftPlaceholders(exemplar.Placeholders ?? new List<VariantPlaceholder>());
                var mainPlaceholder = selectedPlaceholders[0];

                return new PrintArea
                {
                    VariantIds = group
                        .Select(variant => variant.Id)
                        .Distinct()
                        .ToList(),
                    Placeholders = selectedPlaceholders
                        .Select(placeholder => new PrintAreaPlaceholder
                        {
                            Position = placeholder.Position,
                            DecorationMethod = string.IsNullOrWhiteSpace(placeholder.DecorationMethod)
                                ? null
                                : placeholder.DecorationMethod,
                            Images = new List<PrintAreaImage>
                            {
                                CreateCenteredPrintAreaImage(EqualityComparer<VariantPlaceholder>.Default.Equals(placeholder, mainPlaceholder)
                                    ? mainImageId
                                    : logoImageId)
                            }
                        })
                        .ToList()
                };
            })
            .ToList();
    }

    // ── Step 4+5 – Draft product creation ─────────────────────────

    private async Task<Product> CreateDraftProductAsync(
        ImageLookupEntry     image,
        ImageLookupEntry     logoImage,
        BlueprintSuggestion  suggestion,
        BlueprintPrintProvider provider,
        List<Variant>        variants,
        ListingContentBundle listingContent)
    {
        var mainImageId = ResolveImageId(image, "Main");
        var logoImageId = ResolveImageId(logoImage, "Logo");
        var printAreas = BuildDraftPrintAreas(variants, mainImageId, logoImageId);
        var printifyContent = ListingContentBuilder.ResolveChannel(listingContent, "printify");
        //get variant price to produce 

        var productVariants = variants.Select(v => new CreateProductVariant
        {
            Id        = v.Id,
            Price     = (int)(new Currency(CurrencyCode.GBP, 20.00m).ConvertTo(CurrencyCode.USD).GetAwaiter().GetResult().Amount*100),  // $20.00 (Printify stores prices in cents)
            IsEnabled = true
        }).ToList();
        var request = new CreateProductRequest
        {
            Title           = printifyContent.Title,
            Description     = printifyContent.Description,
            BlueprintId     = suggestion.BlueprintId,
            PrintProviderId = provider.Id,
            Tags            = printifyContent.Tags,
            Variants        = productVariants,
            PrintAreas      = printAreas
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

        if (!string.IsNullOrWhiteSpace(record.JobId))
            UploadedJobProductsStore.TrackUpload(_dataBasePath, record.JobId, record.ProductId);

        Console.WriteLine($"[MockupGenerator] Draft record saved → {filePath}");
    }

    private static string? TryExtractJobId(string imagePath)
    {
        var normalizedImagePath = Path.GetFullPath(imagePath);
        var directoryPath = Path.GetDirectoryName(normalizedImagePath);
        if (string.IsNullOrWhiteSpace(directoryPath))
            return null;

        var folderName = Path.GetFileName(directoryPath);
        if (string.IsNullOrWhiteSpace(folderName))
            return null;

        var fileNameWithoutExtension = Path.GetFileNameWithoutExtension(normalizedImagePath);
        if (string.Equals(folderName, fileNameWithoutExtension, StringComparison.OrdinalIgnoreCase))
            return folderName;

        if (File.Exists(Path.Combine(directoryPath, "phase_1.json"))
            || File.Exists(Path.Combine(directoryPath, "phase_3.json")))
        {
            return folderName;
        }

        return null;
    }
}
