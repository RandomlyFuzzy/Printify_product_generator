using System.Globalization;

var repositoryRoot = ResolveRepositoryRoot();
if (repositoryRoot is null)
{
    Console.Error.WriteLine("[ERROR] Could not locate the repository root.");
    return 1;
}

Directory.SetCurrentDirectory(repositoryRoot);

PricingUpdaterSettings settings;
try
{
    settings = PricingUpdaterSettings.Load(repositoryRoot, args);
}
catch (Exception ex)
{
    Console.Error.WriteLine($"[ERROR] {ex.Message}");
    return 1;
}

var client = new PrintifyClient(settings.Token);

Shop targetShop;
try
{
    var shops = await client.GetShopsAsync();
    targetShop = settings.ResolveShop(shops);
}
catch (Exception ex)
{
    Console.Error.WriteLine($"[ERROR] Failed to resolve the target Printify shop: {ex.Message}");
    return 1;
}

Console.WriteLine($"Pricing updater targeting shop {targetShop.Id} ({targetShop.Title}).");
Console.WriteLine(settings.ApplyChanges
    ? "Live mode is enabled. Price updates will be sent to Printify."
    : "Dry-run mode is enabled. No price updates will be sent to Printify.");
Console.WriteLine($"Shipping basis: {settings.ShippingMethod.ToConfigValue()}");
Console.WriteLine($"Profit formula: {settings.MarginPercent.ToString("0.##", CultureInfo.InvariantCulture)}% over production cost.");
Console.WriteLine($"Destination country: {settings.ShippingAddress.Country}");

if (!string.IsNullOrWhiteSpace(settings.ShippingAddress.Region))
{
    Console.WriteLine($"Destination region: {settings.ShippingAddress.Region}");
}

if (!string.IsNullOrWhiteSpace(settings.ShippingAddress.Zip))
{
    Console.WriteLine($"Destination postal code: {settings.ShippingAddress.Zip}");
}

if (settings.ProductLimit.HasValue)
{
    Console.WriteLine($"Product limit: {settings.ProductLimit.Value}");
}

if (settings.VariantLimitPerProduct.HasValue)
{
    Console.WriteLine($"Variant limit per product: {settings.VariantLimitPerProduct.Value}");
}

using var cancellation = new CancellationTokenSource();
Console.CancelKeyPress += (_, eventArgs) =>
{
    eventArgs.Cancel = true;
    if (!cancellation.IsCancellationRequested)
    {
        Console.WriteLine("Cancellation requested. Waiting for the current run to finish...");
        cancellation.Cancel();
    }
};

Func<int, int, int> priceCalculator = (shippingPrice, productionPrice) =>
    CalculateVariantPrice(shippingPrice, productionPrice, settings.MarginPercent);

var updater = new PricingUpdater(client, targetShop.Id, settings, priceCalculator);

while (!cancellation.IsCancellationRequested)
{
    try
    {
        var summary = await updater.RunOnceAsync(cancellation.Token);
        PrintSummary(summary, settings.ApplyChanges);
    }
    catch (OperationCanceledException) when (cancellation.IsCancellationRequested)
    {
        break;
    }
    catch (Exception ex)
    {
        Console.Error.WriteLine($"[ERROR] Pricing update run failed: {ex.Message}");
        if (settings.RunOnce)
        {
            return 1;
        }
    }

    if (settings.RunOnce || cancellation.IsCancellationRequested)
    {
        break;
    }

    var nextRunAt = DateTimeOffset.UtcNow + settings.Interval;
    Console.WriteLine($"Next run scheduled at {nextRunAt:O}.");

    try
    {
        await Task.Delay(settings.Interval, cancellation.Token);
    }
    catch (OperationCanceledException) when (cancellation.IsCancellationRequested)
    {
        break;
    }
}

return 0;

static int CalculateVariantPrice(int shippingPrice, int productionPrice, decimal marginPercent)
{
    _ = shippingPrice;

    if (productionPrice < 0)
    {
        throw new ArgumentOutOfRangeException(nameof(productionPrice), "Production price cannot be negative.");
    }

    var multiplier = 1m + (marginPercent / 100m);
    return checked((int)Math.Ceiling((productionPrice + shippingPrice) * multiplier));
}

static void PrintSummary(PricingUpdateRunSummary summary, bool applyChanges)
{
    Console.WriteLine();
    Console.WriteLine($"Run completed in {(summary.CompletedAt - summary.StartedAt).TotalSeconds:F1}s.");
    Console.WriteLine($"Products discovered: {summary.ProductsDiscovered}");
    Console.WriteLine($"Products processed: {summary.ProductsProcessed}");
    Console.WriteLine($"Products unchanged: {summary.ProductsUnchanged}");
    Console.WriteLine($"Locked products skipped: {summary.ProductsSkippedLocked}");
    Console.WriteLine($"Products without variants: {summary.ProductsWithoutVariants}");
    Console.WriteLine($"Product load failures: {summary.ProductLoadFailures}");
    Console.WriteLine($"Product update failures: {summary.ProductUpdateFailures}");
    Console.WriteLine($"Variants evaluated: {summary.VariantsEvaluated}");
    Console.WriteLine($"Variants changed: {summary.VariantsChanged}");
    Console.WriteLine($"Variants skipped by limit: {summary.VariantsSkippedByLimit}");
    Console.WriteLine($"Variant quote failures: {summary.VariantQuoteFailures}");
    Console.WriteLine(applyChanges
        ? $"Products updated: {summary.ProductsUpdated}"
        : $"Products that would be updated: {summary.ProductsPlannedForUpdate}");
    Console.WriteLine();
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