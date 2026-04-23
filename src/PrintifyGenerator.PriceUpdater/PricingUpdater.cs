using System.Globalization;

sealed class PricingUpdater
{
    private readonly PrintifyClient _client;
    private readonly int _shopId;
    private readonly PricingUpdaterSettings _settings;
    private readonly Func<int, int, int> _priceCalculator;

    public PricingUpdater(
        PrintifyClient client,
        int shopId,
        PricingUpdaterSettings settings,
        Func<int, int, int> priceCalculator)
    {
        _client = client;
        _shopId = shopId;
        _settings = settings;
        _priceCalculator = priceCalculator;
    }

    public async Task<PricingUpdateRunSummary> RunOnceAsync(CancellationToken cancellationToken)
    {
        var summary = new PricingUpdateRunSummary
        {
            StartedAt = DateTimeOffset.UtcNow
        };

        Console.WriteLine();
        Console.WriteLine($"Starting pricing update run at {summary.StartedAt:O}.");

        var products = await _client.GetAllProductsAsync(_shopId);
        var orderedProducts = products
            .OrderBy(product => product.Title, StringComparer.OrdinalIgnoreCase)
            .ThenBy(product => product.Id, StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (_settings.ProductLimit.HasValue)
        {
            orderedProducts = orderedProducts.Take(_settings.ProductLimit.Value).ToList();
        }

        summary.ProductsDiscovered = orderedProducts.Count;

        foreach (var listedProduct in orderedProducts)
        {
            cancellationToken.ThrowIfCancellationRequested();

            Product product;
            try
            {
                product = await _client.GetProductAsync(_shopId, listedProduct.Id);
            }
            catch (Exception ex)
            {
                summary.ProductLoadFailures++;
                Console.Error.WriteLine($"Failed to load product {listedProduct.Id}: {ex.Message}");
                await DelayIfNeededAsync(cancellationToken);
                continue;
            }

            summary.ProductsProcessed++;

            if (product.IsLocked)
            {
                summary.ProductsSkippedLocked++;
                Console.WriteLine($"Skipping locked product {product.Id} ({product.Title}).");
                continue;
            }

            if (product.Variants.Count == 0)
            {
                summary.ProductsWithoutVariants++;
                Console.WriteLine($"Skipping product {product.Id} ({product.Title}) because it has no variants.");
                continue;
            }

            Console.WriteLine($"Processing product {product.Id} ({product.Title}) with {product.Variants.Count} variants.");

            var variantUpdates = new List<CreateProductVariant>(product.Variants.Count);
            var changedVariantsForProduct = 0;
            var orderedVariants = product.Variants
                .OrderBy(currentVariant => currentVariant.Id)
                .ToList();

            IEnumerable<ProductVariant> variantsToEvaluate = orderedVariants;
            if (_settings.VariantLimitPerProduct.HasValue)
            {
                variantsToEvaluate = orderedVariants.Take(_settings.VariantLimitPerProduct.Value);
                summary.VariantsSkippedByLimit += Math.Max(0, orderedVariants.Count - _settings.VariantLimitPerProduct.Value);
            }

            var evaluatedVariantIds = new HashSet<int>();

            foreach (var variant in variantsToEvaluate)
            {
                cancellationToken.ThrowIfCancellationRequested();
                evaluatedVariantIds.Add(variant.Id);

                var currentPrice = variant.Price;
                var newPrice = currentPrice;
                var shippingCost = 0;

                try
                {
                    var shipping = await _client.CalculateShippingAsync(_shopId, BuildShippingRequest(product.Id, variant.Id));
                    shippingCost = ResolveShippingCost(shipping, _settings.ShippingMethod);
                    var worstCaseShippingCost = ResolveHighestAvailableShippingCost(shipping);
                    var effectiveShippingCost = Math.Max(shippingCost, worstCaseShippingCost);
                    newPrice = _priceCalculator(effectiveShippingCost, variant.Cost);

                    if (newPrice < 0)
                    {
                        throw new InvalidOperationException("The pricing function returned a negative price.");
                    }
                }
                catch (Exception ex)
                {
                    summary.VariantQuoteFailures++;
                    Console.Error.WriteLine(
                        $"  Variant {variant.Id} ({variant.Title}) quote failed: {ex.Message}. Keeping current price {FormatMinorUnits(currentPrice)}.");
                }

                summary.VariantsEvaluated++;

                if (newPrice != currentPrice)
                {
                    changedVariantsForProduct++;
                    summary.VariantsChanged++;
                    Console.WriteLine(
                        $"  Variant {variant.Id} ({variant.Title}): {FormatMinorUnits(currentPrice)} -> {FormatMinorUnits(newPrice)} | production {FormatMinorUnits(variant.Cost)} | shipping {FormatMinorUnits(shippingCost)}");
                }

                variantUpdates.Add(new CreateProductVariant
                {
                    Id = variant.Id,
                    Price = newPrice,
                    IsEnabled = variant.IsEnabled
                });

                await DelayIfNeededAsync(cancellationToken);
            }

            foreach (var variant in orderedVariants)
            {
                if (evaluatedVariantIds.Contains(variant.Id))
                {
                    continue;
                }

                variantUpdates.Add(new CreateProductVariant
                {
                    Id = variant.Id,
                    Price = variant.Price,
                    IsEnabled = variant.IsEnabled
                });
            }

            if (changedVariantsForProduct == 0)
            {
                summary.ProductsUnchanged++;
                Console.WriteLine("  No price changes needed.");
                continue;
            }

            if (!_settings.ApplyChanges)
            {
                summary.ProductsPlannedForUpdate++;
                Console.WriteLine($"  Dry run: {changedVariantsForProduct} variant prices would be updated.");
                continue;
            }

            try
            {
                await _client.UpdateProductAsync(_shopId, product.Id, new UpdateProductRequest
                {
                    Variants = variantUpdates
                });

                summary.ProductsUpdated++;
                Console.WriteLine($"  Updated {changedVariantsForProduct} variant prices.");
            }
            catch (Exception ex)
            {
                summary.ProductUpdateFailures++;
                Console.Error.WriteLine($"  Failed to update product {product.Id}: {ex.Message}");
            }
        }

        summary.CompletedAt = DateTimeOffset.UtcNow;
        return summary;
    }

    private ShippingCostRequest BuildShippingRequest(string productId, int variantId)
    {
        return new ShippingCostRequest
        {
            LineItems = new List<SubmitOrderLineItem>
            {
                new()
                {
                    ProductId = productId,
                    VariantId = variantId,
                    Quantity = 1
                }
            },
            AddressTo = _settings.ShippingAddress
        };
    }

    private async Task DelayIfNeededAsync(CancellationToken cancellationToken)
    {
        if (_settings.RequestDelayMs > 0)
        {
            await Task.Delay(_settings.RequestDelayMs, cancellationToken);
        }
    }

    private static int ResolveShippingCost(ShippingCostResponse shipping, PricingShippingMethod method)
    {
        return method switch
        {
            PricingShippingMethod.Standard => shipping.Standard,
            PricingShippingMethod.Express => shipping.Express,
            PricingShippingMethod.Priority => shipping.Priority,
            PricingShippingMethod.PrintifyExpress => shipping.PrintifyExpress,
            PricingShippingMethod.Economy => shipping.Economy,
            PricingShippingMethod.LowestAvailable => new[]
            {
                shipping.Standard,
                shipping.Express,
                shipping.Priority,
                shipping.PrintifyExpress,
                shipping.Economy
            }
            .Where(cost => cost > 0)
            .DefaultIfEmpty(0)
            .Min(),
            _ => shipping.Standard
        };
    }

    private static int ResolveHighestAvailableShippingCost(ShippingCostResponse shipping)
    {
        return new[]
        {
            shipping.Standard,
            shipping.Express,
            shipping.Priority,
            shipping.PrintifyExpress,
            shipping.Economy
        }
        .Where(cost => cost > 0)
        .DefaultIfEmpty(0)
        .Max();
    }

    private static string FormatMinorUnits(int minorUnits)
    {
        return (minorUnits / 100m).ToString("0.00", CultureInfo.InvariantCulture);
    }
}

sealed class PricingUpdateRunSummary
{
    public DateTimeOffset StartedAt { get; init; }
    public DateTimeOffset CompletedAt { get; set; }
    public int ProductsDiscovered { get; set; }
    public int ProductsProcessed { get; set; }
    public int ProductsSkippedLocked { get; set; }
    public int ProductsWithoutVariants { get; set; }
    public int ProductsUnchanged { get; set; }
    public int ProductsUpdated { get; set; }
    public int ProductsPlannedForUpdate { get; set; }
    public int ProductLoadFailures { get; set; }
    public int ProductUpdateFailures { get; set; }
    public int VariantsEvaluated { get; set; }
    public int VariantsChanged { get; set; }
    public int VariantsSkippedByLimit { get; set; }
    public int VariantQuoteFailures { get; set; }
}