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
    List<Shop> shops = await client.GetShopsAsync();
    targetShop = shops.FirstOrDefault(a => a.Title == "Production");//settings.ResolveShop(shops);
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
    CalculateVariantPrice(shippingPrice, productionPrice, settings.MarginPercent, settings.ShippingAddress.Country);

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

static int CalculateVariantPrice(int shippingPrice, int productionPrice, decimal marginPercent,string country = "United States")
{
    if (productionPrice < 0)
    {
        throw new ArgumentOutOfRangeException(nameof(productionPrice), "Production price cannot be negative.");
    }

    var normalizedShippingPrice = Math.Max(0, shippingPrice);
    var marginMultiplier = 1m + (marginPercent / 100m);
    var costFloor = (productionPrice + normalizedShippingPrice) / 100m;
    var targetRetail = costFloor * marginMultiplier;

    Console.Write(
        $"[Calculating] {productionPrice}USD (production) + {normalizedShippingPrice}USD (shipping) with margin {marginPercent:0.##}% for country {country}.");

    var Total = new Currency(CurrencyCode.USD, targetRetail);
    Console.Write($" Target retail before rounding: {Total}.");

    // var targetCurrency = country switch
    // {
    //     "United States" => CurrencyCode.USD,
    //     "USA" => CurrencyCode.USD,
    //     "US" => CurrencyCode.USD,
    //     "United Kingdom" => CurrencyCode.GBP,
    //     "GB" => CurrencyCode.GBP,
    //     _ => throw new NotSupportedException($"Unsupported destination country: {country}")
    // };
    // Total = Total.ConvertTo(targetCurrency).GetAwaiter().GetResult();
    // Console.Write($" Converted total cost to {targetCurrency}: {Total}.");
    // Keep retail-friendly endings while preserving profitability against production + shipping cost floor.
    Total = Total.Amount switch
    {
        < 10 => new Currency(Total.Code, Math.Max(Math.Round(Total.Amount - 0.01m) + 0.99m, targetRetail)),
        < 50 => new Currency(Total.Code, Math.Max(Math.Round(Total.Amount - 0.01m) + 0.49m, targetRetail)),
        _ => new Currency(Total.Code, Math.Max(Math.Round(Total.Amount - 0.01m) + 0.99m, targetRetail))
    };

    if (Total.Amount < costFloor)
    {
        Total = new Currency(Total.Code, costFloor);
    }

    Console.WriteLine($"Rounded total price: {Total}.");

    //convert back to usd
    // Total = Total.ConvertTo(CurrencyCode.USD).GetAwaiter().GetResult();
    Console.WriteLine($"Converted total cost back to USD: {Total}.");

    return checked((int)(Total.Amount * 100));
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