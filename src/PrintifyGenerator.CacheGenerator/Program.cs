using System.Globalization;
using System.Text;
using System.Text.Json;

// get shop
var ProbeImageUrl = "https://dummyimage.com/1200x1200/f6efe3/122029.png&text=Printify+Variant+Image+Probe";
var PricingProductProbeImageFileName = "pricing-product-probe.png";
var runStartedAt = DateTime.UtcNow;


var repositoryRoot = ResolveRepositoryRoot();
if (repositoryRoot is null)
{
    Console.Error.WriteLine("Could not locate the repository root from the current working directory.");
    return 1;
}

var envFilePath = Path.Combine(repositoryRoot, "main.env");
var token = ReadToken(envFilePath);
if (string.IsNullOrWhiteSpace(token))
{
    Console.Error.WriteLine("TOKEN was not found in main.env.");
    return 1;
}

string cacheDir = Path.Combine(repositoryRoot, "src", "data", "Cached");
LogPhase("Startup");
LogStatus($"Repository root: {repositoryRoot}");
LogStatus($"Cache directory: {cacheDir}");

var client = new PrintifyClient(token);
var api = PrintifyBlueprintDatabase.CreateQueryApi(Path.Combine(cacheDir, "blueprint_details"));




//find the shop with the name "Cache"
LogPhase("Shop Discovery");
var shops = await client.GetShopsAsync();
LogStatus($"Loaded {shops.Count} shop(s) from Printify.");
var shop = shops.FirstOrDefault(s => string.Equals(s.Title, "Cache", StringComparison.OrdinalIgnoreCase));
if (shop is null){
    Console.Error.WriteLine("No shop with the name 'Cache' was found. Please create a shop named 'Cache' and run this program again.");
    return 1;
}
LogStatus($"Using cache shop {shop.Id} ({shop.Title}).");

//get a list of all products in the shop
var products = await client.GetAllProductsAsync(shop.Id);
LogStatus($"Loaded {products.Count} existing product(s) from the cache shop.");

// int idx = 0;
// foreach (var product in products){
//     try
//     {
//         await client.DeleteProductAsync(shop.Id, product.Id);
//         Console.WriteLine($"Deleted product {product.Id} with title {product.Title}. ({++idx}/{products.Count})");
//     }
//     catch (PrintifyApiException ex)
//     {
//         Console.Error.WriteLine($"Failed to delete product {product.Id} with title {product.Title}  {ex.Message}");
//     }
// }







//get all blueprints ids from the cache
LogPhase("Blueprint Sync");
var blueprintIds = api.BlueprintIds.ToHashSet();
//get all blueprint ids from the client
var clientBlueprints = await client.GetBlueprintsAsync();
var clientBlueprintIds = clientBlueprints.Select(b => b.Id).ToHashSet();
LogStatus($"Cached blueprint count: {blueprintIds.Count()}. Client blueprint count: {clientBlueprintIds.Count}.");
//find blueprint ids that are in the client but not in the cache
var missingBlueprintIds = clientBlueprintIds.Where(id => !blueprintIds.Contains(id)).ToList();
if (missingBlueprintIds.Count != 0){
    //update the cache with the missing blueprints
    LogStatus($"Found {missingBlueprintIds.Count} missing blueprint(s). Refreshing cached blueprint details.");
    foreach (var blueprintId in missingBlueprintIds){
        try{
            LogStatus($"Fetching blueprint {blueprintId} metadata.");
            var blueprint = await client.GetBlueprintAsync(blueprintId);
            var providers = await client.GetBlueprintPrintProvidersAsync(blueprintId);
            LogStatus($"Blueprint {blueprint.Id} has {providers.Count} provider(s) to cache.");
            var providerDetails = new List<object>();
            foreach (var provider in providers)
            {
                LogStatus($"Fetching provider {provider.Id} for blueprint {blueprint.Id}.");
                VariantResponse? variants = null;
                try
                {
                    variants = await client.GetBlueprintVariantsAsync(blueprint.Id, provider.Id, showOutOfStock: true);
                }
                catch (PrintifyApiException ex) when (ex.StatusCode == 404)
                {
                    Console.WriteLine($"\n    Variants missing for blueprint {blueprint.Id}, provider {provider.Id}: {ex.Message}");
                }

                ShippingInfo? shipping = null;
                try
                {
                    shipping = await client.GetBlueprintShippingAsync(blueprint.Id, provider.Id);
                }
                catch (PrintifyApiException ex) when (ex.StatusCode == 404)
                {
                    Console.WriteLine($"\n    Shipping missing for blueprint {blueprint.Id}, provider {provider.Id}: {ex.Message}");
                }

                providerDetails.Add(new
                {
                    provider,
                    variants,
                    shipping,
                    cost_per_region = shipping?.Profiles
                        .Select(profile => new
                        {
                            countries = profile.Countries,
                            first_item = profile.FirstItem,
                            additional_items = profile.AdditionalItems,
                            variant_count = profile.VariantIds.Count
                        })
                        .ToList()
                });
                //save the blueprint details to the cache
                var detail = new
                {
                    blueprint,
                    print_providers = providerDetails
                };
                var blueprintDetailsDir = Path.Combine(cacheDir, "blueprint_details");
                Directory.CreateDirectory(blueprintDetailsDir);
                var jsonOpts = new JsonSerializerOptions { WriteIndented = true };
                await File.WriteAllTextAsync(
                    Path.Combine(blueprintDetailsDir, $"{blueprint.Id}.json"),
                    JsonSerializer.Serialize(detail, jsonOpts));
                Console.WriteLine($"  Cached missing blueprint {blueprint.Id} - {blueprint.Title}.");
            }
            //update the blueprints.json file in the cache
            var blueprints = await client.GetBlueprintsAsync();
            blueprints = blueprints.OrderBy(blueprint => blueprint.Id).ToList();
            await File.WriteAllTextAsync(
                Path.Combine(cacheDir, "blueprints.json"),
                JsonSerializer.Serialize(blueprints, new JsonSerializerOptions { WriteIndented = true }));
            LogStatus($"Updated blueprints.json after caching blueprint {blueprintId}.");

        }catch (PrintifyApiException ex)
        {
            Console.Error.WriteLine($"\n  WARNING: Failed to fetch blueprint {blueprintId}: {ex.Message}");
        }
    }
}
else
{
    LogStatus("No missing blueprints detected.");
}

api = PrintifyBlueprintDatabase.CreateQueryApi(Path.Combine(cacheDir, "blueprint_details"));
LogPhase("Probe Product Index");
var existingProbeProductsByProvider = new Dictionary<(int BlueprintId, int ProviderId), List<(Product Product, int PageNumber, HashSet<string> CombinationKeys, bool HasMalformedDescription)>>();
foreach (var product in products)
{
    if (!TryParseProbeProduct(product, out var parsedProduct))
    {
        continue;
    }

    var providerKey = (parsedProduct.BlueprintId, parsedProduct.ProviderId);
    if (!existingProbeProductsByProvider.TryGetValue(providerKey, out var providerProducts))
    {
        providerProducts = new List<(Product Product, int PageNumber, HashSet<string> CombinationKeys, bool HasMalformedDescription)>();
        existingProbeProductsByProvider[providerKey] = providerProducts;
    }

    providerProducts.Add((product, parsedProduct.PageNumber, parsedProduct.CombinationKeys, parsedProduct.HasMalformedDescription));
}

foreach (var providerProducts in existingProbeProductsByProvider.Values)
{
    providerProducts.Sort((left, right) => left.PageNumber.CompareTo(right.PageNumber));
}
LogStatus($"Indexed {existingProbeProductsByProvider.Count} blueprint/provider probe product group(s).");

int MaxProbeVariantBatchSize = 100;
LogPhase("Provider Diff");
List<(int PID, int BlueprintId, List<int> VariantIds)> providersToRebuild = new List<(int PID, int BlueprintId, List<int> VariantIds)>();
var totalProviderCount = api.BlueprintIds.Sum(id => api.GetBlueprintDetail(id).PrintProviders.Count);
var scannedProviderCount = 0;
foreach (var blueprintId in api.BlueprintIds.OrderBy(id => id))
{
    var detail = api.GetBlueprintDetail(blueprintId);
    foreach (var providerDetail in detail.PrintProviders.OrderBy(provider => provider.Provider.Id))
    {
        scannedProviderCount++;
        if (scannedProviderCount == 1 || scannedProviderCount % 25 == 0 || scannedProviderCount == totalProviderCount)
        {
            LogStatus($"Checked {scannedProviderCount}/{totalProviderCount} blueprint/provider pair(s). Rebuilds queued: {providersToRebuild.Count}.");
        }

        if (providerDetail.Variants is null)
        {
            Console.WriteLine($"Skipping blueprint {blueprintId}, provider {providerDetail.Provider.Id} because variant data is missing from the cache.");
            continue;
        }

        var providerKey = (BlueprintId: blueprintId, ProviderId: providerDetail.Provider.Id);
        var desiredVariants = providerDetail.Variants.Variants;
        var desiredCombinationKeys = desiredVariants
            .Select(variant => BuildCombinationKey(blueprintId, providerDetail.Provider.Id, variant.Id, variant.Options))
            .ToHashSet(StringComparer.Ordinal);

        existingProbeProductsByProvider.TryGetValue(providerKey, out var existingProviderProducts);
        var existingCombinationCounts = new Dictionary<string, int>(StringComparer.Ordinal);
        var hasMalformedDescription = false;

        foreach (var existingProviderProduct in existingProviderProducts ?? Enumerable.Empty<(Product Product, int PageNumber, HashSet<string> CombinationKeys, bool HasMalformedDescription)>())
        {
            hasMalformedDescription |= existingProviderProduct.HasMalformedDescription;

            foreach (var combinationKey in existingProviderProduct.CombinationKeys)
            {
                existingCombinationCounts[combinationKey] = existingCombinationCounts.TryGetValue(combinationKey, out var count)
                    ? count + 1
                    : 1;
            }
        }

        var existingCombinationKeys = existingCombinationCounts.Keys.ToHashSet(StringComparer.Ordinal);
        var missingCount = desiredCombinationKeys.Except(existingCombinationKeys, StringComparer.Ordinal).Count();
        var staleCount = existingCombinationKeys.Except(desiredCombinationKeys, StringComparer.Ordinal).Count();
        var duplicateCount = existingCombinationCounts.Count(entry => entry.Value > 1);
        if (missingCount == 0 && staleCount == 0 && duplicateCount == 0 && !hasMalformedDescription)
        {
            continue;
        }

        Console.WriteLine(
            $"Rebuilding blueprint {blueprintId}, provider {providerDetail.Provider.Id}: " +
            $"{missingCount} missing, {staleCount} stale, {duplicateCount} duplicate, malformed description: {hasMalformedDescription}.");

        var providerVariantIds = desiredVariants
            .Select(variant => variant.Id)
            .Distinct()
            .ToList();
        if (providerVariantIds.Count == 0)
        {
            continue;
        }

        providersToRebuild.Add((providerDetail.Provider.Id, blueprintId, providerVariantIds));
    }
}
LogStatus($"Queued {providersToRebuild.Count} provider rebuild(s).");

LogPhase("Cleanup");
var totalProductsToDelete = providersToRebuild
    .Select(providerToRebuild => existingProbeProductsByProvider.TryGetValue((providerToRebuild.BlueprintId, providerToRebuild.PID), out var existingProviderProducts)
        ? existingProviderProducts.Count
        : 0)
    .Sum();
LogStatus($"Deleting {totalProductsToDelete} stale probe product page(s).");
var deletedProducts = 0;
foreach (var providerToRebuild in providersToRebuild)
{
    if (!existingProbeProductsByProvider.TryGetValue((providerToRebuild.BlueprintId, providerToRebuild.PID), out var existingProviderProducts))
    {
        continue;
    }

    foreach (var existingProviderProduct in existingProviderProducts)
    {
        var product = existingProviderProduct.Product;

        try
        {
            await client.DeleteProductAsync(shop.Id, product.Id);
            products.RemoveAll(existing => string.Equals(existing.Id, product.Id, StringComparison.OrdinalIgnoreCase));
            deletedProducts++;
            Console.WriteLine($"Deleted product {product.Id} with title {product.Title} for blueprint {providerToRebuild.BlueprintId} and provider {providerToRebuild.PID}.");
        }
        catch (PrintifyApiException ex)
        {
            Console.Error.WriteLine($"Failed to delete product {product.Id} with title {product.Title} for blueprint {providerToRebuild.BlueprintId} and provider {providerToRebuild.PID}: {ex.Message}");
        }
    }
}

List<(int PID, int BlueprintId, List<int>)> providersToPublish = new List<(int PID, int BlueprintId, List<int>)>();
foreach (var providerToRebuild in providersToRebuild)
{
    foreach (var batch in providerToRebuild.VariantIds.Chunk(MaxProbeVariantBatchSize))
    {
        providersToPublish.Add((providerToRebuild.PID, providerToRebuild.BlueprintId, batch.ToList()));
    }
}
LogStatus($"Prepared {providersToPublish.Count} probe product page(s) to create.");

//make sure a template image is uploaded to the shop for the product variants to use, and get its id

//create the full list of products to publish with the title, description and tags as described above, and publish them as drafts only, then i can start to harvest the images of the variants and the production cost of each product and variant
Dictionary<string,int> bpidandpidPageNumberTracker = new Dictionary<string,int>();
var newProducts = new List<Product>();
int index = 0;
UploadedImage? probeImage = null;

if (providersToPublish.Count == 0)
{
    Console.WriteLine("All cache probe products already match the current blueprint/provider variant combinations.");
}
else
{
    LogPhase("Rebuild");
    try
    {
        LogStatus($"Uploading probe image {PricingProductProbeImageFileName}.");
        probeImage = await client.UploadImageByUrlAsync(PricingProductProbeImageFileName, ProbeImageUrl);
        LogStatus($"Uploaded probe image {probeImage.Id}. Starting product creation.");

        foreach(var item in providersToPublish)
        {
            var blueprintId = item.BlueprintId;
            var providerId = item.PID;
            var variantIds = item.Item3;
            var detail = api.GetBlueprintDetail(blueprintId);
            var providerDetail = detail.PrintProviders.First(p => p.Provider.Id == providerId);
            var subvariants = api.GetSubvariants(blueprintId, providerId)
                .Where(s => variantIds.Contains(s.VariantId))
                .ToList();
            //create the product title, description and tags
            var pageNumberKey = $"{blueprintId}-{providerId}";
            if (!bpidandpidPageNumberTracker.ContainsKey(pageNumberKey))
            {
                bpidandpidPageNumberTracker[pageNumberKey] = 1;
            }
            var Pnum = bpidandpidPageNumberTracker[pageNumberKey]++;
            var productTitle = $"{Pnum}-{blueprintId}-{providerId}";
            var productDescription = string.Join(Environment.NewLine, subvariants.Select(s => $"{s.VariantId}-{FormatSubvariantOptions(s.Options)}"));
            var productTags = new List<string> { $"1-{blueprintId}-{providerId}" };
            LogStatus($"Creating probe page {index + 1}/{providersToPublish.Count} for blueprint {blueprintId}, provider {providerId}, page {Pnum} with {subvariants.Count} variant(s).");

            try
            {
                var createdProduct = await client.CreateProductAsync(shop.Id, new CreateProductRequest
                {
                    Title = productTitle,
                    Description = productDescription,
                    BlueprintId = blueprintId,
                    PrintProviderId = providerId,
                    Tags = productTags,
                    Variants = subvariants.Select(s => new CreateProductVariant
                    {
                        Id = s.VariantId,
                        Price = 20000,
                        IsEnabled = true
                    }).ToList(),
                    PrintAreas = BuildProbePrintAreas(subvariants, probeImage.Id),
                });
                createdProduct = await WaitForProbeProductImagesAsync(client, shop.Id, createdProduct);
                newProducts.Add(createdProduct);

                PrintLoadingBar(++index, providersToPublish.Count);
                Console.WriteLine($"Created draft product {createdProduct.Id} with title {createdProduct.Title} for provider {providerId} with blueprint {blueprintId}.");
                Console.CursorTop -=2;
            }
            catch (PrintifyApiException ex)
            {
                Console.Error.WriteLine($"Failed to create draft product for provider {providerId} with blueprint {blueprintId}: {ex.Message}");
                Console.WriteLine();
            }
        }
    }
    finally
    {
        if (probeImage is not null)
        {
            LogStatus($"Archiving probe image {probeImage.Id}.");
            await ArchiveProbeUploadSafeAsync(client, probeImage.Id);
        }
    }
}

//group back together all the variant ids by provider and blueprint so it can be checked together
LogPhase("Cache Write");
Dictionary<(int BlueprintId, int ProviderId), List<ProductVariant>> cachedVariantsPerProvider = new ();
Dictionary<(int BlueprintId, int ProviderId,int variationid),List<string>> imagesUrlsCached = new ();
foreach (var product in newProducts)
{
    var key = (product.BlueprintId, product.PrintProviderId);
    if (!cachedVariantsPerProvider.ContainsKey(key))
    {
        cachedVariantsPerProvider[key] = new List<ProductVariant>();
    }
    cachedVariantsPerProvider[key].AddRange(product.Variants);

    AddProductImagesToCache(product, imagesUrlsCached);
}
//check if the files exists for existing products and if not, add them to the cache as well
foreach (var product in products)
{
    var key = (product.BlueprintId, product.PrintProviderId);
    var blueprintId = product.BlueprintId;
    var providerId = product.PrintProviderId;
    var priceCachePath = Path.Combine(cacheDir, "variants","prices", $"{blueprintId}-{providerId}.json");
    Product? fullProduct = null;

    if (!HasUsableVariantPriceCache(priceCachePath))
    {
        var productWithPricing = product;
        if (!HasAnyVariantCosts(productWithPricing))
        {
            fullProduct ??= await LoadFullProductSafeAsync(client, shop.Id, productWithPricing);
            productWithPricing = fullProduct;
        }

        if (!cachedVariantsPerProvider.ContainsKey(key))
        {
            cachedVariantsPerProvider[key] = new List<ProductVariant>();
        }

        cachedVariantsPerProvider[key].AddRange(productWithPricing.Variants);
    }

    var imageCachePath = Path.Combine(cacheDir, "variants","images", $"{blueprintId}-{providerId}.json");
    if (!File.Exists(imageCachePath))
    {
        var productWithImages = product;
        if ((productWithImages.Images?.Count ?? 0) == 0)
        {
            fullProduct ??= await LoadFullProductSafeAsync(client, shop.Id, productWithImages);
            productWithImages = fullProduct;
        }

        AddProductImagesToCache(productWithImages, imagesUrlsCached);
    }

}
LogStatus($"Collected variant cache data for {cachedVariantsPerProvider.Count} provider pair(s).");
//i want the same structure as the prices cache 
//Cached/variants/images/BPID-PID.json with the image url for each variant in a json array object like this: { "id": 123, "ImageUrls": ["url1", "url2"] }
var grouped = imagesUrlsCached.GroupBy(kv => (kv.Key.BlueprintId, kv.Key.ProviderId)).ToList();
var variantsImgesToCachePerProvider = new Dictionary<(int BlueprintId, int ProviderId), List<(int vid, List<string> imageUrls)>>();

foreach(var group in grouped)
{
        var blueprintId = group.Key.BlueprintId;
        var providerId = group.Key.ProviderId;
        var variantId = group.ToArray()[0].Key.variationid;
        var cachePath = Path.Combine(cacheDir, "variants","images", $"{blueprintId}-{providerId}.json");
        if (!File.Exists(cachePath))
        {
            if(!variantsImgesToCachePerProvider.ContainsKey((blueprintId, providerId)))
            {
                variantsImgesToCachePerProvider[(blueprintId, providerId)] = new List<(int vid, List<string> imageUrls)>();
            }
            variantsImgesToCachePerProvider[(blueprintId, providerId)].AddRange(group.Select(g => (g.Key.variationid, g.Value)).ToList());
        }
}
    LogStatus($"Prepared {variantsImgesToCachePerProvider.Count} image cache file(s) to write.");

// same them to a cahe file in Cached/vaiants/prices/BPID-PID.json with the variant id, options and image url for each variant, so it can be used later to check if the variants have the correct images and options in the cache
foreach (var key in cachedVariantsPerProvider.Keys)
{
    var blueprintId = key.BlueprintId;
    var providerId = key.ProviderId;
    var variants = cachedVariantsPerProvider[key]
        .GroupBy(variant => variant.Id)
        .Select(group => group.First())
        .ToList();
    if (variants.Count == 0)
    {
        continue;
    }

    var cachePath = Path.Combine(cacheDir, "variants","prices", $"{blueprintId}-{providerId}.json");
    var cacheDirectory = Path.GetDirectoryName(cachePath);
    if (!string.IsNullOrWhiteSpace(cacheDirectory))
    {
        Directory.CreateDirectory(cacheDirectory);
    }
    await File.WriteAllTextAsync(cachePath, JsonSerializer.Serialize(variants, new JsonSerializerOptions { WriteIndented = true }));
    Console.WriteLine($"Saved {variants.Count} variants for blueprint {blueprintId} and provider {providerId} to cache.");
}

//save the list of images to a cache file in Cached/variant/images/BPID-PID.json with the image url for each variant, so it can be used later to check if the variants have the correct images in the cache   
foreach (var key in variantsImgesToCachePerProvider.Keys)
{
    var blueprintId = key.BlueprintId;
    var providerId = key.ProviderId;
    var cachePath = Path.Combine(cacheDir, "variants","images", $"{blueprintId}-{providerId}.json");
    var cacheDirectory = Path.GetDirectoryName(cachePath);
    var variantImageEntries = variantsImgesToCachePerProvider[key]
        .Select(entry => new
        {
            id = entry.vid,
            urls = entry.imageUrls
        })
        .ToList();

    if (!string.IsNullOrWhiteSpace(cacheDirectory))
    {
        Directory.CreateDirectory(cacheDirectory);
    }
    await File.WriteAllTextAsync(cachePath, JsonSerializer.Serialize(variantImageEntries, new JsonSerializerOptions { WriteIndented = true }));
    Console.WriteLine($"Saved {variantImageEntries.Count} image urls for blueprint {blueprintId}, provider {providerId} to cache.");
}

LogPhase("Complete");
LogStatus($"Run complete. Deleted {deletedProducts} product(s), created {newProducts.Count} product(s), wrote {cachedVariantsPerProvider.Count} variant cache file(s), wrote {variantsImgesToCachePerProvider.Count} image cache file(s).");




void PrintLoadingBar(int current, int total)
{
    int barWidth = 50;
    double progress = (double)current / total;
    int filledWidth = (int)(progress * barWidth);
    string bar = $"[{new string('#', filledWidth)}{new string('-', barWidth - filledWidth)}] {current}/{total}";
    Console.WriteLine($"\r{bar}");
}

void LogPhase(string phase)
{
    Console.WriteLine();
    Console.WriteLine($"[{DateTime.UtcNow:O}] {phase}");
}

void LogStatus(string message)
{
    var elapsed = DateTime.UtcNow - runStartedAt;
    Console.WriteLine($"  [{elapsed:mm\\:ss}] {message}");
}

static bool TryParseProbeProduct(
    Product product,
    out (int BlueprintId, int ProviderId, int PageNumber, HashSet<string> CombinationKeys, bool HasMalformedDescription) parsedProduct)
{
    parsedProduct = default;

    if (!TryParseProbeProductIdentity(product, out var blueprintId, out var providerId, out var pageNumber))
    {
        return false;
    }

    var combinationKeys = new HashSet<string>(StringComparer.Ordinal);
    var hasMalformedDescription = false;
    foreach (var line in SplitDescriptionLines(product.Description))
    {
        if (TryBuildCombinationKeyFromDescriptionLine(blueprintId, providerId, line, out var combinationKey))
        {
            combinationKeys.Add(combinationKey);
            continue;
        }

        hasMalformedDescription = true;
    }

    parsedProduct = (blueprintId, providerId, pageNumber, combinationKeys, hasMalformedDescription);
    return true;
}

static bool TryParseProbeProductIdentity(Product product, out int blueprintId, out int providerId, out int pageNumber)
{
    blueprintId = 0;
    providerId = 0;
    pageNumber = 0;

    var titleParts = (product.Title ?? string.Empty).Split('-', StringSplitOptions.TrimEntries);
    if (titleParts.Length == 3 &&
        int.TryParse(titleParts[0], out pageNumber) &&
        int.TryParse(titleParts[1], out blueprintId) &&
        int.TryParse(titleParts[2], out providerId))
    {
        return true;
    }

    foreach (var tag in product.Tags ?? Enumerable.Empty<string>())
    {
        var tagParts = tag.Split('-', StringSplitOptions.TrimEntries);
        if (tagParts.Length == 3 &&
            int.TryParse(tagParts[0], out var tagPrefix) &&
            tagPrefix == 1 &&
            int.TryParse(tagParts[1], out blueprintId) &&
            int.TryParse(tagParts[2], out providerId))
        {
            pageNumber = 0;
            return true;
        }
    }

    return false;
}

static IEnumerable<string> SplitDescriptionLines(string? description)
{
    return (description ?? string.Empty)
        .Split(new[] { "\r\n", "\n\r", "\n", "\r" }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
        .Where(line => !string.IsNullOrWhiteSpace(line));
}

static bool TryBuildCombinationKeyFromDescriptionLine(int blueprintId, int providerId, string line, out string combinationKey)
{
    combinationKey = string.Empty;

    var separatorIndex = line.IndexOf('-');
    if (separatorIndex <= 0)
    {
        return false;
    }

    if (!int.TryParse(line[..separatorIndex], out var variantId))
    {
        return false;
    }

    var optionsText = line[(separatorIndex + 1)..].Trim();
    combinationKey = $"{blueprintId}-{providerId}-{variantId}-{NormalizeOptionsText(optionsText)}";
    return true;
}

static string BuildCombinationKey(int blueprintId, int providerId, int variantId, Dictionary<string, object>? options)
{
    return $"{blueprintId}-{providerId}-{variantId}-{FormatVariantOptions(options)}";
}

static string FormatSubvariantOptions(IReadOnlyDictionary<string, string>? options)
{
    if (options is null || options.Count == 0)
    {
        return "no_options";
    }

    return string.Join(
        ",",
        options
            .OrderBy(option => option.Key, StringComparer.OrdinalIgnoreCase)
            .ThenBy(option => option.Value, StringComparer.Ordinal)
            .Select(option => $"{option.Key}={option.Value}"));
}

static string FormatVariantOptions(Dictionary<string, object>? options)
{
    if (options is null || options.Count == 0)
    {
        return "no_options";
    }

    return string.Join(
        ",",
        options
            .OrderBy(option => option.Key, StringComparer.OrdinalIgnoreCase)
            .ThenBy(option => NormalizeOptionValue(option.Value), StringComparer.Ordinal)
            .Select(option => $"{option.Key}={NormalizeOptionValue(option.Value)}"));
}

static string NormalizeOptionsText(string? optionsText)
{
    if (string.IsNullOrWhiteSpace(optionsText) || string.Equals(optionsText, "no_options", StringComparison.OrdinalIgnoreCase))
    {
        return "no_options";
    }

    var normalizedOptions = optionsText
        .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
        .Select(option =>
        {
            var separatorIndex = option.IndexOf('=');
            return separatorIndex >= 0
                ? (Key: option[..separatorIndex].Trim(), Value: option[(separatorIndex + 1)..].Trim())
                : (Key: option.Trim(), Value: string.Empty);
        })
        .OrderBy(option => option.Key, StringComparer.OrdinalIgnoreCase)
        .ThenBy(option => option.Value, StringComparer.Ordinal)
        .ToList();

    return normalizedOptions.Count == 0
        ? "no_options"
        : string.Join(
            ",",
            normalizedOptions.Select(option => $"{option.Key}={option.Value}"));
}

static string NormalizeOptionValue(object? value)
{
    return value switch
    {
        null => string.Empty,
        JsonElement element when element.ValueKind == JsonValueKind.Null || element.ValueKind == JsonValueKind.Undefined => string.Empty,
        JsonElement element when element.ValueKind == JsonValueKind.String => element.GetString() ?? string.Empty,
        JsonElement element => element.ToString(),
        _ => value.ToString() ?? string.Empty
    };
}

async Task<Product> WaitForProbeProductImagesAsync(PrintifyClient printifyClient, int shopId, Product product)
{
    var current = product;
    if ((current.Images?.Count ?? 0) > 0)
    {
        LogStatus($"Product {current.Id} already has mockup images.");
        return current;
    }

    LogStatus($"Waiting for mockup images for product {current.Id} ({current.Title}).");

    for (var attempt = 0; attempt < 5; attempt++)
    {
        await Task.Delay(TimeSpan.FromSeconds(1.5));
        LogStatus($"Polling images for product {current.Id}: attempt {attempt + 1}/5.");
        current = await printifyClient.GetProductAsync(shopId, current.Id);

        if ((current.Images?.Count ?? 0) > 0)
        {
            LogStatus($"Mockup images ready for product {current.Id}.");
            return current;
        }
    }

    LogStatus($"Mockup images were still unavailable after polling product {current.Id}.");

    return current;
}

async Task<Product> LoadFullProductSafeAsync(PrintifyClient printifyClient, int shopId, Product product)
{
    try
    {
        return await printifyClient.GetProductAsync(shopId, product.Id);
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Failed to load full details for existing product {product.Id}: {ex.Message}");
        return product;
    }
}

static bool HasAnyVariantCosts(Product product)
{
    return product.Variants.Any(variant => variant.Cost > 0);
}

static bool HasUsableVariantPriceCache(string cachePath)
{
    if (!File.Exists(cachePath))
    {
        return false;
    }

    try
    {
        using var stream = File.OpenRead(cachePath);
        var cachedVariants = JsonSerializer.Deserialize<List<ProductVariant>>(stream, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            ReadCommentHandling = JsonCommentHandling.Skip,
            AllowTrailingCommas = true
        });

        return cachedVariants?.Any(variant => variant.Id > 0 && variant.Cost > 0) == true;
    }
    catch
    {
        return false;
    }
}

static void AddProductImagesToCache(
    Product product,
    Dictionary<(int BlueprintId, int ProviderId, int variationid), List<string>> imagesUrlsCached)
{
    foreach (var image in product.Images?.OfType<ProductMockupImage>() ?? Enumerable.Empty<ProductMockupImage>())
    {
        var imageUrl = image.Src?.Trim();
        if (string.IsNullOrWhiteSpace(imageUrl))
        {
            continue;
        }

        foreach (var variantId in image.VariantIds.Distinct())
        {
            var key = (product.BlueprintId, product.PrintProviderId, variantId);
            if (!imagesUrlsCached.TryGetValue(key, out var imageUrls))
            {
                imageUrls = new List<string>();
                imagesUrlsCached[key] = imageUrls;
            }

            if (!imageUrls.Contains(imageUrl))
            {
                imageUrls.Add(imageUrl);
            }
        }
    }
}

async Task ArchiveProbeUploadSafeAsync(PrintifyClient printifyClient, string imageId)
{
    try
    {
        await printifyClient.ArchiveUploadAsync(imageId);
        Console.WriteLine($"Archived probe image {imageId}.");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Failed to archive probe image {imageId}: {ex.Message}");
    }
}

static string BuildPlaceholderSignature(IReadOnlyList<VariantPlaceholder> placeholders)
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
static List<PrintArea> BuildProbePrintAreas(
    IReadOnlyList<PrintifyBlueprintSubvariant> subvariants,
    string uploadedImageId)
{
    return subvariants
        .GroupBy(subvariant => BuildPlaceholderSignature(subvariant.Placeholders), StringComparer.Ordinal)
        .Select(group =>
        {
            var exemplar = group.First();

            return new PrintArea
            {
                VariantIds = group
                    .Select(subvariant => subvariant.VariantId)
                    .Distinct()
                    .ToList(),
                Placeholders = exemplar.Placeholders
                    .Select(placeholder => new PrintAreaPlaceholder
                    {
                        Position = placeholder.Position,
                        DecorationMethod = string.IsNullOrWhiteSpace(placeholder.DecorationMethod)
                            ? null
                            : placeholder.DecorationMethod,
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
                    })
                    .ToList()
            };
        })
        .ToList();
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
return 0;
/*
const string ProbeImageUrl = "https://dummyimage.com/1200x1200/f6efe3/122029.png&text=Printify+Variant+Image+Probe";
const string PricingProductProbeImageFileName = "pricing-product-probe.png";
const string PricingProductCacheTag = "printify-generator-pricing-cache";
const string PricingProductDescription = "Published pricing lookup product created by PrintifyGenerator.CacheGenerator and kept for later production and shipping price checks.";
const int VariantProbeIntervalMs = 450;
const int MaxProbeVariantBatchSize = 100;
const int PublishedProductRequestLimitPerMinute = 180;
const int PricingProductListPageSize = 50;
const int PricingProductRetailPrice = 2000;

var repositoryRoot = ResolveRepositoryRoot();
if (repositoryRoot is null)
{
    Console.Error.WriteLine("Could not locate the repository root from the current working directory.");
    return 1;
}

Directory.SetCurrentDirectory(repositoryRoot);

if (HasFlag(args, "--help") || HasFlag(args, "-h"))
{
    await PrintUsage(repositoryRoot);
    return 0;
}

if (!TryReadIntOption(args, "--blueprint", out var blueprintFilter, out var optionError) ||
    !TryReadIntOption(args, "--limit", out var limit, out optionError) ||
    !TryReadIntOption(args, "--shop-id", out var shopIdOverride, out optionError))
{

    Console.Error.WriteLine(optionError);
    return 1;
}

var mode = ReadMode(args);
var force = HasFlag(args, "--force");
var resetCache = HasFlag(args, "--reset-cache");

return mode switch
{
    "catalog" => await GenerateCatalogCacheAsync(repositoryRoot, blueprintFilter, limit),
    "variant-images" => await GenerateVariantImageCacheAsync(repositoryRoot, blueprintFilter, limit, force, shopIdOverride),
    "pricing-products" => await GeneratePricingProductCacheAsync(repositoryRoot, blueprintFilter, limit, shopIdOverride, resetCache),
    "publish-products" => await GeneratePricingProductCacheAsync(repositoryRoot, blueprintFilter, limit, shopIdOverride, resetCache),
    "all" => await GenerateAllCachesAsync(repositoryRoot, blueprintFilter, limit, force, shopIdOverride),
    _ => await UnknownMode(mode, repositoryRoot)
};

static async Task<int> GenerateAllCachesAsync(string repositoryRoot, int? blueprintFilter, int? limit, bool force, int? shopIdOverride)
{
    var catalogExitCode = await GenerateCatalogCacheAsync(repositoryRoot, blueprintFilter, limit);
    if (catalogExitCode != 0)
    {
        return catalogExitCode;
    }

    return await GenerateVariantImageCacheAsync(repositoryRoot, blueprintFilter, limit, force, shopIdOverride);
}

static async Task<int> GenerateCatalogCacheAsync(string repositoryRoot, int? blueprintFilter, int? limit)
{
    var envFilePath = Path.Combine(repositoryRoot, "main.env");
    var token = ReadToken(envFilePath);
    if (string.IsNullOrWhiteSpace(token))
    {
        Console.Error.WriteLine("TOKEN was not found in main.env.");
        return 1;
    }

    var client = new PrintifyClient(token);
    var cacheDir = Path.Combine(repositoryRoot, "src", "data", "Cached");
    Directory.CreateDirectory(cacheDir);

    var jsonOpts = new JsonSerializerOptions { WriteIndented = true };

    Console.WriteLine("Fetching blueprints...");
    var blueprints = await client.GetBlueprintsAsync();
    blueprints = blueprints
        .OrderBy(blueprint => blueprint.Id)
        .ToList();

    if (blueprintFilter.HasValue)
    {
        blueprints = blueprints
            .Where(blueprint => blueprint.Id == blueprintFilter.Value)
            .ToList();
    }

    if (limit.HasValue)
    {
        blueprints = blueprints
            .Take(limit.Value)
            .ToList();
    }

    await File.WriteAllTextAsync(
        Path.Combine(cacheDir, "blueprints.json"),
        JsonSerializer.Serialize(blueprints, jsonOpts));
    Console.WriteLine($"  Cached {blueprints.Count} blueprints.");

    Console.WriteLine("Fetching print providers...");
    var printProviders = await client.GetPrintProvidersAsync();
    await File.WriteAllTextAsync(
        Path.Combine(cacheDir, "print_providers.json"),
        JsonSerializer.Serialize(printProviders, jsonOpts));
    Console.WriteLine($"  Cached {printProviders.Count} print providers.");

    var blueprintDetailsDir = Path.Combine(cacheDir, "blueprint_details");
    Directory.CreateDirectory(blueprintDetailsDir);

    var requestCount = 0;
    var stopwatch = System.Diagnostics.Stopwatch.StartNew();

    async Task ThrottleAsync()
    {
        requestCount++;

        if (requestCount % 90 != 0)
        {
            return;
        }

        var elapsed = stopwatch.Elapsed;
        if (elapsed.TotalSeconds < 60)
        {
            var delay = TimeSpan.FromSeconds(62) - elapsed;
            Console.WriteLine($"  Catalog rate-limit pause: waiting {delay.TotalSeconds:F0}s ...");
            await Task.Delay(delay);
        }

        stopwatch.Restart();
        requestCount = 0;
    }

    Console.WriteLine($"Fetching per-blueprint details for {blueprints.Count} blueprints...");

    for (var index = 0; index < blueprints.Count; index++)
    {
        var blueprint = blueprints[index];
        Console.Write($"\r  [{index + 1}/{blueprints.Count}] Blueprint {blueprint.Id} - {blueprint.Title}                    ");

        try
        {
            await ThrottleAsync();
            var providers = await client.GetBlueprintPrintProvidersAsync(blueprint.Id);
            var providerDetails = new List<object>();

            foreach (var provider in providers)
            {
                await ThrottleAsync();
                VariantResponse? variants = null;
                try
                {
                    variants = await client.GetBlueprintVariantsAsync(blueprint.Id, provider.Id, showOutOfStock: true);
                }
                catch (PrintifyApiException ex) when (ex.StatusCode == 404)
                {
                    Console.WriteLine($"\n    Variants missing for blueprint {blueprint.Id}, provider {provider.Id}: {ex.Message}");
                }

                await ThrottleAsync();
                ShippingInfo? shipping = null;
                try
                {
                    shipping = await client.GetBlueprintShippingAsync(blueprint.Id, provider.Id);
                }
                catch (PrintifyApiException ex) when (ex.StatusCode == 404)
                {
                    Console.WriteLine($"\n    Shipping missing for blueprint {blueprint.Id}, provider {provider.Id}: {ex.Message}");
                }

                providerDetails.Add(new
                {
                    provider,
                    variants,
                    shipping,
                    cost_per_region = shipping?.Profiles
                        .Select(profile => new
                        {
                            countries = profile.Countries,
                            first_item = profile.FirstItem,
                            additional_items = profile.AdditionalItems,
                            variant_count = profile.VariantIds.Count
                        })
                        .ToList()
                });
            }

            var detail = new
            {
                blueprint,
                print_providers = providerDetails
            };

            await File.WriteAllTextAsync(
                Path.Combine(blueprintDetailsDir, $"{blueprint.Id}.json"),
                JsonSerializer.Serialize(detail, jsonOpts));
        }
        catch (PrintifyApiException ex)
        {
            Console.Error.WriteLine($"\n  WARNING: Failed blueprint {blueprint.Id}: {ex.Message}");
        }
    }

    Console.WriteLine();
    Console.WriteLine("Fetching shops...");
    var shops = await client.GetShopsAsync();
    await File.WriteAllTextAsync(
        Path.Combine(cacheDir, "shops.json"),
        JsonSerializer.Serialize(shops, jsonOpts));
    Console.WriteLine($"  Cached {shops.Count} shops.");

    Console.WriteLine("Catalog cache generation complete.");
    return 0;
}

static async Task<int> GenerateVariantImageCacheAsync(string repositoryRoot, int? blueprintFilter, int? limit, bool force, int? shopIdOverride)
{
    var envFilePath = Path.Combine(repositoryRoot, "main.env");
    var token = ReadToken(envFilePath);
    if (string.IsNullOrWhiteSpace(token))
    {
        Console.Error.WriteLine("TOKEN was not found in main.env.");
        return 1;
    }

    var cacheDir = Path.Combine(repositoryRoot, "src", "data", "Cached");
    var blueprintDetailsDir = Path.Combine(cacheDir, "blueprint_details");
    if (!Directory.Exists(blueprintDetailsDir))
    {
        Console.Error.WriteLine($"Blueprint details directory not found: {blueprintDetailsDir}");
        Console.Error.WriteLine("Run the catalog cache pass first.");
        return 1;
    }

    Directory.CreateDirectory(cacheDir);

    var client = new PrintifyClient(token);
    var api = PrintifyBlueprintDatabase.CreateQueryApi(blueprintDetailsDir);
    var variantImageCachePath = Path.Combine(cacheDir, "variant_images.json");
    var document = LoadVariantImageCacheDocument(variantImageCachePath);

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

    var providerEstimate = blueprintIds.Sum(id => api.GetProviders(id).Count());
    var shops = await client.GetShopsAsync();
    var shop = ResolveProbeShop(envFilePath, shopIdOverride, shops);
    if (shop is null)
    {
        Console.Error.WriteLine("No usable Printify shop was found for temporary draft-product variant image probing.");
        return 1;
    }

    Console.WriteLine($"Scanning {blueprintIds.Count} blueprint(s) and {providerEstimate} provider(s) for variant mockup images.");
    Console.WriteLine($"Using shop {shop.Id} ({shop.Title}) for temporary draft-product image probing.");
    Console.WriteLine(force
        ? "Force mode is enabled: cached provider image entries will be refreshed."
        : "Resume mode is enabled: providers with complete cached image coverage will be skipped.");

    UploadedImage? probeImage = null;
    var processedProviders = 0;
    var skippedProviders = 0;
    var failedProviders = 0;
    var cachedImages = 0;
    var providerIndex = 0;

    try
    {
        probeImage = await UploadProbeImageAsync(client);
        Console.WriteLine($"Uploaded probe image {probeImage.Id} for temporary draft product creation.");

        document.ShopId = shop.Id;
        document.ShopTitle = shop.Title;
        document.SampleImagePreviewUrl = probeImage.PreviewUrl;

        foreach (var blueprintId in blueprintIds)
        {
            var detail = api.GetBlueprintDetail(blueprintId);

            foreach (var providerDetail in detail.PrintProviders.OrderBy(provider => provider.Provider.Title, StringComparer.OrdinalIgnoreCase))
            {
                providerIndex++;
                var variants = providerDetail.Variants.Variants
                    .OrderBy(variant => variant.Id)
                    .ToList();

                if (variants.Count == 0)
                {
                    skippedProviders++;
                    continue;
                }

                var variantIds = variants.Select(variant => variant.Id).ToList();
                var cachedVariantCount = CountCachedVariants(document, blueprintId, providerDetail.Provider.Id, variantIds);
                if (!force && cachedVariantCount == variantIds.Count)
                {
                    skippedProviders++;
                    continue;
                }

                var subvariants = api.GetSubvariants(blueprintId, providerDetail.Provider.Id)
                    .Where(subvariant => subvariant.Placeholders.Count > 0)
                    .OrderBy(subvariant => subvariant.VariantId)
                    .ToList();

                if (subvariants.Count == 0)
                {
                    Console.WriteLine($"[{providerIndex}/{providerEstimate}] Blueprint {blueprintId} / Provider {providerDetail.Provider.Id} skipped: no placeholders available.");
                    skippedProviders++;
                    continue;
                }

                Console.WriteLine(
                    $"[{providerIndex}/{providerEstimate}] Blueprint {blueprintId} {detail.Blueprint.Title} | " +
                    $"Provider {providerDetail.Provider.Id} {providerDetail.Provider.Title} | " +
                    $"cached {cachedVariantCount}/{variantIds.Count}");

                try
                {
                    var entries = await LoadDraftProductVariantImageEntriesAsync(
                        client,
                        shop.Id,
                        blueprintId,
                        providerDetail.Provider.Id,
                        subvariants,
                        probeImage.Id);

                    if (entries.Count == 0)
                    {
                        Console.WriteLine("  No variant image URLs were returned for this provider.");
                        skippedProviders++;
                        continue;
                    }

                    MergeVariantImageEntries(document, entries);
                    document.GeneratedAtUtc = DateTime.UtcNow;
                    SaveVariantImageCacheDocument(variantImageCachePath, document);

                    processedProviders++;
                    cachedImages += entries.Count;

                    Console.WriteLine($"  Cached {entries.Count}/{variantIds.Count} variant image(s). Total cached entries: {document.Entries.Count}.");
                    await Task.Delay(VariantProbeIntervalMs);
                }
                catch (Exception ex)
                {
                    failedProviders++;
                    Console.WriteLine($"  Provider probe failed: {ex.Message}");
                }
            }
        }
    }
    finally
    {
        if (probeImage is not null)
        {
            await ArchiveProbeImageSafeAsync(client, probeImage.Id);
        }
    }

    Console.WriteLine();
    Console.WriteLine("Variant image cache generation complete.");
    Console.WriteLine($"Providers processed: {processedProviders}");
    Console.WriteLine($"Providers skipped: {skippedProviders}");
    Console.WriteLine($"Providers failed: {failedProviders}");
    Console.WriteLine($"Variant image entries captured this run: {cachedImages}");
    Console.WriteLine($"Cache file: {variantImageCachePath}");

    return failedProviders > 0 && processedProviders == 0 ? 1 : 0;
}

static async Task<int> GeneratePricingProductCacheAsync(string repositoryRoot, int? blueprintFilter, int? limit, int? shopIdOverride, bool resetCache)
{
    var envFilePath = Path.Combine(repositoryRoot, "main.env");
    var token = ReadToken(envFilePath);
    if (string.IsNullOrWhiteSpace(token))
    {
        Console.Error.WriteLine("TOKEN was not found in main.env.");
        return 1;
    }

    var cacheDir = Path.Combine(repositoryRoot, "src", "data", "Cached");
    var blueprintDetailsDir = Path.Combine(cacheDir, "blueprint_details");
    if (!Directory.Exists(blueprintDetailsDir))
    {
        Console.Error.WriteLine($"Blueprint details directory not found: {blueprintDetailsDir}");
        Console.Error.WriteLine("Run the catalog cache pass first.");
        return 1;
    }

    Directory.CreateDirectory(cacheDir);

    var client = new PrintifyClient(token);
    var api = PrintifyBlueprintDatabase.CreateQueryApi(blueprintDetailsDir);
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

    var shops = await client.GetShopsAsync();
    var shop = ResolvePublishingShop(envFilePath, shopIdOverride, shops);
    if (shop is null)
    {
        Console.Error.WriteLine("No usable Printify shop was found for pricing-product publishing.");
        Console.Error.WriteLine("Set SHOP_ID, PRICE_UPDATER_SHOP_ID, or pass --shop-id to target a publishable shop.");
        return 1;
    }

    var cachePath = Path.Combine(cacheDir, $"pricing_products_shop_{shop.Id}.json");
    var document = LoadPricingProductCacheDocument(cachePath);
    document.ShopId = shop.Id;
    document.ShopTitle = shop.Title;

    var plans = BuildPricingProductPlans(api, blueprintIds);
    if (plans.Count == 0)
    {
        Console.Error.WriteLine("No pricing-product pages could be built from the cached blueprint details.");
        return 1;
    }

    Console.WriteLine($"Preparing {plans.Count} pricing product page(s) across {blueprintIds.Count} blueprint(s).");
    Console.WriteLine($"Using shop {shop.Id} ({shop.Title}) for published pricing products.");
    Console.WriteLine(resetCache
        ? "Reset mode is enabled: matching published pricing products will be deleted and recreated."
        : "Resume mode is enabled: matching products already in the target shop will be reused.");

    if (string.Equals(shop.SalesChannel, "custom_integration", StringComparison.OrdinalIgnoreCase))
    {
        Console.WriteLine("WARNING: the selected shop uses custom_integration. Publishing may not be supported by that sales channel.");
    }

    var requestCount = 0;
    var stopwatch = System.Diagnostics.Stopwatch.StartNew();

    async Task ThrottleAsync()
    {
        requestCount++;

        if (requestCount % PublishedProductRequestLimitPerMinute != 0)
        {
            return;
        }

        var elapsed = stopwatch.Elapsed;
        if (elapsed.TotalSeconds < 60)
        {
            var delay = TimeSpan.FromSeconds(62) - elapsed;
            Console.WriteLine($"  Pricing-product rate-limit pause: waiting {delay.TotalSeconds:F0}s ...");
            await Task.Delay(delay);
        }

        stopwatch.Restart();
        requestCount = 0;
    }

    var existingProductsByTitle = await LoadExistingProductsByTitleAsync(client, shop.Id, ThrottleAsync);

    if (resetCache)
    {
        var deletedCount = await DeleteMatchingPricingProductsAsync(client, shop.Id, plans, existingProductsByTitle, ThrottleAsync);
        document = new PrintifyPricingProductCacheDocument
        {
            ShopId = shop.Id,
            ShopTitle = shop.Title
        };
        SavePricingProductCacheDocument(cachePath, document);
        existingProductsByTitle = await LoadExistingProductsByTitleAsync(client, shop.Id, ThrottleAsync);
        Console.WriteLine($"Deleted {deletedCount} matching pricing product(s) before rebuilding.");
    }

    var probeImage = await EnsurePricingProbeUploadAsync(client, document, ThrottleAsync);
    document.GeneratedAtUtc = DateTime.UtcNow;
    SavePricingProductCacheDocument(cachePath, document);

    var createdPages = 0;
    var resumedPages = 0;
    var failedPages = 0;

    for (var index = 0; index < plans.Count; index++)
    {
        var plan = plans[index];
        Console.WriteLine($"[{index + 1}/{plans.Count}] {plan.Title} | {plan.Subvariants.Count} variant(s)");

        if (TryFindExistingPricingProduct(plan, existingProductsByTitle, out var existingProduct, out var variantMismatch))
        {
            try
            {
                if (!existingProduct.Visible)
                {
                    Console.WriteLine($"  Found existing draft product {existingProduct.Id}; publishing it now.");
                    await EnsurePricingProductPublishedAsync(client, shop.Id, existingProduct, ThrottleAsync);
                    existingProduct.Visible = true;
                }
                else
                {
                    Console.WriteLine($"  Reusing existing published product {existingProduct.Id}.");
                }

                MergePricingProductCacheEntry(document, CreatePricingProductCacheEntry(plan, existingProduct));
                document.GeneratedAtUtc = DateTime.UtcNow;
                SavePricingProductCacheDocument(cachePath, document);

                resumedPages++;
                await Task.Delay(VariantProbeIntervalMs);
            }
            catch (Exception ex)
            {
                failedPages++;
                Console.WriteLine($"  Existing product publish failed: {ex.Message}");
            }

            continue;
        }

        if (variantMismatch)
        {
            failedPages++;
            Console.WriteLine("  Existing product with this title has a different variant set. Re-run with --reset-cache to rebuild it.");
            continue;
        }

        try
        {
            var createdProduct = await CreateAndPublishPricingProductAsync(client, shop.Id, plan, probeImage.Id, ThrottleAsync);
            AddIndexedProduct(existingProductsByTitle, createdProduct);

            MergePricingProductCacheEntry(document, CreatePricingProductCacheEntry(plan, createdProduct));
            document.GeneratedAtUtc = DateTime.UtcNow;
            SavePricingProductCacheDocument(cachePath, document);

            createdPages++;
            Console.WriteLine($"  Published product {createdProduct.Id}.");
            await Task.Delay(VariantProbeIntervalMs);
        }
        catch (Exception ex)
        {
            failedPages++;
            Console.WriteLine($"  Pricing product publish failed: {ex.Message}");
        }
    }

    Console.WriteLine();
    Console.WriteLine("Pricing product generation complete.");
    Console.WriteLine($"Pages created: {createdPages}");
    Console.WriteLine($"Pages resumed: {resumedPages}");
    Console.WriteLine($"Pages failed: {failedPages}");
    Console.WriteLine($"Cache file: {cachePath}");

    return failedPages > 0 && createdPages == 0 && resumedPages == 0 ? 1 : 0;
}

static List<PricingProductPlan> BuildPricingProductPlans(PrintifyBlueprintQueryApi api, IReadOnlyList<int> blueprintIds)
{
    var plans = new List<PricingProductPlan>();

    foreach (var blueprintId in blueprintIds)
    {
        var detail = api.GetBlueprintDetail(blueprintId);

        foreach (var providerDetail in detail.PrintProviders.OrderBy(provider => provider.Provider.Title, StringComparer.OrdinalIgnoreCase))
        {
            var subvariants = api.GetSubvariants(blueprintId, providerDetail.Provider.Id)
                .Where(subvariant => subvariant.Placeholders.Count > 0)
                .OrderBy(subvariant => subvariant.VariantId)
                .ToList();

            if (subvariants.Count == 0)
            {
                continue;
            }

            var pageNumber = 0;
            foreach (var batch in subvariants.Chunk(MaxProbeVariantBatchSize))
            {
                var batchList = batch.ToList();
                pageNumber++;

                plans.Add(new PricingProductPlan
                {
                    BlueprintId = blueprintId,
                    BlueprintTitle = detail.Blueprint.Title,
                    ProviderId = providerDetail.Provider.Id,
                    ProviderTitle = providerDetail.Provider.Title,
                    PageNumber = pageNumber,
                    Title = BuildPricingProductTitle(detail.Blueprint.Title, providerDetail.Provider.Title, pageNumber),
                    Subvariants = batchList
                });
            }
        }
    }

    return plans;
}

static async Task<Dictionary<string, List<Product>>> LoadExistingProductsByTitleAsync(PrintifyClient client, int shopId, Func<Task> throttleAsync)
{
    var index = new Dictionary<string, List<Product>>(StringComparer.OrdinalIgnoreCase);
    var pageNumber = 1;

    while (true)
    {
        await throttleAsync();
        var page = await client.GetProductsAsync(shopId, pageNumber, PricingProductListPageSize);

        foreach (var product in page.Data)
        {
            AddIndexedProduct(index, product);
        }

        if (page.Data.Count == 0 || page.CurrentPage >= page.LastPage)
        {
            break;
        }

        pageNumber++;
    }

    return index;
}

static void AddIndexedProduct(Dictionary<string, List<Product>> existingProductsByTitle, Product product)
{
    var lookupKey = NormalizeLookupValue(product.Title);
    if (string.IsNullOrWhiteSpace(lookupKey))
    {
        return;
    }

    if (!existingProductsByTitle.TryGetValue(lookupKey, out var products))
    {
        products = new List<Product>();
        existingProductsByTitle[lookupKey] = products;
    }

    products.RemoveAll(existing => string.Equals(existing.Id, product.Id, StringComparison.OrdinalIgnoreCase));
    products.Add(product);
}

static async Task<int> DeleteMatchingPricingProductsAsync(
    PrintifyClient client,
    int shopId,
    IReadOnlyList<PricingProductPlan> plans,
    IReadOnlyDictionary<string, List<Product>> existingProductsByTitle,
    Func<Task> throttleAsync)
{
    var deletedProductIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

    foreach (var plan in plans)
    {
        foreach (var product in GetExistingPricingProducts(plan, existingProductsByTitle))
        {
            if (!deletedProductIds.Add(product.Id))
            {
                continue;
            }

            await throttleAsync();
            if (!await DeletePricingProductSafeAsync(client, shopId, product.Id, product.Title))
            {
                deletedProductIds.Remove(product.Id);
            }
        }
    }

    return deletedProductIds.Count;
}

static bool TryFindExistingPricingProduct(
    PricingProductPlan plan,
    IReadOnlyDictionary<string, List<Product>> existingProductsByTitle,
    out Product product,
    out bool variantMismatch)
{
    variantMismatch = false;

    foreach (var candidate in GetExistingPricingProducts(plan, existingProductsByTitle)
        .OrderByDescending(existing => existing.Visible))
    {
        if (HasExpectedPricingTags(candidate, plan))
        {
            if (candidate.Variants.Count == 0 || HasMatchingVariantIds(candidate, plan.VariantIds))
            {
                product = candidate;
                return true;
            }

            variantMismatch = true;
            continue;
        }

        if (HasMatchingVariantIds(candidate, plan.VariantIds))
        {
            product = candidate;
            return true;
        }

        variantMismatch = true;
    }

    product = null!;
    return false;
}

static IEnumerable<Product> GetExistingPricingProducts(PricingProductPlan plan, IReadOnlyDictionary<string, List<Product>> existingProductsByTitle)
{
    var lookupKey = NormalizeLookupValue(plan.Title);
    if (!existingProductsByTitle.TryGetValue(lookupKey, out var candidates))
    {
        return Enumerable.Empty<Product>();
    }

    return candidates.Where(candidate =>
        candidate.BlueprintId == plan.BlueprintId &&
        candidate.PrintProviderId == plan.ProviderId);
}

static bool HasExpectedPricingTags(Product product, PricingProductPlan plan)
{
    var expectedTags = BuildPricingProductTags(plan);
    var actualTags = product.Tags ?? new List<string>();

    return expectedTags.All(expectedTag => actualTags.Any(actualTag => string.Equals(actualTag, expectedTag, StringComparison.OrdinalIgnoreCase)));
}

static bool HasMatchingVariantIds(Product product, IReadOnlyList<int> expectedVariantIds)
{
    if (product.Variants.Count == 0)
    {
        return false;
    }

    var actualVariantIds = product.Variants
        .Select(variant => variant.Id)
        .OrderBy(id => id)
        .ToList();

    if (actualVariantIds.Count != expectedVariantIds.Count)
    {
        return false;
    }

    for (var index = 0; index < actualVariantIds.Count; index++)
    {
        if (actualVariantIds[index] != expectedVariantIds[index])
        {
            return false;
        }
    }

    return true;
}

static async Task<Product> CreateAndPublishPricingProductAsync(
    PrintifyClient client,
    int shopId,
    PricingProductPlan plan,
    string uploadedImageId,
    Func<Task> throttleAsync)
{
    var request = new CreateProductRequest
    {
        Title = plan.Title,
        Description = PricingProductDescription,
        BlueprintId = plan.BlueprintId,
        PrintProviderId = plan.ProviderId,
        Tags = BuildPricingProductTags(plan),
        Variants = plan.Subvariants
            .Select(subvariant => new CreateProductVariant
            {
                Id = subvariant.VariantId,
                Price = PricingProductRetailPrice,
                IsEnabled = true
            })
            .ToList(),
        PrintAreas = BuildProbePrintAreas(plan.Subvariants, uploadedImageId)
    };

    await throttleAsync();
    var product = await client.CreateProductAsync(shopId, request);
    product = await WaitForProductImagesAsync(client, shopId, product, throttleAsync);

    if ((product.Images?.Count ?? 0) == 0)
    {
        throw new InvalidOperationException($"Product {product.Id} did not return any images for publishing.");
    }

    await throttleAsync();
    await client.PublishProductAsync(shopId, product.Id, new PublishProductRequest());

    product.Visible = true;
    return product;
}

static async Task EnsurePricingProductPublishedAsync(PrintifyClient client, int shopId, Product product, Func<Task> throttleAsync)
{
    if (product.Visible)
    {
        return;
    }

    await throttleAsync();
    await client.PublishProductAsync(shopId, product.Id, new PublishProductRequest());
}

static async Task<UploadedImage> EnsurePricingProbeUploadAsync(
    PrintifyClient client,
    PrintifyPricingProductCacheDocument document,
    Func<Task> throttleAsync)
{
    if (!string.IsNullOrWhiteSpace(document.ProbeUploadId))
    {
        try
        {
            await throttleAsync();
            var existingUpload = await client.GetUploadAsync(document.ProbeUploadId);

            document.ProbeUploadId = existingUpload.Id;
            document.ProbeUploadPreviewUrl = existingUpload.PreviewUrl;

            Console.WriteLine($"Reusing pricing probe upload {existingUpload.Id}.");
            return existingUpload;
        }
        catch (PrintifyApiException ex) when (ex.StatusCode == 404)
        {
            Console.WriteLine($"Cached pricing probe upload {document.ProbeUploadId} was not found. Uploading a fresh probe image.");
        }
    }

    await throttleAsync();
    var uploaded = await client.UploadImageByUrlAsync(PricingProductProbeImageFileName, ProbeImageUrl);

    document.ProbeUploadId = uploaded.Id;
    document.ProbeUploadPreviewUrl = uploaded.PreviewUrl;

    Console.WriteLine($"Uploaded pricing probe image {uploaded.Id}.");
    return uploaded;
}

static string BuildPricingProductTitle(string blueprintTitle, string providerTitle, int pageNumber)
{
    var suffix = $"-{pageNumber}";
    var baseTitle = $"{SanitizePricingTitlePart(blueprintTitle)}-{SanitizePricingTitlePart(providerTitle)}";
    var maxBaseLength = Math.Max(1, 180 - suffix.Length);

    if (baseTitle.Length > maxBaseLength)
    {
        baseTitle = baseTitle[..maxBaseLength].TrimEnd('-', ' ');
    }

    return $"{baseTitle}{suffix}";
}

static string SanitizePricingTitlePart(string value)
{
    if (string.IsNullOrWhiteSpace(value))
    {
        return "untitled";
    }

    var builder = new StringBuilder(value.Length);
    var shouldInsertSpace = false;

    foreach (var character in value.Trim())
    {
        if (char.IsWhiteSpace(character) || char.IsControl(character) || character is '/' or '\\' or ':' or '|' or '"')
        {
            shouldInsertSpace = builder.Length > 0;
            continue;
        }

        if (shouldInsertSpace && builder.Length > 0)
        {
            builder.Append(' ');
            shouldInsertSpace = false;
        }

        builder.Append(character);
    }

    var sanitized = builder.ToString().Trim();
    return string.IsNullOrWhiteSpace(sanitized) ? "untitled" : sanitized;
}

static List<string> BuildPricingProductTags(PricingProductPlan plan)
{
    return new List<string>
    {
        PricingProductCacheTag,
        $"pricing-blueprint-{plan.BlueprintId}",
        $"pricing-provider-{plan.ProviderId}",
        $"pricing-page-{plan.PageNumber}"
    };
}

static string NormalizeLookupValue(string? value)
{
    return (value ?? string.Empty).Trim();
}

static PrintifyPricingProductCacheDocument LoadPricingProductCacheDocument(string filePath)
{
    return PrintifyPricingProductCacheStore.Load(filePath);
}

static void SavePricingProductCacheDocument(string filePath, PrintifyPricingProductCacheDocument document)
{
    PrintifyPricingProductCacheStore.Save(filePath, document);
}

static void MergePricingProductCacheEntry(PrintifyPricingProductCacheDocument document, PrintifyPricingProductCacheEntry entry)
{
    document.Entries.RemoveAll(existing =>
        existing.ShopId == entry.ShopId &&
        existing.BlueprintId == entry.BlueprintId &&
        existing.ProviderId == entry.ProviderId &&
        existing.PageNumber == entry.PageNumber);

    document.Entries.Add(entry);
}

static PrintifyPricingProductCacheEntry CreatePricingProductCacheEntry(PricingProductPlan plan, Product product)
{
    return new PrintifyPricingProductCacheEntry
    {
        ShopId = product.ShopId,
        BlueprintId = plan.BlueprintId,
        BlueprintTitle = plan.BlueprintTitle,
        ProviderId = plan.ProviderId,
        ProviderTitle = plan.ProviderTitle,
        PageNumber = plan.PageNumber,
        Title = plan.Title,
        ProductId = product.Id,
        Visible = product.Visible,
        IsPublished = product.Visible,
        ExternalId = product.External?.Id,
        ExternalHandle = product.External?.Handle,
        UpdatedAtUtc = DateTime.UtcNow,
        VariantIds = plan.VariantIds.ToList()
    };
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

async static Task PrintUsage(string? repositoryRoot)
{
    Console.WriteLine("PrintifyGenerator.CacheGenerator usage:");
    Console.WriteLine("  dotnet run --project src/PrintifyGenerator.CacheGenerator -- catalog [--blueprint <id>] [--limit <n>]");
    Console.WriteLine("  dotnet run --project src/PrintifyGenerator.CacheGenerator -- variant-images [--blueprint <id>] [--limit <n>] [--shop-id <id>] [--force]");
    Console.WriteLine("  dotnet run --project src/PrintifyGenerator.CacheGenerator -- pricing-products [--blueprint <id>] [--limit <n>] [--shop-id <id>] [--reset-cache]");
    Console.WriteLine("  dotnet run --project src/PrintifyGenerator.CacheGenerator -- publish-products [--blueprint <id>] [--limit <n>] [--shop-id <id>] [--reset-cache]");
    Console.WriteLine("  dotnet run --project src/PrintifyGenerator.CacheGenerator -- all [--blueprint <id>] [--limit <n>] [--shop-id <id>] [--force]");
    foreach(var shop in await new PrintifyClient(ReadToken(Path.Combine(repositoryRoot, "main.env"))).GetShopsAsync())
    {
        Console.WriteLine($"Shop ID {shop.Id}: {shop.Title} (Sales channel: {shop.SalesChannel})");
    }
}

async static Task<int> UnknownMode(string mode, string? repositoryRoot)
{
    Console.Error.WriteLine($"Unknown mode '{mode}'.");
    await PrintUsage(repositoryRoot);
    return 1;
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

static string ReadMode(string[] args)
{
    for (var index = 0; index < args.Length; index++)
    {
        var arg = args[index];
        if (string.IsNullOrWhiteSpace(arg))
        {
            continue;
        }

        if (string.Equals(arg, "--blueprint", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(arg, "--limit", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(arg, "--shop-id", StringComparison.OrdinalIgnoreCase))
        {
            index++;
            continue;
        }

        if (arg[0] == '-')
        {
            continue;
        }

        return arg.Trim().ToLowerInvariant();
    }

    return "catalog";
}

static Shop? ResolveProbeShop(string envFilePath, int? shopIdOverride, IReadOnlyList<Shop> shops)
{
    var configuredShopId = shopIdOverride
        ?? ReadOptionalIntEnvValue(envFilePath, "SHOP_ID")
        ?? ReadOptionalIntEnvValue(envFilePath, "SHOPID");

    if (configuredShopId.HasValue)
    {
        return shops.FirstOrDefault(shop => shop.Id == configuredShopId.Value);
    }

    return shops.FirstOrDefault(shop => shop.Title.Contains("cache", StringComparison.OrdinalIgnoreCase))
        ?? shops.FirstOrDefault(shop => string.Equals(shop.SalesChannel, "custom_integration", StringComparison.OrdinalIgnoreCase))
        ?? shops.FirstOrDefault();
}

static Shop? ResolvePublishingShop(string envFilePath, int? shopIdOverride, IReadOnlyList<Shop> shops)
{
    var configuredShopId = shopIdOverride
        ?? ReadOptionalIntEnvValue(envFilePath, "PRICE_UPDATER_SHOP_ID")
        ?? ReadOptionalIntEnvValue(envFilePath, "SHOP_ID")
        ?? ReadOptionalIntEnvValue(envFilePath, "SHOPID");

    if (configuredShopId.HasValue)
    {
        return shops.FirstOrDefault(shop => shop.Id == configuredShopId.Value);
    }

    return shops.FirstOrDefault(shop => !string.Equals(shop.SalesChannel, "custom_integration", StringComparison.OrdinalIgnoreCase))
        ?? shops.FirstOrDefault();
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

        return int.TryParse(line[(key.Length + 1)..].Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsedValue)
            ? parsedValue
            : null;
    }

    return null;
}

static async Task<UploadedImage> UploadProbeImageAsync(PrintifyClient client)
{
    return await client.UploadImageByUrlAsync(
        $"variant-image-probe-{DateTime.UtcNow:yyyyMMdd-HHmmss}.png",
        ProbeImageUrl);
}

static async Task<List<PrintifyVariantImageCacheEntry>> LoadDraftProductVariantImageEntriesAsync(
    PrintifyClient client,
    int shopId,
    int blueprintId,
    int providerId,
    IReadOnlyList<PrintifyBlueprintSubvariant> subvariants,
    string uploadedImageId)
{
    var printableSubvariants = subvariants
        .Where(subvariant => subvariant.Placeholders.Count > 0)
        .ToList();

    _ = printableSubvariants.FirstOrDefault()
        ?? throw new InvalidOperationException(
            $"Blueprint {blueprintId}, provider {providerId} has no placeholders available for temporary product creation.");

    var entriesByVariantId = new Dictionary<int, PrintifyVariantImageCacheEntry>();

    foreach (var batch in printableSubvariants.Chunk(MaxProbeVariantBatchSize))
    {
        var batchList = batch.ToList();
        var seedSubvariant = batchList[0];
        var request = new CreateProductRequest
        {
            Title = $"Variant Image Cache Probe - {seedSubvariant.BlueprintTitle} - {providerId}",
            Description = "Temporary product used to inspect Printify variant mockup images.",
            BlueprintId = blueprintId,
            PrintProviderId = providerId,
            Variants = batchList
                .Select(subvariant => new CreateProductVariant
                {
                    Id = subvariant.VariantId,
                    Price = 2000,
                    IsEnabled = true
                })
                .ToList(),
            PrintAreas = BuildProbePrintAreas(batchList, uploadedImageId)
        };

        Product? product = null;

        try
        {
            product = await client.CreateProductAsync(shopId, request);
            product = await WaitForProductImagesAsync(client, shopId, product);
            var imageLookup = BuildProductVariantImageLookup(product);

            foreach (var subvariant in batchList)
            {
                if (!imageLookup.TryGetValue(subvariant.VariantId, out var image))
                {
                    continue;
                }

                entriesByVariantId[subvariant.VariantId] = new PrintifyVariantImageCacheEntry
                {
                    BlueprintId = blueprintId,
                    ProviderId = providerId,
                    VariantId = subvariant.VariantId,
                    ImageUrl = image.Src,
                    Position = image.Position,
                    IsDefault = image.IsDefault,
                    UpdatedAtUtc = DateTime.UtcNow
                };
            }
        }
        finally
        {
            if (product is not null)
            {
                await DeleteProbeProductSafeAsync(client, shopId, product.Id);
            }
        }
    }

    return entriesByVariantId.Values
        .OrderBy(entry => entry.VariantId)
        .ToList();
}

static List<PrintArea> BuildProbePrintAreas(
    IReadOnlyList<PrintifyBlueprintSubvariant> subvariants,
    string uploadedImageId)
{
    return subvariants
        .GroupBy(subvariant => BuildPlaceholderSignature(subvariant.Placeholders), StringComparer.Ordinal)
        .Select(group =>
        {
            var exemplar = group.First();

            return new PrintArea
            {
                VariantIds = group
                    .Select(subvariant => subvariant.VariantId)
                    .Distinct()
                    .ToList(),
                Placeholders = exemplar.Placeholders
                    .Select(placeholder => new PrintAreaPlaceholder
                    {
                        Position = placeholder.Position,
                        DecorationMethod = string.IsNullOrWhiteSpace(placeholder.DecorationMethod)
                            ? null
                            : placeholder.DecorationMethod,
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
                    })
                    .ToList()
            };
        })
        .ToList();
}

static string BuildPlaceholderSignature(IReadOnlyList<VariantPlaceholder> placeholders)
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

static async Task<Product> WaitForProductImagesAsync(PrintifyClient client, int shopId, Product product, Func<Task>? beforeFetchAsync = null)
{
    var current = product;
    if ((current.Images?.Count ?? 0) > 0)
    {
        return current;
    }

    for (var attempt = 0; attempt < 5; attempt++)
    {
        await Task.Delay(TimeSpan.FromSeconds(1.5));

        if (beforeFetchAsync is not null)
        {
            await beforeFetchAsync();
        }

        current = await client.GetProductAsync(shopId, current.Id);

        if ((current.Images?.Count ?? 0) > 0)
        {
            return current;
        }
    }

    return current;
}

static Dictionary<int, ProductMockupImage> BuildProductVariantImageLookup(Product product)
{
    var lookup = new Dictionary<int, (ProductMockupImage Image, int Rank)>();

    foreach (var image in product.Images ?? Enumerable.Empty<ProductMockupImage>())
    {
        var imageUrl = image.Src?.Trim();
        if (string.IsNullOrWhiteSpace(imageUrl))
        {
            continue;
        }

        var rank = 0;
        if (!image.IsDefault)
        {
            rank += 10;
        }

        if (!string.Equals(image.Position, "front", StringComparison.OrdinalIgnoreCase))
        {
            rank += 5;
        }

        foreach (var variantId in image.VariantIds)
        {
            if (variantId <= 0)
            {
                continue;
            }

            if (lookup.TryGetValue(variantId, out var existing) && existing.Rank <= rank)
            {
                continue;
            }

            lookup[variantId] = (image with { Src = imageUrl }, rank);
        }
    }

    return lookup.ToDictionary(entry => entry.Key, entry => entry.Value.Image);
}

static void MergeVariantImageEntries(PrintifyVariantImageCacheDocument document, IReadOnlyList<PrintifyVariantImageCacheEntry> entries)
{
    foreach (var entry in entries)
    {
        document.Entries.RemoveAll(existing =>
            existing.BlueprintId == entry.BlueprintId &&
            existing.ProviderId == entry.ProviderId &&
            existing.VariantId == entry.VariantId);

        document.Entries.Add(entry);
    }
}

static int CountCachedVariants(PrintifyVariantImageCacheDocument document, int blueprintId, int providerId, IReadOnlyCollection<int> variantIds)
{
    var variantIdSet = variantIds.ToHashSet();

    return document.Entries.Count(entry =>
        entry.BlueprintId == blueprintId &&
        entry.ProviderId == providerId &&
        variantIdSet.Contains(entry.VariantId) &&
        !string.IsNullOrWhiteSpace(entry.ImageUrl));
}

static PrintifyVariantImageCacheDocument LoadVariantImageCacheDocument(string filePath)
{
    if (!File.Exists(filePath))
    {
        return new PrintifyVariantImageCacheDocument();
    }

    try
    {
        var json = File.ReadAllText(filePath);
        var loaded = JsonSerializer.Deserialize<PrintifyVariantImageCacheDocument>(json, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            ReadCommentHandling = JsonCommentHandling.Skip,
            AllowTrailingCommas = true
        });

        return loaded ?? new PrintifyVariantImageCacheDocument();
    }
    catch
    {
        return new PrintifyVariantImageCacheDocument();
    }
}

static void SaveVariantImageCacheDocument(string filePath, PrintifyVariantImageCacheDocument document)
{
    document.Entries = document.Entries
        .Where(entry =>
            entry.BlueprintId > 0 &&
            entry.ProviderId > 0 &&
            entry.VariantId > 0 &&
            !string.IsNullOrWhiteSpace(entry.ImageUrl))
        .OrderBy(entry => entry.BlueprintId)
        .ThenBy(entry => entry.ProviderId)
        .ThenBy(entry => entry.VariantId)
        .ToList();

    var json = JsonSerializer.Serialize(document, new JsonSerializerOptions { WriteIndented = true });
    File.WriteAllText(filePath, json);
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

static async Task<bool> DeletePricingProductSafeAsync(PrintifyClient client, int shopId, string productId, string title)
{
    try
    {
        await client.DeleteProductAsync(shopId, productId);
        Console.WriteLine($"  Deleted pricing product {productId} ({title}).");
        return true;
    }
    catch (Exception ex)
    {
        Console.WriteLine($"  Failed to delete pricing product {productId} ({title}): {ex.Message}");
        return false;
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

sealed class PricingProductPlan
{
    public int BlueprintId { get; init; }
    public string BlueprintTitle { get; init; } = string.Empty;
    public int ProviderId { get; init; }
    public string ProviderTitle { get; init; } = string.Empty;
    public int PageNumber { get; init; }
    public string Title { get; init; } = string.Empty;
    public List<PrintifyBlueprintSubvariant> Subvariants { get; init; } = new();

    public IReadOnlyList<int> VariantIds => Subvariants
        .Select(subvariant => subvariant.VariantId)
        .OrderBy(id => id)
        .ToList();
}

/**/