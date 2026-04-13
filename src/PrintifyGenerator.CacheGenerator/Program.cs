using System.Text.Json;

var jsonOpts = new JsonSerializerOptions { WriteIndented = true };

// ── Read token from main.env ───────────────────────────────────────
string token = "";
if (File.Exists("./main.env"))
{
    foreach (var line in File.ReadAllLines("./main.env"))
    {
        if (line.StartsWith("TOKEN="))
        {
            token = line.Substring("TOKEN=".Length).Trim();
            break;
        }
    }
}

if (string.IsNullOrEmpty(token))
{
    Console.Error.WriteLine("ERROR: No TOKEN found in main.env");
    return 1;
}

var client = new PrintifyClient(token);

// ── 0 clear images from printify ─────────────────────────────────────

var uploads = await client.GetAllUploadsAsync();
Console.WriteLine($"Found {uploads.Count} uploaded images. Archiving...");
foreach(var imageid in uploads)
{
    // Console.WriteLine($"Archiving image {imageid} ...");
    Console.WriteLine($"  ID: {imageid.Id}");
    await client.ArchiveUploadAsync(imageid.Id);
}


return 0 ;




var cacheDir = "./src/data/Cached";
Directory.CreateDirectory(cacheDir);

// ── 1. Blueprints ──────────────────────────────────────────────────
Console.WriteLine("Fetching blueprints...");
var blueprints = await client.GetBlueprintsAsync();
await File.WriteAllTextAsync(
    Path.Combine(cacheDir, "blueprints.json"),
    JsonSerializer.Serialize(blueprints, jsonOpts));
Console.WriteLine($"  Cached {blueprints.Count} blueprints.");

// ── 2. Print Providers ─────────────────────────────────────────────
Console.WriteLine("Fetching print providers...");
var printProviders = await client.GetPrintProvidersAsync();
await File.WriteAllTextAsync(
    Path.Combine(cacheDir, "print_providers.json"),
    JsonSerializer.Serialize(printProviders, jsonOpts));
Console.WriteLine($"  Cached {printProviders.Count} print providers.");

// ── 3. Per-blueprint details (providers, variants, shipping) ───────
// Catalog rate limit is 100 req/min — pace requests accordingly.
var blueprintDetailsDir = Path.Combine(cacheDir, "blueprint_details");
Directory.CreateDirectory(blueprintDetailsDir);

int requestCount = 0;
var stopwatch = System.Diagnostics.Stopwatch.StartNew();

async Task ThrottleAsync()
{
    requestCount++;
    // Every 90 requests, check if we need to wait to stay under 100/min
    if (requestCount % 90 == 0)
    {
        var elapsed = stopwatch.Elapsed;
        if (elapsed.TotalSeconds < 60)
        {
            var delay = TimeSpan.FromSeconds(62) - elapsed;
            Console.WriteLine($"  Rate-limit pause: waiting {delay.TotalSeconds:F0}s ...");
            await Task.Delay(delay);
        }
        stopwatch.Restart();
        requestCount = 0;
    }
}

Console.WriteLine($"Fetching per-blueprint details for {blueprints.Count} blueprints...");

for (int i = 0; i < blueprints.Count; i++)
{
    var bp = blueprints[i];
    Console.Write($"\r  [{i + 1}/{blueprints.Count}] Blueprint {bp.Id} – {bp.Title}                    ");

    try
    {
        // Providers for this blueprint
        await ThrottleAsync();
        var bpProviders = await client.GetBlueprintPrintProvidersAsync(bp.Id);

        var providerDetails = new List<object>();

        foreach (var prov in bpProviders)
        {
            // Variants (includes placeholders = print locations & sizes)
            await ThrottleAsync();
            VariantResponse? variants = null;
            try
            {
                variants = await client.GetBlueprintVariantsAsync(bp.Id, prov.Id, showOutOfStock: true);
            }
            catch (PrintifyApiException ex) when (ex.StatusCode == 404)
            {
                // Some combos may not exist
            }

            // Shipping
            await ThrottleAsync();
            ShippingInfo? shipping = null;
            try
            {
                shipping = await client.GetBlueprintShippingAsync(bp.Id, prov.Id);
            }
            catch (PrintifyApiException ex) when (ex.StatusCode == 404)
            {
                // Some combos may not exist
            }

            // Build a clean cost-per-region summary from shipping profiles
            var costPerRegion = shipping?.Profiles
                .Select(p => new
                {
                    countries = p.Countries,
                    first_item = p.FirstItem,
                    additional_items = p.AdditionalItems,
                    variant_count = p.VariantIds.Count
                })
                .ToList();

            providerDetails.Add(new
            {
                provider = prov,
                variants,
                shipping,
                cost_per_region = costPerRegion
            });
        }

        var detail = new
        {
            blueprint = bp,
            print_providers = providerDetails
        };

        await File.WriteAllTextAsync(
            Path.Combine(blueprintDetailsDir, $"{bp.Id}.json"),
            JsonSerializer.Serialize(detail, jsonOpts));
    }
    catch (PrintifyApiException ex)
    {
        Console.Error.WriteLine($"\n  WARNING: Failed blueprint {bp.Id}: {ex.Message}");
    }
}

Console.WriteLine();

// ── 4. Shops + products ────────────────────────────────────────────
Console.WriteLine("Fetching shops...");
var shops = await client.GetShopsAsync();
await File.WriteAllTextAsync(
    Path.Combine(cacheDir, "shops.json"),
    JsonSerializer.Serialize(shops, jsonOpts));
Console.WriteLine($"  Cached {shops.Count} shops.");

var productsDir = Path.Combine(cacheDir, "products");
Directory.CreateDirectory(productsDir);

foreach (var shop in shops)
{
    Console.WriteLine($"Fetching products for shop {shop.Id} ({shop.Title})...");
    try
    {
        var products = await client.GetAllProductsAsync(shop.Id);
        await File.WriteAllTextAsync(
            Path.Combine(productsDir, $"shop_{shop.Id}.json"),
            JsonSerializer.Serialize(products, jsonOpts));
        Console.WriteLine($"  Cached {products.Count} products.");
    }
    catch (PrintifyApiException ex)
    {
        Console.Error.WriteLine($"  WARNING: Failed products for shop {shop.Id}: {ex.Message}");
    }
}

Console.WriteLine("Cache generation complete.");
return 0;
