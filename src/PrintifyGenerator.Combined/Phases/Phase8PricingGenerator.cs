using System.Globalization;

public static partial class PhaseFactory
{
    private sealed class Phase8PricingGenerator : PhaseGeneratorBase
    {
        public Phase8PricingGenerator()
            : base(8, "Pricing Validation", "id/phase8.txt => Final validated pricing with profit guarantees") { }

        protected override bool IsCompleteCore(PhaseBundle bundle)
            => File.Exists(bundle.ResolvePhaseFile(9, "txt"));

        protected override bool CanRunCore(PhaseBundle bundle)
            => File.Exists(bundle.ResolvePhaseFile(8, "txt"));

        protected override async Task<PhaseExecutionResult> ExecuteCoreAsync(
            PhaseBundle bundle,
            CancellationToken cancellationToken)
        {
            var inputFile = bundle.ResolvePhaseFile(8, "txt");
            var outputFile = bundle.ResolvePhaseFile(9, "txt");

            var runtime = CombinedRuntime.Current;
            var printify = runtime.GetPrintifyClient();

            var shops = await printify.GetShopsAsync();

            var lines = new List<string>();
            var updatedProducts = 0;
            var warnings = 0;

            foreach (var row in File.ReadLines(inputFile))
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (string.IsNullOrWhiteSpace(row))
                    continue;

                var parts = row.Split(',', StringSplitOptions.TrimEntries);
                if (parts.Length < 7)
                    continue;

                var targetProductId = parts[0];
                var sourceProductId = parts[1];
                var variantId = int.Parse(parts[2], CultureInfo.InvariantCulture);
                var shopTitle = parts[3];
                var productionPrice = int.Parse(parts[4], CultureInfo.InvariantCulture);
                var oldShipping = int.Parse(parts[5], CultureInfo.InvariantCulture);
                var oldPrice = int.Parse(parts[6], CultureInfo.InvariantCulture);

                var shop = ResolveShop(shopTitle, shops);
                if (shop is null)
                {
                    warnings++;
                    continue;
                }

                Product product;
                try
                {
                    product = await printify.GetProductAsync(shop.Id, targetProductId);
                }
                catch
                {
                    warnings++;
                    continue;
                }

                var variant = product.Variants.FirstOrDefault(v => v.Id == variantId);
                if (variant is null)
                    continue;

                var profile = ResolvePricingProfile(shop.Title);
                var address = CreateShippingAddress(profile.CountryCode);

                int shipping;
                try
                {
                    shipping = await ResolveShippingLiveSafe(printify, shop.Id, product, variant.Id, address);
                }
                catch
                {
                    warnings++;
                    continue;
                }

                var newPrice = CalculateSafePrice(
                    productionPrice,
                    shipping,
                    profile);

                // only update if changed
                if (newPrice != variant.Price)
                {
                    try
                    {
						await printify.UpdateProductAsync(shop.Id, product.Id,
							new UpdateProductRequest
							{
								Variants = new List<CreateProductVariant>
								{
									new()
									{
										Id = variant.Id,
										Price = newPrice,
										IsEnabled = variant.IsEnabled
									}
								}
							});

                        updatedProducts++;
                    }
                    catch
                    {
                        warnings++;
                        continue;
                    }
                }

                lines.Add(string.Join(",",
                    targetProductId,
                    sourceProductId,
                    variant.Id.ToString(CultureInfo.InvariantCulture),
                    shop.Title,
                    productionPrice,
                    shipping,
                    newPrice));
            }

            File.WriteAllLines(outputFile, lines);

            return PhaseExecutionResult.Done(
                $"Phase9 complete. {lines.Count} rows validated. {updatedProducts} products updated. Warnings: {warnings}");
        }

        // 🔒 Deterministic pricing (NO LOOPS)
        private static int CalculateSafePrice(
            int productionPrice,
            int shippingPrice,
            MarketplacePricingProfile profile)
        {
            var cost = (productionPrice + Math.Max(0, shippingPrice)) / 100m;

            var feeRate =
                (profile.VariableFeePercent + profile.InternationalFeePercent) / 100m;

            var taxRate = profile.SalesTaxPercent / 100m;

            var retained = 1m - ((1m + taxRate) * feeRate);

            if (retained <= 0)
                throw new InvalidOperationException("Invalid fee structure.");

            var required =
                (cost + profile.FixedFeeDollars + profile.MinimumProfitDollars) / retained;

            var rounded = RoundUpNicePrice(required);

            var profit =
                (rounded * retained) - profile.FixedFeeDollars - cost;

            if (profit < profile.MinimumProfitDollars)
            {
                rounded += 1m;
            }

            return checked((int)Math.Ceiling(rounded * 100m));
        }

        private static async Task<int> ResolveShippingLiveSafe(
            PrintifyClient client,
            int shopId,
            Product product,
            int variantId,
            Address address)
        {
            var shipping = await client.CalculateShippingAsync(shopId,
                new ShippingCostRequest
                {
                    LineItems = new List<SubmitOrderLineItem>
                    {
                        new()
                        {
                            ProductId = product.Id,
                            VariantId = variantId,
                            Quantity = 1
                        }
                    },
                    AddressTo = address
                });

            var costs = new[]
            {
                shipping.Standard,
                shipping.Express,
                shipping.Priority,
                shipping.PrintifyExpress,
                shipping.Economy
            }
            .Where(x => x > 0)
            .OrderBy(x => x)
            .ToList();

            if (costs.Count == 0)
                return 0;

            // median instead of max (more realistic, still safe)
            return costs[costs.Count / 2];
        }

        private static decimal RoundUpNicePrice(decimal amount)
        {
            var normalized = Math.Max(amount, 0m);

            var cents = normalized switch
            {
                < 10m => 0.99m,
                < 50m => 0.49m,
                _ => 0.99m,
            };

            var rounded = Math.Floor(normalized) + cents;

            if (rounded < normalized)
                rounded += 1m;

            return decimal.Round(rounded, 2, MidpointRounding.AwayFromZero);
        }

        // reuse your existing helpers
        private static Shop? ResolveShop(string title, IReadOnlyList<Shop> shops)
            => shops.FirstOrDefault(s =>
                string.Equals(s.Title, title, StringComparison.OrdinalIgnoreCase));

        private static Address CreateShippingAddress(string countryCode)
            => new Address
            {
                Country = countryCode,
                City = "Validator",
                Address1 = "1 Validation Way",
                Zip = "10001"
            };

        private static MarketplacePricingProfile ResolvePricingProfile(string shopTitle)
        {
            if (shopTitle.Contains("ebay", StringComparison.OrdinalIgnoreCase))
                return new("US", 8.5m, 13.6m, 1.35m, 0.40m, 40m);

            if (shopTitle.Contains("etsy", StringComparison.OrdinalIgnoreCase))
                return new("US", 8.5m, 9.5m, 2.5m, 0.25m, 40m);

            throw new InvalidOperationException($"Unknown shop: {shopTitle}");
        }

        private sealed record MarketplacePricingProfile(
            string CountryCode,
            decimal SalesTaxPercent,
            decimal VariableFeePercent,
            decimal InternationalFeePercent,
            decimal FixedFeeDollars,
            decimal MinimumProfitDollars);
    }
}