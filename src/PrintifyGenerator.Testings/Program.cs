using System.Globalization;
using System.Text;

const string UkRegionCode = "GB";
const CurrencyCode RankingCurrency = CurrencyCode.GBP;
const int CatalogRequestIntervalMs = 650;
const string ProbeImageUrl = "https://dummyimage.com/1200x1200/ffffff/000000.png&text=Printify+Test";

var repositoryRoot = ResolveRepositoryRoot();
if (repositoryRoot is null)
{
    Console.Error.WriteLine("Could not locate the repository root from the current working directory.");
    return 1;
}

Directory.SetCurrentDirectory(repositoryRoot);

var logsDirectory = Path.Combine(repositoryRoot, "src", "data", "staging", "logs");
Directory.CreateDirectory(logsDirectory);

var logFilePath = Path.Combine(
    logsDirectory,
    $"uk-price-breakdown-{DateTime.UtcNow:yyyyMMdd-HHmmss}.log");

using var logStream = new StreamWriter(logFilePath, append: false)
{
    AutoFlush = true
};

using var teeOut = new TeeTextWriter(Console.Out, logStream);
using var teeError = new TeeTextWriter(Console.Error, logStream);

Console.SetOut(teeOut);
Console.SetError(teeError);

Console.WriteLine($"Logging output to {logFilePath}");

if (!TryReadIntOption(args, "--blueprint", out var blueprintFilter, out var optionError) ||
    !TryReadIntOption(args, "--limit", out var limit, out optionError))
{
    Console.Error.WriteLine(optionError);
    return 1;
}

var envFilePath = Path.Combine(repositoryRoot, "main.env");
var token = ReadToken(envFilePath);
if (string.IsNullOrWhiteSpace(token))
{
    Console.Error.WriteLine(
        "TOKEN was not found in main.env. Live Printify pricing is required because cached blueprint_details files do not contain comparable variant pricing.");
    return 1;
}

var api = PrintifyBlueprintDatabase.CreateQueryApi();
var printify = new PrintifyClient(token);
var exchangeRates = new ExchangeRateCache();

var blueprintIds = api.BlueprintIds
    .OrderBy(id => id)
    .ToList();

if (blueprintFilter.HasValue)
{
    blueprintIds = blueprintIds
        .Where(id => id == blueprintFilter.Value)
        .ToList();
}

if (limit.HasValue)
{
    blueprintIds = blueprintIds
        .Take(limit.Value)
        .ToList();
}

if (blueprintIds.Count == 0)
{
    Console.Error.WriteLine("No blueprint IDs matched the requested filters.");
    return 1;
}

var enableDraftProductCosts = HasFlag(args, "--draft-product-costs") || blueprintIds.Count == 1;
int? draftShopId = null;
UploadedImage? probeImage = null;

if (enableDraftProductCosts)
{
    var shops = await printify.GetShopsAsync();
    draftShopId = ResolveShopId(envFilePath, shops);

    if (!draftShopId.HasValue)
    {
        Console.Error.WriteLine("No usable Printify shop was found for temporary draft-product cost probing.");
        return 1;
    }

    probeImage = await UploadProbeImageAsync(printify);
    var selectedShop = shops.First(shop => shop.Id == draftShopId.Value);
    Console.WriteLine($"Using shop {selectedShop.Id} ({selectedShop.Title}) for temporary draft-product cost probing.");
    Console.WriteLine($"Uploaded probe image {probeImage.Id} for temporary product creation.");
}
else
{
    Console.WriteLine("Draft-product production cost probing is disabled for multi-blueprint scans to avoid product-creation rate limits.");
}

Console.WriteLine($"Scanning {blueprintIds.Count} blueprint(s) for UK-deliverable provider/variant/options combinations.");
Console.WriteLine("Ranking is based on one-item delivered total to GB: provider amount + first-item shipping, normalized to GBP.");

UkDeliveredCandidate? cheapest = null;
UkDeliveredCandidate? mostExpensive = null;
UkShippingCandidate? cheapestShippingOnly = null;
UkShippingCandidate? mostExpensiveShippingOnly = null;

var ukBlueprintCount = 0;
var skippedNoUkShipping = 0;
var skippedUnsupportedCurrency = 0;
var skippedNoComparableAmount = 0;
var providerFailures = 0;
var totalCandidates = 0;
var lastCatalogRequestAt = DateTimeOffset.MinValue;

try
{
foreach (var blueprintId in blueprintIds)
{
    var ukShippingQuotes = api.GetShippingQuotes(blueprintId)
        .Where(quote => string.Equals(quote.Region, UkRegionCode, StringComparison.OrdinalIgnoreCase))
        .Where(quote => quote.FirstItemCost.HasValue)
        .ToList();

    if (ukShippingQuotes.Count == 0)
    {
        skippedNoUkShipping++;
        continue;
    }

    ukBlueprintCount++;

    var providerIds = ukShippingQuotes
        .Select(quote => quote.ProviderId)
        .Distinct()
        .OrderBy(id => id)
        .ToList();

    Console.WriteLine();
    Console.WriteLine($"Blueprint {blueprintId} - {ukShippingQuotes[0].BlueprintTitle}: {providerIds.Count} UK-capable provider(s)");

    foreach (var providerId in providerIds)
    {
        lastCatalogRequestAt = await WaitForCatalogSlotAsync(lastCatalogRequestAt, CatalogRequestIntervalMs);

        IReadOnlyList<PrintifyBlueprintSubvariant> subvariants;
        try
        {
            subvariants = await LoadLiveSubvariantsWithRetryAsync(api, printify, blueprintId, providerId);
        }
        catch (Exception ex)
        {
            providerFailures++;
            Console.WriteLine($"  Provider {providerId} failed after retries: {ex.Message}");
            continue;
        }

        IReadOnlyDictionary<int, DraftProductVariantInfo>? draftProductVariants = null;
        if (enableDraftProductCosts && draftShopId.HasValue && probeImage is not null)
        {
            try
            {
                draftProductVariants = await LoadDraftProductVariantDetailsAsync(
                    printify,
                    draftShopId.Value,
                    blueprintId,
                    providerId,
                    subvariants,
                    probeImage.Id);

                Console.WriteLine($"  Loaded production costs for {draftProductVariants.Count} variants via temporary draft product.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"  Draft-product cost probe failed for provider {providerId}: {ex.Message}");
            }
        }

        var cachedSubvariantsById = subvariants.ToDictionary(subvariant => subvariant.VariantId);
        var providerTitle = subvariants.FirstOrDefault()?.ProviderTitle ?? providerId.ToString(CultureInfo.InvariantCulture);

        Dictionary<int, List<ProviderShippingQuote>> quotesByVariantId;
        if (enableDraftProductCosts)
        {
            try
            {
                var liveShipping = await printify.GetBlueprintShippingAsync(blueprintId, providerId);
                quotesByVariantId = BuildUkShippingQuotesByVariantId(liveShipping);

                if (quotesByVariantId.Count == 0)
                {
                    Console.WriteLine($"  Provider {providerId} has no live GB shipping quotes.");
                    continue;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"  Live shipping lookup failed for provider {providerId}: {ex.Message}");
                quotesByVariantId = ukShippingQuotes
                    .Where(quote => quote.ProviderId == providerId)
                    .GroupBy(quote => quote.VariantId)
                    .ToDictionary(
                        group => group.Key,
                        group => group.Select(quote => new ProviderShippingQuote(
                            quote.VariantId,
                            quote.FirstItemCost,
                            quote.AdditionalItemCost,
                            quote.Currency)).ToList());
            }
        }
        else
        {
            quotesByVariantId = ukShippingQuotes
                .Where(quote => quote.ProviderId == providerId)
                .GroupBy(quote => quote.VariantId)
                .ToDictionary(
                    group => group.Key,
                    group => group.Select(quote => new ProviderShippingQuote(
                        quote.VariantId,
                        quote.FirstItemCost,
                        quote.AdditionalItemCost,
                        quote.Currency)).ToList());
        }

        var variantIdsToProcess = (enableDraftProductCosts && draftProductVariants is not null)
            ? quotesByVariantId.Keys.OrderBy(id => id).ToList()
            : subvariants.Select(subvariant => subvariant.VariantId).ToList();

        foreach (var variantId in variantIdsToProcess)
        {
            if (!quotesByVariantId.TryGetValue(variantId, out var variantQuotes))
            {
                continue;
            }

            cachedSubvariantsById.TryGetValue(variantId, out var subvariant);

            DraftProductVariantInfo? draftVariantInfo = null;
            if (draftProductVariants is not null)
            {
                draftProductVariants.TryGetValue(variantId, out draftVariantInfo);
            }

            if (subvariant is null && draftVariantInfo is null)
            {
                continue;
            }

            var variantTitle = subvariant?.VariantTitle ?? draftVariantInfo!.VariantTitle;
            var optionsText = subvariant is not null
                ? FormatOptions(subvariant.Options)
                : draftVariantInfo!.OptionsText;

            foreach (var quote in variantQuotes)
            {
                if (!TryParseCurrencyCode(quote.Currency, out var shippingCurrency))
                {
                    skippedUnsupportedCurrency++;
                    continue;
                }

                var shippingOnlyAmount = new ComparableMoney(
                    quote.FirstItemCost!.Value,
                    shippingCurrency,
                    "first-item shipping");

                ComparableMoney? shippingOnlyAdditional = quote.AdditionalItemCost.HasValue
                    ? new ComparableMoney(quote.AdditionalItemCost.Value, shippingCurrency, "additional-item shipping")
                    : null;

                var shippingOnlyGbp = await exchangeRates.ConvertMinorUnitsAsync(
                    shippingOnlyAmount.MinorUnits,
                    shippingOnlyAmount.Currency,
                    RankingCurrency);

                decimal? shippingOnlyAdditionalGbp = null;
                if (shippingOnlyAdditional is not null)
                {
                    shippingOnlyAdditionalGbp = await exchangeRates.ConvertMinorUnitsAsync(
                        shippingOnlyAdditional.Value.MinorUnits,
                        shippingOnlyAdditional.Value.Currency,
                        RankingCurrency);
                }

                var shippingCandidate = new UkShippingCandidate(
                    blueprintId,
                    ukShippingQuotes[0].BlueprintTitle,
                    providerId,
                    providerTitle,
                    variantId,
                    variantTitle,
                    optionsText,
                    shippingOnlyAmount,
                    shippingOnlyAdditional,
                    shippingOnlyGbp,
                    shippingOnlyAdditionalGbp);

                if (cheapestShippingOnly is null || shippingCandidate.ShippingGbp < cheapestShippingOnly.ShippingGbp)
                {
                    cheapestShippingOnly = shippingCandidate;
                }

                if (mostExpensiveShippingOnly is null || shippingCandidate.ShippingGbp > mostExpensiveShippingOnly.ShippingGbp)
                {
                    mostExpensiveShippingOnly = shippingCandidate;
                }

                if (!TryResolveComparableAmount(variantId, subvariant?.Variant, shippingCurrency, draftProductVariants, out var comparableAmount))
                {
                    skippedNoComparableAmount++;
                    Console.WriteLine(
                        $"  Missing provider amount for variant {variantId} ({variantTitle}) | " +
                        $"cost={FormatNullableMinorUnits(subvariant?.Variant.Cost)} | " +
                        $"price={FormatNullableMinorUnits(subvariant?.Variant.Price)} | " +
                        $"prices={(subvariant?.Variant.Prices.Count ?? 0)} | " +
                        $"shipping={FormatMinorMoney(shippingOnlyAmount.MinorUnits, shippingOnlyAmount.Currency)} | " +
                        $"options {optionsText}");
                    continue;
                }

                var shippingAmount = shippingOnlyAmount;
                var additionalShipping = shippingOnlyAdditional;

                var comparableAmountGbp = await exchangeRates.ConvertMinorUnitsAsync(
                    comparableAmount.MinorUnits,
                    comparableAmount.Currency,
                    RankingCurrency);

                var shippingAmountGbp = shippingOnlyGbp;
                var additionalShippingGbp = shippingOnlyAdditionalGbp;

                var candidate = new UkDeliveredCandidate(
                    blueprintId,
                    ukShippingQuotes[0].BlueprintTitle,
                    providerId,
                    providerTitle,
                    variantId,
                    variantTitle,
                    optionsText,
                    comparableAmount,
                    shippingAmount,
                    additionalShipping,
                    comparableAmountGbp,
                    shippingAmountGbp,
                    additionalShippingGbp,
                    comparableAmountGbp + shippingAmountGbp);

                totalCandidates++;
                Console.WriteLine(FormatCandidate(totalCandidates, candidate));

                if (cheapest is null || candidate.TotalGbp < cheapest.TotalGbp)
                {
                    cheapest = candidate;
                    Console.WriteLine($"  -> New cheapest so far: {FormatMoney(candidate.TotalGbp, RankingCurrency)}");
                }

                if (mostExpensive is null || candidate.TotalGbp > mostExpensive.TotalGbp)
                {
                    mostExpensive = candidate;
                    Console.WriteLine($"  -> New most expensive so far: {FormatMoney(candidate.TotalGbp, RankingCurrency)}");
                }
            }
        }
    }
}

Console.WriteLine();
Console.WriteLine($"Finished scanning {blueprintIds.Count} blueprint(s).");
Console.WriteLine($"UK-capable blueprints: {ukBlueprintCount}");
Console.WriteLine($"UK-deliverable combinations priced: {totalCandidates}");

if (skippedNoUkShipping > 0)
{
    Console.WriteLine($"Skipped without UK shipping: {skippedNoUkShipping}");
}

if (providerFailures > 0)
{
    Console.WriteLine($"Providers skipped after retries: {providerFailures}");
}

if (skippedNoComparableAmount > 0)
{
    Console.WriteLine($"Variants skipped without comparable provider amount: {skippedNoComparableAmount}");
}

if (skippedUnsupportedCurrency > 0)
{
    Console.WriteLine($"Variants skipped due to unsupported currency code: {skippedUnsupportedCurrency}");
}

if (cheapest is null || mostExpensive is null)
{
    Console.Error.WriteLine("No UK-deliverable combinations with provider base pricing were found.");
    Console.Error.WriteLine(
        "Printify's current public catalog variants endpoint no longer returns cost or price fields, so delivered totals cannot be computed from catalog data alone.");

    if (cheapestShippingOnly is not null && mostExpensiveShippingOnly is not null)
    {
        Console.WriteLine();
        Console.WriteLine("Cheapest UK shipping-only combination:");
        Console.WriteLine(FormatShippingSummary(cheapestShippingOnly));

        Console.WriteLine();
        Console.WriteLine("Most expensive UK shipping-only combination:");
        Console.WriteLine(FormatShippingSummary(mostExpensiveShippingOnly));
    }

    return 1;
}

Console.WriteLine();
Console.WriteLine("Cheapest UK-deliverable combination:");
Console.WriteLine(FormatSummary(cheapest));

Console.WriteLine();
Console.WriteLine("Most expensive UK-deliverable combination:");
Console.WriteLine(FormatSummary(mostExpensive));

return 0;
}
finally
{
    if (probeImage is not null)
    {
        await ArchiveProbeImageSafeAsync(printify, probeImage.Id);
    }
}

static string? ResolveRepositoryRoot()
{
    var probeRoots = new[]
    {
        Directory.GetCurrentDirectory(),
        AppContext.BaseDirectory
    }
    .Where(path => !string.IsNullOrWhiteSpace(path))
    .Distinct(StringComparer.OrdinalIgnoreCase);

    foreach (var probeRoot in probeRoots)
    {
        var current = new DirectoryInfo(Path.GetFullPath(probeRoot));

        while (current is not null)
        {
            if (File.Exists(Path.Combine(current.FullName, "PrintifyGenerator.sln")))
            {
                return current.FullName;
            }

            current = current.Parent;
        }
    }

    return null;
}

static string ReadToken(string envFilePath)
{
    if (!File.Exists(envFilePath))
    {
        return string.Empty;
    }

    foreach (var line in File.ReadLines(envFilePath))
    {
        if (line.StartsWith("TOKEN=", StringComparison.OrdinalIgnoreCase))
        {
            return line["TOKEN=".Length..].Trim();
        }
    }

    return string.Empty;
}

static bool TryReadIntOption(string[] args, string optionName, out int? value, out string error)
{
    value = null;
    error = string.Empty;

    for (var index = 0; index < args.Length; index++)
    {
        if (!string.Equals(args[index], optionName, StringComparison.OrdinalIgnoreCase))
        {
            continue;
        }

        if (index + 1 >= args.Length)
        {
            error = $"Missing value for {optionName}.";
            return false;
        }

        if (!int.TryParse(args[index + 1], NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsedValue) || parsedValue <= 0)
        {
            error = $"Invalid value '{args[index + 1]}' for {optionName}. Expected a positive integer.";
            return false;
        }

        value = parsedValue;
        index++;
    }

    return true;
}

static bool HasFlag(string[] args, string flag)
{
    return args.Any(arg => string.Equals(arg, flag, StringComparison.OrdinalIgnoreCase));
}

static int? ResolveShopId(string envFilePath, IReadOnlyList<Shop> shops)
{
    var configuredShopId = ReadOptionalIntEnvValue(envFilePath, "SHOP_ID")
        ?? ReadOptionalIntEnvValue(envFilePath, "SHOPID");

    if (configuredShopId.HasValue)
    {
        return shops.Any(shop => shop.Id == configuredShopId.Value)
            ? configuredShopId.Value
            : null;
    }

    return shops
        .Where(shop => string.Equals(shop.SalesChannel, "custom_integration", StringComparison.OrdinalIgnoreCase))
        .Select(shop => (int?)shop.Id)
        .FirstOrDefault()
        ?? shops.Select(shop => (int?)shop.Id).FirstOrDefault();
}

static int? ReadOptionalIntEnvValue(string envFilePath, string key)
{
    if (!File.Exists(envFilePath))
    {
        return null;
    }

    foreach (var line in File.ReadLines(envFilePath))
    {
        if (!line.StartsWith($"{key}=", StringComparison.OrdinalIgnoreCase))
        {
            continue;
        }

        return int.TryParse(line[(key.Length + 1)..].Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var value)
            ? value
            : null;
    }

    return null;
}

static async Task<DateTimeOffset> WaitForCatalogSlotAsync(DateTimeOffset lastRequestAt, int minimumIntervalMs)
{
    if (lastRequestAt != DateTimeOffset.MinValue)
    {
        var elapsed = DateTimeOffset.UtcNow - lastRequestAt;
        var remainingDelay = minimumIntervalMs - (int)elapsed.TotalMilliseconds;

        if (remainingDelay > 0)
        {
            await Task.Delay(remainingDelay);
        }
    }

    return DateTimeOffset.UtcNow;
}

static async Task<IReadOnlyList<PrintifyBlueprintSubvariant>> LoadLiveSubvariantsWithRetryAsync(
    PrintifyBlueprintQueryApi api,
    PrintifyClient client,
    int blueprintId,
    int providerId)
{
    Exception? lastError = null;

    for (var attempt = 1; attempt <= 3; attempt++)
    {
        try
        {
            return await api.GetSubvariantsWithLivePricingAsync(client, blueprintId, providerId, showOutOfStock: true);
        }
        catch (Exception ex)
        {
            lastError = ex;

            if (attempt == 3)
            {
                break;
            }

            Console.WriteLine($"  Provider {providerId} attempt {attempt} failed: {ex.Message}. Retrying...");
            await Task.Delay(TimeSpan.FromSeconds(attempt * 2));
        }
    }

    throw lastError ?? new InvalidOperationException($"Provider {providerId} could not be loaded.");
}

static async Task<UploadedImage> UploadProbeImageAsync(PrintifyClient client)
{
    return await client.UploadImageByUrlAsync(
        $"uk-cost-probe-{DateTime.UtcNow:yyyyMMdd-HHmmss}.png",
        ProbeImageUrl);
}

static async Task<IReadOnlyDictionary<int, DraftProductVariantInfo>> LoadDraftProductVariantDetailsAsync(
    PrintifyClient client,
    int shopId,
    int blueprintId,
    int providerId,
    IReadOnlyList<PrintifyBlueprintSubvariant> subvariants,
    string uploadedImageId)
{
    var seedSubvariant = subvariants.FirstOrDefault(subvariant => subvariant.Placeholders.Count > 0)
        ?? throw new InvalidOperationException(
            $"Blueprint {blueprintId}, provider {providerId} has no placeholders available for temporary product creation.");

    var seedPlaceholder = seedSubvariant.Placeholders
        .FirstOrDefault(placeholder => string.Equals(placeholder.Position, "front", StringComparison.OrdinalIgnoreCase))
        ?? seedSubvariant.Placeholders[0];

    var request = new CreateProductRequest
    {
        Title = $"UK Cost Probe - {seedSubvariant.BlueprintTitle} - {providerId}",
        Description = "Temporary product used to inspect Printify production cost per variant.",
        BlueprintId = blueprintId,
        PrintProviderId = providerId,
        Variants = new List<CreateProductVariant>
        {
            new()
            {
                Id = seedSubvariant.VariantId,
                Price = 2000,
                IsEnabled = true
            }
        },
        PrintAreas = new List<PrintArea>
        {
            new()
            {
                VariantIds = new List<int> { seedSubvariant.VariantId },
                Placeholders = new List<PrintAreaPlaceholder>
                {
                    new()
                    {
                        Position = seedPlaceholder.Position,
                        DecorationMethod = string.IsNullOrWhiteSpace(seedPlaceholder.DecorationMethod)
                            ? null
                            : seedPlaceholder.DecorationMethod,
                        Images = new List<PrintAreaImage>
                        {
                            new()
                            {
                                Id = uploadedImageId,
                                X = 0.5,
                                Y = 0.5,
                                Scale = 1,
                                Angle = 0,
                                Width = 1,
                                Height = 1
                            }
                        }
                    }
                }
            }
        }
    };

    Product? product = null;

    try
    {
        product = await client.CreateProductAsync(shopId, request);
        var optionLookup = new Dictionary<int, (string Name, string Title)>();

        if (product.Options is not null)
        {
            foreach (var option in product.Options)
            {
                if (option.Values is null)
                {
                    continue;
                }

                foreach (var value in option.Values)
                {
                    optionLookup[value.Id] = (option.Type, value.Title);
                }
            }
        }

        return product.Variants.ToDictionary(
            variant => variant.Id,
            variant => new DraftProductVariantInfo(
                variant.Cost,
                variant.Title,
                FormatProductVariantOptions(variant.Options, optionLookup)));
    }
    finally
    {
        if (product is not null)
        {
            await DeleteProbeProductSafeAsync(client, shopId, product.Id);
        }
    }
}

static async Task DeleteProbeProductSafeAsync(PrintifyClient client, int shopId, string productId)
{
    try
    {
        await client.DeleteProductAsync(shopId, productId);
        Console.WriteLine($"  Deleted temporary probe product {productId}.");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"  Failed to delete temporary probe product {productId}: {ex.Message}");
    }
}

static async Task ArchiveProbeImageSafeAsync(PrintifyClient client, string imageId)
{
    try
    {
        await client.ArchiveUploadAsync(imageId);
        Console.WriteLine($"Archived probe image {imageId}.");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Failed to archive probe image {imageId}: {ex.Message}");
    }
}

static bool TryResolveComparableAmount(
    int variantId,
    Variant? variant,
    CurrencyCode shippingCurrency,
    IReadOnlyDictionary<int, DraftProductVariantInfo>? draftProductVariants,
    out ComparableMoney amount)
{
    if (draftProductVariants is not null && draftProductVariants.TryGetValue(variantId, out var draftVariant))
    {
        amount = new ComparableMoney(draftVariant.Cost, shippingCurrency, "production cost");
        return true;
    }

    if (variant is null)
    {
        amount = default;
        return false;
    }

    if (variant.Cost is int cost)
    {
        amount = new ComparableMoney(cost, shippingCurrency, "provider cost");
        return true;
    }

    if (TryResolveFromPrices(variant.Prices, RankingCurrency, out amount))
    {
        return true;
    }

    if (TryResolveFromPrices(variant.Prices, shippingCurrency, out amount))
    {
        return true;
    }

    if (TryResolveFromPrices(variant.Prices, null, out amount))
    {
        return true;
    }

    if (variant.Price is int price)
    {
        amount = new ComparableMoney(price, shippingCurrency, "price");
        return true;
    }

    amount = default;
    return false;
}

static bool TryResolveFromPrices(
    IReadOnlyList<VariantPrice> prices,
    CurrencyCode? preferredCurrency,
    out ComparableMoney amount)
{
    IEnumerable<VariantPrice> candidates = prices;

    if (preferredCurrency.HasValue)
    {
        candidates = candidates.Where(price =>
            string.Equals(price.Currency, preferredCurrency.Value.ToString(), StringComparison.OrdinalIgnoreCase));
    }

    foreach (var candidate in candidates)
    {
        if (TryParseCurrencyCode(candidate.Currency, out var currencyCode))
        {
            amount = new ComparableMoney(candidate.Price, currencyCode, "price");
            return true;
        }
    }

    amount = default;
    return false;
}

static bool TryParseCurrencyCode(string? currency, out CurrencyCode currencyCode)
{
    if (!string.IsNullOrWhiteSpace(currency) &&
        Enum.TryParse<CurrencyCode>(currency, ignoreCase: true, out currencyCode))
    {
        return true;
    }

    currencyCode = default;
    return false;
}

static string FormatCandidate(int index, UkDeliveredCandidate candidate)
{
    var additionalShippingText = candidate.AdditionalShipping is null
        ? string.Empty
        : $" | additional shipping {FormatMinorMoney(candidate.AdditionalShipping.Value.MinorUnits, candidate.AdditionalShipping.Value.Currency)}";

    return $"[{index}] total {FormatMoney(candidate.TotalGbp, RankingCurrency)} | " +
           $"{candidate.ComparableAmount.Label} {FormatMinorMoney(candidate.ComparableAmount.MinorUnits, candidate.ComparableAmount.Currency)} + " +
           $"shipping {FormatMinorMoney(candidate.ShippingAmount.MinorUnits, candidate.ShippingAmount.Currency)}{additionalShippingText} | " +
           $"blueprint {candidate.BlueprintId} {candidate.BlueprintTitle} | " +
           $"provider {candidate.ProviderId} {candidate.ProviderTitle} | " +
           $"variant {candidate.VariantId} {candidate.VariantTitle} | " +
           $"options {candidate.OptionsText}";
}

static string FormatSummary(UkDeliveredCandidate candidate)
{
    var lines = new List<string>
    {
        $"Total to GB: {FormatMoney(candidate.TotalGbp, RankingCurrency)}",
        $"Provider amount: {FormatMinorMoney(candidate.ComparableAmount.MinorUnits, candidate.ComparableAmount.Currency)} ({candidate.ComparableAmount.Label})",
        $"First-item shipping: {FormatMinorMoney(candidate.ShippingAmount.MinorUnits, candidate.ShippingAmount.Currency)}",
        $"Blueprint: {candidate.BlueprintId} - {candidate.BlueprintTitle}",
        $"Provider: {candidate.ProviderId} - {candidate.ProviderTitle}",
        $"Variant: {candidate.VariantId} - {candidate.VariantTitle}",
        $"Options: {candidate.OptionsText}"
    };

    if (candidate.AdditionalShipping is not null)
    {
        lines.Add($"Additional-item shipping: {FormatMinorMoney(candidate.AdditionalShipping.Value.MinorUnits, candidate.AdditionalShipping.Value.Currency)}");
    }

    return string.Join(Environment.NewLine, lines);
}

static string FormatShippingSummary(UkShippingCandidate candidate)
{
    var lines = new List<string>
    {
        $"First-item shipping to GB: {FormatMoney(candidate.ShippingGbp, RankingCurrency)}",
        $"Shipping amount: {FormatMinorMoney(candidate.ShippingAmount.MinorUnits, candidate.ShippingAmount.Currency)}",
        $"Blueprint: {candidate.BlueprintId} - {candidate.BlueprintTitle}",
        $"Provider: {candidate.ProviderId} - {candidate.ProviderTitle}",
        $"Variant: {candidate.VariantId} - {candidate.VariantTitle}",
        $"Options: {candidate.OptionsText}"
    };

    if (candidate.AdditionalShipping is not null)
    {
        lines.Add($"Additional-item shipping: {FormatMinorMoney(candidate.AdditionalShipping.Value.MinorUnits, candidate.AdditionalShipping.Value.Currency)}");
    }

    return string.Join(Environment.NewLine, lines);
}

static string FormatOptions(IReadOnlyDictionary<string, string> options)
{
    if (options.Count == 0)
    {
        return "(none)";
    }

    return string.Join(", ", options
        .OrderBy(option => option.Key, StringComparer.OrdinalIgnoreCase)
        .Select(option => $"{option.Key}={option.Value}"));
}

static string FormatMinorMoney(int minorUnits, CurrencyCode currency)
{
    return FormatMoney(minorUnits / 100m, currency);
}

static string FormatMoney(decimal amount, CurrencyCode currency)
{
    return $"{currency} {amount.ToString("0.00", CultureInfo.InvariantCulture)}";
}

static string FormatNullableMinorUnits(int? minorUnits)
{
    return minorUnits.HasValue
        ? (minorUnits.Value / 100m).ToString("0.00", CultureInfo.InvariantCulture)
        : "null";
}

static string FormatProductVariantOptions(
    IReadOnlyList<int>? optionIds,
    IReadOnlyDictionary<int, (string Name, string Title)> optionLookup)
{
    if (optionIds is null || optionIds.Count == 0)
    {
        return "(none)";
    }

    return string.Join(", ", optionIds
        .Select(optionId => optionLookup.TryGetValue(optionId, out var option)
            ? (Name: option.Name, Title: option.Title)
            : (Name: $"option_{optionId}", Title: optionId.ToString(CultureInfo.InvariantCulture)))
        .OrderBy(option => option.Name, StringComparer.OrdinalIgnoreCase)
        .Select(option => $"{option.Name}={option.Title}"));
}

static Dictionary<int, List<ProviderShippingQuote>> BuildUkShippingQuotesByVariantId(ShippingInfo shipping)
{
    var quotesByVariantId = new Dictionary<int, List<ProviderShippingQuote>>();

    foreach (var profile in shipping.Profiles)
    {
        if (!profile.Countries.Contains(UkRegionCode, StringComparer.OrdinalIgnoreCase))
        {
            continue;
        }

        var quote = new ProviderShippingQuote(
            0,
            profile.FirstItem?.Cost,
            profile.AdditionalItems?.Cost,
            profile.FirstItem?.Currency ?? profile.AdditionalItems?.Currency ?? string.Empty);

        foreach (var variantId in profile.VariantIds)
        {
            if (!quotesByVariantId.TryGetValue(variantId, out var quotes))
            {
                quotes = new List<ProviderShippingQuote>();
                quotesByVariantId[variantId] = quotes;
            }

            quotes.Add(quote with { VariantId = variantId });
        }
    }

    return quotesByVariantId;
}

readonly record struct ComparableMoney(int MinorUnits, CurrencyCode Currency, string Label);

sealed record DraftProductVariantInfo(int Cost, string VariantTitle, string OptionsText);

sealed record UkDeliveredCandidate(
    int BlueprintId,
    string BlueprintTitle,
    int ProviderId,
    string ProviderTitle,
    int VariantId,
    string VariantTitle,
    string OptionsText,
    ComparableMoney ComparableAmount,
    ComparableMoney ShippingAmount,
    ComparableMoney? AdditionalShipping,
    decimal ComparableAmountGbp,
    decimal ShippingAmountGbp,
    decimal? AdditionalShippingGbp,
    decimal TotalGbp);

readonly record struct ProviderShippingQuote(
    int VariantId,
    int? FirstItemCost,
    int? AdditionalItemCost,
    string Currency);

sealed record UkShippingCandidate(
    int BlueprintId,
    string BlueprintTitle,
    int ProviderId,
    string ProviderTitle,
    int VariantId,
    string VariantTitle,
    string OptionsText,
    ComparableMoney ShippingAmount,
    ComparableMoney? AdditionalShipping,
    decimal ShippingGbp,
    decimal? AdditionalShippingGbp);

sealed class ExchangeRateCache
{
    private readonly Dictionary<(CurrencyCode From, CurrencyCode To), decimal> _rates = new();

    public async Task<decimal> ConvertMinorUnitsAsync(int minorUnits, CurrencyCode from, CurrencyCode to)
    {
        var amount = minorUnits / 100m;
        if (from == to)
        {
            return amount;
        }

        var rate = await GetRateAsync(from, to);
        return amount * rate;
    }

    private async Task<decimal> GetRateAsync(CurrencyCode from, CurrencyCode to)
    {
        var key = (from, to);
        if (_rates.TryGetValue(key, out var cachedRate))
        {
            return cachedRate;
        }

        var rate = await new Currency(from, 1m).GetCurrentExchangeRate(to);
        _rates[key] = rate;
        return rate;
    }
}

sealed class TeeTextWriter : TextWriter
{
    private readonly TextWriter _primary;
    private readonly TextWriter _secondary;

    public TeeTextWriter(TextWriter primary, TextWriter secondary)
    {
        _primary = primary;
        _secondary = secondary;
    }

    public override Encoding Encoding => _primary.Encoding;

    public override void Write(char value)
    {
        _primary.Write(value);
        _secondary.Write(value);
    }

    public override void Write(string? value)
    {
        _primary.Write(value);
        _secondary.Write(value);
    }

    public override void WriteLine(string? value)
    {
        _primary.WriteLine(value);
        _secondary.WriteLine(value);
    }

    public override void Flush()
    {
        _primary.Flush();
        _secondary.Flush();
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _secondary.Flush();
        }
    }
}