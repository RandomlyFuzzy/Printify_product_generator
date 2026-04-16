using System.Collections.Concurrent;
using System.Collections.ObjectModel;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;

public static class PrintifyBlueprintDatabase
{
    private static readonly Lazy<PrintifyBlueprintQueryApi> _defaultApi = new(() => new PrintifyBlueprintQueryApi());

    public static PrintifyBlueprintQueryApi Default => _defaultApi.Value;

    public static PrintifyBlueprintQueryApi CreateQueryApi(string? blueprintDetailsDirectory = null)
    {
        return new PrintifyBlueprintQueryApi(blueprintDetailsDirectory);
    }

    public static PrintifyCachedBlueprintDetail GetBlueprintDetail(int blueprintId)
    {
        return Default.GetBlueprintDetail(blueprintId);
    }

    public static bool TryGetBlueprintDetail(int blueprintId, out PrintifyCachedBlueprintDetail detail)
    {
        return Default.TryGetBlueprintDetail(blueprintId, out detail);
    }

    public static Blueprint GetBlueprint(int blueprintId)
    {
        return GetBlueprintDetail(blueprintId).Blueprint;
    }

    public static IEnumerable<PrintifyCachedBlueprintDetail> GetAllBlueprintDetails()
    {
        return Default.GetAllBlueprintDetails();
    }

    public static IEnumerable<Blueprint> GetAllBlueprints()
    {
        return Default.GetAllBlueprints();
    }

    public static IEnumerable<PrintifyCachedBlueprintProviderDetail> GetProviders(int blueprintId)
    {
        return Default.GetProviders(blueprintId);
    }

    public static IEnumerable<Variant> GetVariants(int blueprintId, int? providerId = null)
    {
        return Default.GetVariants(blueprintId, providerId);
    }

    public static IEnumerable<PrintifyBlueprintSubvariant> GetSubvariants(int blueprintId, int? providerId = null)
    {
        return Default.GetSubvariants(blueprintId, providerId);
    }

    public static IEnumerable<PrintifyBlueprintShippingQuote> GetShippingQuotes(int blueprintId, int? providerId = null)
    {
        return Default.GetShippingQuotes(blueprintId, providerId);
    }

    public static Task<IReadOnlyList<PrintifyBlueprintSubvariant>> GetSubvariantsWithLivePricingAsync(
        PrintifyClient client,
        int blueprintId,
        int? providerId = null,
        bool showOutOfStock = true)
    {
        return Default.GetSubvariantsWithLivePricingAsync(client, blueprintId, providerId, showOutOfStock);
    }

    public static Task<IReadOnlyList<PrintifyBlueprintShippingQuote>> GetShippingQuotesWithLivePricingAsync(
        PrintifyClient client,
        int blueprintId,
        int? providerId = null,
        bool showOutOfStock = true)
    {
        return Default.GetShippingQuotesWithLivePricingAsync(client, blueprintId, providerId, showOutOfStock);
    }

    public static string? GetVariantImageUrl(int blueprintId, int providerId, int variantId)
    {
        return Default.GetVariantImageUrl(blueprintId, providerId, variantId);
    }

    public static bool TryGetVariantImageUrl(int blueprintId, int providerId, int variantId, out string imageUrl)
    {
        return Default.TryGetVariantImageUrl(blueprintId, providerId, variantId, out imageUrl);
    }

    public static string? GetProviderExampleImageUrl(int blueprintId, int providerId)
    {
        return Default.GetProviderExampleImageUrl(blueprintId, providerId);
    }

    public static bool TryGetProviderExampleImageUrl(int blueprintId, int providerId, out string imageUrl)
    {
        return Default.TryGetProviderExampleImageUrl(blueprintId, providerId, out imageUrl);
    }
}

public sealed class PrintifyBlueprintQueryApi
{
    private const string RelativeBlueprintDetailsPath = "src/data/Cached/blueprint_details";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true
    };

    private static readonly IReadOnlyDictionary<string, string> EmptyOptions =
        new ReadOnlyDictionary<string, string>(new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase));

    private readonly ConcurrentDictionary<int, PrintifyCachedBlueprintDetail> _cache = new();
    private readonly IReadOnlyDictionary<int, string> _blueprintFiles;
    private readonly ConcurrentDictionary<VariantPricingCacheKey, IReadOnlyDictionary<int, ProductVariant>> _variantPricingLookupByProvider = new();
    private readonly ConcurrentDictionary<ProviderImageCacheKey, ProviderImageCache> _providerImageCacheByProvider = new();
    private readonly Lazy<IReadOnlyDictionary<VariantImageCacheKey, PrintifyVariantImageCacheEntry>> _variantImageLookup;

    public PrintifyBlueprintQueryApi(string? blueprintDetailsDirectory = null)
    {
        BlueprintDetailsDirectory = ResolveBlueprintDetailsDirectory(blueprintDetailsDirectory);
        VariantPricingCacheDirectory = Path.Combine(Path.GetDirectoryName(BlueprintDetailsDirectory) ?? BlueprintDetailsDirectory, "variants", "prices");
        VariantImageCacheDirectory = Path.Combine(Path.GetDirectoryName(BlueprintDetailsDirectory) ?? BlueprintDetailsDirectory, "variants", "images");
        VariantImageLookupPath = Path.Combine(Path.GetDirectoryName(BlueprintDetailsDirectory) ?? BlueprintDetailsDirectory, "variant_images.json");
        _blueprintFiles = LoadFileIndex(BlueprintDetailsDirectory);
        _variantImageLookup = new Lazy<IReadOnlyDictionary<VariantImageCacheKey, PrintifyVariantImageCacheEntry>>(LoadVariantImageLookup);
    }

    public string BlueprintDetailsDirectory { get; }
    public string VariantPricingCacheDirectory { get; }
    public string VariantImageCacheDirectory { get; }
    public string VariantImageLookupPath { get; }

    public IEnumerable<int> BlueprintIds => _blueprintFiles.Keys.OrderBy(id => id);

    public IEnumerable<PrintifyCachedBlueprintDetail> GetAllBlueprintDetails()
    {
        return BlueprintIds.Select(GetBlueprintDetail);
    }

    public IEnumerable<Blueprint> GetAllBlueprints()
    {
        return GetAllBlueprintDetails().Select(detail => detail.Blueprint);
    }

    public PrintifyCachedBlueprintDetail GetBlueprintDetail(int blueprintId)
    {
        if (!_blueprintFiles.ContainsKey(blueprintId))
        {
            throw new KeyNotFoundException($"Blueprint detail with ID {blueprintId} was not found in '{BlueprintDetailsDirectory}'.");
        }

        return _cache.GetOrAdd(blueprintId, LoadBlueprintDetail);
    }

    public bool TryGetBlueprintDetail(int blueprintId, out PrintifyCachedBlueprintDetail detail)
    {
        if (!_blueprintFiles.ContainsKey(blueprintId))
        {
            detail = default!;
            return false;
        }

        detail = _cache.GetOrAdd(blueprintId, LoadBlueprintDetail);
        return true;
    }

    public string? GetVariantImageUrl(int blueprintId, int providerId, int variantId)
    {
        return TryGetVariantImageUrl(blueprintId, providerId, variantId, out var imageUrl)
            ? imageUrl
            : null;
    }

    public string? GetProviderExampleImageUrl(int blueprintId, int providerId)
    {
        return TryGetProviderExampleImageUrl(blueprintId, providerId, out var imageUrl)
            ? imageUrl
            : null;
    }

    public bool TryGetVariantImageUrl(int blueprintId, int providerId, int variantId, out string imageUrl)
    {
        if (_variantImageLookup.Value.TryGetValue(new VariantImageCacheKey(blueprintId, providerId, variantId), out var entry) &&
            !string.IsNullOrWhiteSpace(entry.ImageUrl))
        {
            imageUrl = entry.ImageUrl;
            return true;
        }

        var providerImageCache = _providerImageCacheByProvider.GetOrAdd(
            new ProviderImageCacheKey(blueprintId, providerId),
            LoadProviderImageCache);
        if (providerImageCache.VariantImageUrls.TryGetValue(variantId, out var providerImageUrl) &&
            !string.IsNullOrWhiteSpace(providerImageUrl))
        {
            imageUrl = providerImageUrl;
            return true;
        }

        imageUrl = string.Empty;
        return false;
    }

    public bool TryGetProviderExampleImageUrl(int blueprintId, int providerId, out string imageUrl)
    {
        var providerImageCache = _providerImageCacheByProvider.GetOrAdd(
            new ProviderImageCacheKey(blueprintId, providerId),
            LoadProviderImageCache);

        if (!string.IsNullOrWhiteSpace(providerImageCache.FirstImageUrl))
        {
            imageUrl = providerImageCache.FirstImageUrl;
            return true;
        }

        imageUrl = string.Empty;
        return false;
    }

    public IEnumerable<PrintifyCachedBlueprintProviderDetail> GetProviders(int blueprintId)
    {
        return GetBlueprintDetail(blueprintId).PrintProviders;
    }

    public IEnumerable<Variant> GetVariants(int blueprintId, int? providerId = null)
    {
        return GetProviderEntries(GetBlueprintDetail(blueprintId), providerId)
            .SelectMany(provider => provider.Variants.Variants);
    }

    public IEnumerable<PrintifyBlueprintSubvariant> GetSubvariants(int blueprintId, int? providerId = null)
    {
        var detail = GetBlueprintDetail(blueprintId);

        foreach (var provider in GetProviderEntries(detail, providerId))
        {
            var shippingLookup = BuildShippingLookup(provider.Shipping);

            foreach (var variant in provider.Variants.Variants)
            {
                shippingLookup.TryGetValue(variant.Id, out var shippingProfiles);
                shippingProfiles ??= new List<ShippingProfile>();

                yield return new PrintifyBlueprintSubvariant
                {
                    Blueprint = detail.Blueprint,
                    Provider = provider.Provider,
                    Variant = variant,
                    Options = CreateOptionMap(variant.Options),
                    Placeholders = variant.Placeholders ?? new List<VariantPlaceholder>(),
                    HandlingTime = provider.Shipping.HandlingTime,
                    ShippingProfiles = shippingProfiles,
                    Regions = GetRegions(shippingProfiles)
                };
            }
        }
    }

    public IEnumerable<PrintifyBlueprintShippingQuote> GetShippingQuotes(int blueprintId, int? providerId = null)
    {
        var detail = GetBlueprintDetail(blueprintId);

        foreach (var provider in GetProviderEntries(detail, providerId))
        {
            var variantsById = provider.Variants.Variants.ToDictionary(variant => variant.Id);

            foreach (var profile in provider.Shipping.Profiles)
            {
                var regions = profile.Countries.Count == 0
                    ? new[] { string.Empty }
                    : profile.Countries.Distinct(StringComparer.OrdinalIgnoreCase);

                foreach (var variantId in profile.VariantIds)
                {
                    if (!variantsById.TryGetValue(variantId, out var variant))
                    {
                        continue;
                    }

                    var options = CreateOptionMap(variant.Options);

                    foreach (var region in regions)
                    {
                        yield return new PrintifyBlueprintShippingQuote
                        {
                            Blueprint = detail.Blueprint,
                            Provider = provider.Provider,
                            Variant = variant,
                            Options = options,
                            HandlingTime = provider.Shipping.HandlingTime,
                            Region = region,
                            FirstItem = profile.FirstItem,
                            AdditionalItems = profile.AdditionalItems,
                            ShippingProfile = profile
                        };
                    }
                }
            }
        }
    }

    public async Task<IReadOnlyList<PrintifyBlueprintSubvariant>> GetSubvariantsWithLivePricingAsync(
        PrintifyClient client,
        int blueprintId,
        int? providerId = null,
        bool showOutOfStock = true)
    {
        ArgumentNullException.ThrowIfNull(client);

        var subvariants = GetSubvariants(blueprintId, providerId).ToList();
        if (subvariants.Count == 0)
        {
            return subvariants;
        }

        var liveVariantsByProvider = await LoadLiveVariantsByProviderAsync(client, blueprintId, providerId, showOutOfStock);

        return subvariants
            .Select(subvariant =>
            {
                if (TryGetLiveVariant(liveVariantsByProvider, subvariant.ProviderId, subvariant.VariantId, out var liveVariant))
                {
                    return subvariant with { Variant = MergeVariant(subvariant.Variant, liveVariant) };
                }

                return subvariant;
            })
            .ToList();
    }

    public async Task<IReadOnlyList<PrintifyBlueprintShippingQuote>> GetShippingQuotesWithLivePricingAsync(
        PrintifyClient client,
        int blueprintId,
        int? providerId = null,
        bool showOutOfStock = true)
    {
        ArgumentNullException.ThrowIfNull(client);

        var quotes = GetShippingQuotes(blueprintId, providerId).ToList();
        if (quotes.Count == 0)
        {
            return quotes;
        }

        var liveVariantsByProvider = await LoadLiveVariantsByProviderAsync(client, blueprintId, providerId, showOutOfStock);

        return quotes
            .Select(quote =>
            {
                if (TryGetLiveVariant(liveVariantsByProvider, quote.ProviderId, quote.VariantId, out var liveVariant))
                {
                    return quote with { Variant = MergeVariant(quote.Variant, liveVariant) };
                }

                return quote;
            })
            .ToList();
    }

    private PrintifyCachedBlueprintDetail LoadBlueprintDetail(int blueprintId)
    {
        var filePath = _blueprintFiles[blueprintId];

        using var stream = File.OpenRead(filePath);
        var detail = JsonSerializer.Deserialize<PrintifyCachedBlueprintDetail>(stream, JsonOptions)
            ?? throw new InvalidDataException($"Blueprint detail file '{filePath}' could not be deserialized.");

        if (detail.Blueprint.Id != 0 && detail.Blueprint.Id != blueprintId)
        {
            throw new InvalidDataException(
                $"Blueprint detail file '{filePath}' contains blueprint ID {detail.Blueprint.Id}, expected {blueprintId}.");
        }

        return MergeCachedVariantPricing(detail);
    }

    private PrintifyCachedBlueprintDetail MergeCachedVariantPricing(PrintifyCachedBlueprintDetail detail)
    {
        if (detail.Blueprint.Id <= 0 || detail.PrintProviders.Count == 0)
        {
            return detail;
        }

        var mergedProviders = new List<PrintifyCachedBlueprintProviderDetail>(detail.PrintProviders.Count);
        var hasChanges = false;

        foreach (var providerDetail in detail.PrintProviders)
        {
            var cachedVariantsById = _variantPricingLookupByProvider.GetOrAdd(
                new VariantPricingCacheKey(detail.Blueprint.Id, providerDetail.Provider.Id),
                LoadVariantPricingLookup);

            if (cachedVariantsById.Count == 0)
            {
                mergedProviders.Add(providerDetail);
                continue;
            }

            var mergedVariants = providerDetail.Variants.Variants
                .Select(variant => MergeVariantFromPricingCache(variant, cachedVariantsById))
                .ToList();

            var providerChanged = false;
            for (var index = 0; index < mergedVariants.Count; index++)
            {
                if (!ReferenceEquals(mergedVariants[index], providerDetail.Variants.Variants[index]))
                {
                    providerChanged = true;
                    hasChanges = true;
                    break;
                }
            }

            if (!providerChanged)
            {
                mergedProviders.Add(providerDetail);
                continue;
            }

            mergedProviders.Add(providerDetail with
            {
                Variants = providerDetail.Variants with { Variants = mergedVariants }
            });
        }

        return hasChanges ? detail with { PrintProviders = mergedProviders } : detail;
    }

    private static IEnumerable<PrintifyCachedBlueprintProviderDetail> GetProviderEntries(
        PrintifyCachedBlueprintDetail detail,
        int? providerId)
    {
        if (!providerId.HasValue)
        {
            return detail.PrintProviders;
        }

        return detail.PrintProviders.Where(provider => provider.Provider.Id == providerId.Value);
    }

    private static Dictionary<int, List<ShippingProfile>> BuildShippingLookup(ShippingInfo shipping)
    {
        var lookup = new Dictionary<int, List<ShippingProfile>>();

        foreach (var profile in shipping.Profiles)
        {
            foreach (var variantId in profile.VariantIds)
            {
                if (!lookup.TryGetValue(variantId, out var profiles))
                {
                    profiles = new List<ShippingProfile>();
                    lookup[variantId] = profiles;
                }

                profiles.Add(profile);
            }
        }

        return lookup;
    }

    private async Task<Dictionary<int, Dictionary<int, Variant>>> LoadLiveVariantsByProviderAsync(
        PrintifyClient client,
        int blueprintId,
        int? providerId,
        bool showOutOfStock)
    {
        var providers = GetProviders(blueprintId)
            .Where(provider => !providerId.HasValue || provider.Provider.Id == providerId.Value)
            .Select(provider => provider.Provider.Id)
            .Distinct()
            .ToList();

        var liveVariantsByProvider = new Dictionary<int, Dictionary<int, Variant>>();

        foreach (var currentProviderId in providers)
        {
            var variantResponse = await client.GetBlueprintVariantsAsync(blueprintId, currentProviderId, showOutOfStock);
            liveVariantsByProvider[currentProviderId] = variantResponse.Variants.ToDictionary(variant => variant.Id);
        }

        return liveVariantsByProvider;
    }

    private static bool TryGetLiveVariant(
        IReadOnlyDictionary<int, Dictionary<int, Variant>> liveVariantsByProvider,
        int providerId,
        int variantId,
        out Variant variant)
    {
        if (liveVariantsByProvider.TryGetValue(providerId, out var variantsById) &&
            variantsById.TryGetValue(variantId, out variant!))
        {
            return true;
        }

        variant = default!;
        return false;
    }

    private static Variant MergeVariant(Variant cachedVariant, Variant liveVariant)
    {
        return cachedVariant with
        {
            Title = string.IsNullOrWhiteSpace(liveVariant.Title) ? cachedVariant.Title : liveVariant.Title,
            Cost = liveVariant.Cost ?? cachedVariant.Cost,
            Price = liveVariant.Price ?? cachedVariant.Price,
            Prices = liveVariant.Prices.Count > 0 ? liveVariant.Prices : cachedVariant.Prices,
            Options = liveVariant.Options ?? cachedVariant.Options,
            Placeholders = liveVariant.Placeholders ?? cachedVariant.Placeholders
        };
    }

    private static IReadOnlyList<string> GetRegions(IEnumerable<ShippingProfile> shippingProfiles)
    {
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var regions = new List<string>();

        foreach (var profile in shippingProfiles)
        {
            foreach (var region in profile.Countries)
            {
                if (seen.Add(region))
                {
                    regions.Add(region);
                }
            }
        }

        return regions;
    }

    private IReadOnlyDictionary<int, ProductVariant> LoadVariantPricingLookup(VariantPricingCacheKey key)
    {
        var lookup = new Dictionary<int, ProductVariant>();
        var filePath = Path.Combine(VariantPricingCacheDirectory, $"{key.BlueprintId}-{key.ProviderId}.json");

        if (!File.Exists(filePath))
        {
            return new ReadOnlyDictionary<int, ProductVariant>(lookup);
        }

        try
        {
            using var stream = File.OpenRead(filePath);
            var variants = JsonSerializer.Deserialize<List<ProductVariant>>(stream, JsonOptions);

            foreach (var variant in variants ?? new List<ProductVariant>())
            {
                if (variant.Id <= 0 || variant.Cost <= 0)
                {
                    continue;
                }

                lookup[variant.Id] = variant;
            }
        }
        catch
        {
            return new ReadOnlyDictionary<int, ProductVariant>(new Dictionary<int, ProductVariant>());
        }

        return new ReadOnlyDictionary<int, ProductVariant>(lookup);
    }

    private static Variant MergeVariantFromPricingCache(
        Variant variant,
        IReadOnlyDictionary<int, ProductVariant> cachedVariantsById)
    {
        if (variant.Cost.HasValue ||
            !cachedVariantsById.TryGetValue(variant.Id, out var cachedVariant) ||
            cachedVariant.Cost <= 0)
        {
            return variant;
        }

        return variant with
        {
            Cost = cachedVariant.Cost
        };
    }

    private static IReadOnlyDictionary<string, string> CreateOptionMap(Dictionary<string, object>? options)
    {
        if (options == null || options.Count == 0)
        {
            return EmptyOptions;
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

        return new ReadOnlyDictionary<string, string>(normalized);
    }

    private static IReadOnlyDictionary<int, string> LoadFileIndex(string blueprintDetailsDirectory)
    {
        var index = new Dictionary<int, string>();

        foreach (var filePath in Directory.EnumerateFiles(blueprintDetailsDirectory, "*.json", SearchOption.TopDirectoryOnly))
        {
            if (!int.TryParse(Path.GetFileNameWithoutExtension(filePath), out var blueprintId))
            {
                continue;
            }

            index[blueprintId] = filePath;
        }

        return new ReadOnlyDictionary<int, string>(index);
    }

    private static string ResolveBlueprintDetailsDirectory(string? blueprintDetailsDirectory)
    {
        if (!string.IsNullOrWhiteSpace(blueprintDetailsDirectory))
        {
            var explicitPath = Path.GetFullPath(blueprintDetailsDirectory);
            if (Directory.Exists(explicitPath))
            {
                return explicitPath;
            }

            throw new DirectoryNotFoundException($"Blueprint details directory not found: {explicitPath}");
        }

        var probeRoots = new[]
        {
            Directory.GetCurrentDirectory(),
            AppContext.BaseDirectory,
            Path.GetDirectoryName(typeof(PrintifyBlueprintQueryApi).Assembly.Location)
        }
        .Where(path => !string.IsNullOrWhiteSpace(path))
        .Distinct(StringComparer.OrdinalIgnoreCase)!;

        foreach (var probeRoot in probeRoots)
        {
            var resolved = TryResolveBlueprintDetailsDirectory(probeRoot!);
            if (resolved != null)
            {
                return resolved;
            }
        }

        throw new DirectoryNotFoundException(
            $"Could not locate '{RelativeBlueprintDetailsPath}'. Pass the blueprint details directory explicitly to {nameof(PrintifyBlueprintDatabase.CreateQueryApi)}.");
    }

    private static string? TryResolveBlueprintDetailsDirectory(string startPath)
    {
        var current = new DirectoryInfo(Path.GetFullPath(startPath));

        while (current != null)
        {
            var candidate = Path.Combine(current.FullName, RelativeBlueprintDetailsPath);
            if (Directory.Exists(candidate))
            {
                return candidate;
            }

            current = current.Parent;
        }

        return null;
    }

    private IReadOnlyDictionary<VariantImageCacheKey, PrintifyVariantImageCacheEntry> LoadVariantImageLookup()
    {
        var index = new Dictionary<VariantImageCacheKey, PrintifyVariantImageCacheEntry>();

        if (!File.Exists(VariantImageLookupPath))
        {
            return new ReadOnlyDictionary<VariantImageCacheKey, PrintifyVariantImageCacheEntry>(index);
        }

        try
        {
            var json = File.ReadAllText(VariantImageLookupPath);
            var document = JsonSerializer.Deserialize<PrintifyVariantImageCacheDocument>(json, JsonOptions);

            foreach (var entry in document?.Entries ?? new List<PrintifyVariantImageCacheEntry>())
            {
                if (entry.BlueprintId <= 0 || entry.ProviderId <= 0 || entry.VariantId <= 0)
                {
                    continue;
                }

                var imageUrl = (entry.ImageUrl ?? string.Empty).Trim();
                if (string.IsNullOrWhiteSpace(imageUrl))
                {
                    continue;
                }

                var sanitizedEntry = new PrintifyVariantImageCacheEntry
                {
                    BlueprintId = entry.BlueprintId,
                    ProviderId = entry.ProviderId,
                    VariantId = entry.VariantId,
                    ImageUrl = imageUrl,
                    Position = (entry.Position ?? string.Empty).Trim(),
                    IsDefault = entry.IsDefault,
                    UpdatedAtUtc = entry.UpdatedAtUtc == default ? DateTime.UtcNow : entry.UpdatedAtUtc
                };

                index[new VariantImageCacheKey(entry.BlueprintId, entry.ProviderId, entry.VariantId)] = sanitizedEntry;
            }
        }
        catch
        {
            return new ReadOnlyDictionary<VariantImageCacheKey, PrintifyVariantImageCacheEntry>(
                new Dictionary<VariantImageCacheKey, PrintifyVariantImageCacheEntry>());
        }

        return new ReadOnlyDictionary<VariantImageCacheKey, PrintifyVariantImageCacheEntry>(index);
    }

    private ProviderImageCache LoadProviderImageCache(ProviderImageCacheKey key)
    {
        var filePath = Path.Combine(VariantImageCacheDirectory, $"{key.BlueprintId}-{key.ProviderId}.json");
        if (!File.Exists(filePath))
        {
            return ProviderImageCache.Empty;
        }

        try
        {
            using var stream = File.OpenRead(filePath);
            var entries = JsonSerializer.Deserialize<List<PrintifyVariantImageUrlsEntry>>(stream, JsonOptions);
            var variantImageUrls = new Dictionary<int, string>();
            string? firstImageUrl = null;

            foreach (var entry in entries ?? new List<PrintifyVariantImageUrlsEntry>())
            {
                if (entry.Id <= 0)
                {
                    continue;
                }

                var imageUrl = entry.Urls
                    .Select(url => (url ?? string.Empty).Trim())
                    .FirstOrDefault(url => !string.IsNullOrWhiteSpace(url));
                if (string.IsNullOrWhiteSpace(imageUrl))
                {
                    continue;
                }

                variantImageUrls[entry.Id] = imageUrl;
                firstImageUrl ??= imageUrl;
            }

            return new ProviderImageCache(
                new ReadOnlyDictionary<int, string>(variantImageUrls),
                firstImageUrl ?? string.Empty);
        }
        catch
        {
            return ProviderImageCache.Empty;
        }
    }
}

internal readonly record struct VariantPricingCacheKey(int BlueprintId, int ProviderId);
internal readonly record struct ProviderImageCacheKey(int BlueprintId, int ProviderId);
internal readonly record struct VariantImageCacheKey(int BlueprintId, int ProviderId, int VariantId);
internal sealed record ProviderImageCache(IReadOnlyDictionary<int, string> VariantImageUrls, string FirstImageUrl)
{
    public static ProviderImageCache Empty { get; } = new(
        new ReadOnlyDictionary<int, string>(new Dictionary<int, string>()),
        string.Empty);
}

public sealed record PrintifyCachedBlueprintDetail
{
    [JsonPropertyName("blueprint")]
    public Blueprint Blueprint { get; init; } = new();

    [JsonPropertyName("print_providers")]
    public List<PrintifyCachedBlueprintProviderDetail> PrintProviders { get; init; } = new();
}

public sealed record PrintifyCachedBlueprintProviderDetail
{
    [JsonPropertyName("provider")]
    public BlueprintPrintProvider Provider { get; init; } = new();

    [JsonPropertyName("variants")]
    public VariantResponse Variants { get; init; } = new();

    [JsonPropertyName("shipping")]
    public ShippingInfo Shipping { get; init; } = new();
}

public sealed record PrintifyBlueprintSubvariant
{
    public Blueprint Blueprint { get; init; } = new();
    public BlueprintPrintProvider Provider { get; init; } = new();
    public Variant Variant { get; init; } = new();
    public IReadOnlyDictionary<string, string> Options { get; init; } =
        new ReadOnlyDictionary<string, string>(new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase));
    public IReadOnlyList<VariantPlaceholder> Placeholders { get; init; } = Array.Empty<VariantPlaceholder>();
    public HandlingTime? HandlingTime { get; init; }
    public IReadOnlyList<ShippingProfile> ShippingProfiles { get; init; } = Array.Empty<ShippingProfile>();
    public IReadOnlyList<string> Regions { get; init; } = Array.Empty<string>();

    public int BlueprintId => Blueprint.Id;
    public string BlueprintTitle => Blueprint.Title;
    public int ProviderId => Provider.Id;
    public string ProviderTitle => Provider.Title;
    public int VariantId => Variant.Id;
    public string VariantTitle => Variant.Title;
    public int? Cost => Variant.Cost;
    public int? Price => Variant.Price ?? Variant.Prices.FirstOrDefault()?.Price;

    public string? GetOption(string optionName)
    {
        return Options.TryGetValue(optionName, out var value) ? value : null;
    }
}

public sealed record PrintifyBlueprintShippingQuote
{
    public Blueprint Blueprint { get; init; } = new();
    public BlueprintPrintProvider Provider { get; init; } = new();
    public Variant Variant { get; init; } = new();
    public IReadOnlyDictionary<string, string> Options { get; init; } =
        new ReadOnlyDictionary<string, string>(new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase));
    public HandlingTime? HandlingTime { get; init; }
    public string Region { get; init; } = string.Empty;
    public ShippingCost? FirstItem { get; init; }
    public ShippingCost? AdditionalItems { get; init; }
    public ShippingProfile ShippingProfile { get; init; } = new();

    public int BlueprintId => Blueprint.Id;
    public string BlueprintTitle => Blueprint.Title;
    public int ProviderId => Provider.Id;
    public string ProviderTitle => Provider.Title;
    public int VariantId => Variant.Id;
    public string VariantTitle => Variant.Title;
    public int? Cost => Variant.Cost;
    public int? Price => Variant.Price ?? Variant.Prices.FirstOrDefault()?.Price;
    public int? FirstItemCost => FirstItem?.Cost;
    public int? AdditionalItemCost => AdditionalItems?.Cost;
    public string Currency => FirstItem?.Currency ?? AdditionalItems?.Currency ?? string.Empty;

    public string? GetOption(string optionName)
    {
        return Options.TryGetValue(optionName, out var value) ? value : null;
    }
}

public sealed class PrintifyVariantImageCacheDocument
{
    public DateTime GeneratedAtUtc { get; set; }
    public int? ShopId { get; set; }
    public string ShopTitle { get; set; } = string.Empty;
    public string? SampleImagePreviewUrl { get; set; }
    public List<PrintifyVariantImageCacheEntry> Entries { get; set; } = new();
}

public sealed class PrintifyVariantImageCacheEntry
{
    public int BlueprintId { get; set; }
    public int ProviderId { get; set; }
    public int VariantId { get; set; }
    public string ImageUrl { get; set; } = string.Empty;
    public string Position { get; set; } = string.Empty;
    public bool IsDefault { get; set; }
    public DateTime UpdatedAtUtc { get; set; }
}

public sealed class PrintifyVariantImageUrlsEntry
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("urls")]
    public List<string> Urls { get; set; } = new();
}