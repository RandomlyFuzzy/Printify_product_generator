using System.Globalization;
using Microsoft.VisualBasic;

public static partial class PhaseFactory
{
    private sealed class Phase8PricingGenerator : PhaseGeneratorBase
    {
        public Phase8PricingGenerator()
            : base(8, "Pricing Validation", "id/phase8.txt => Final validated pricing with profit guarantees") { }

        protected override bool IsCompleteCore(PhaseBundle bundle)
            => File.Exists(bundle.ResolvePhaseFile(8, "txt"))&& new FileInfo(bundle.ResolvePhaseFile(8, "txt")).Length > 0;
    
        protected override bool CanRunCore(PhaseBundle bundle)
            => File.Exists(bundle.ResolvePhaseFile(7, "txt"));

        protected override async Task<PhaseExecutionResult> ExecuteCoreAsync(
            PhaseBundle bundle,
            CancellationToken cancellationToken)
        {
            var inputFile = bundle.ResolvePhaseFile(7, "txt");
            var outputFile = bundle.ResolvePhaseFile(8, "txt");

            var runtime = CombinedRuntime.Current;
            var printify = runtime.GetPrintifyClient();

            var shops = await printify.GetShopsAsync();

            var lines = new List<string>();
            var updatedProducts = 0;
            var warnings = 0;

            //pid,shopname,newpid
            //69eb5a3838f9f004a708d9cd,My Etsy Store,69ed4322293dd6baa00733bc
            foreach (var row in File.ReadLines(inputFile))
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (string.IsNullOrWhiteSpace(row))
                    continue;

                var parts = row.Split(',', StringSplitOptions.TrimEntries);
                if (parts.Length < 3)
                    continue;

                var _ = parts[0];
                var shopTitle = parts[1];
                var targetProductId = parts[2];

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

                var variant = product.Variants.FirstOrDefault();
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

                var updatedVariants = new List<CreateProductVariant>();

                foreach (var v in product.Variants)
                {
                    if (!v.IsEnabled)
                        continue;

                    var productionPrice = v.Cost;

                    var newPrice = CalculateSafePrice(
                        productionPrice,
                        shipping,
                        shop.Title);

                    updatedVariants.Add(new CreateProductVariant
                    {
                        Id = v.Id,
                        Price = newPrice,
                        IsEnabled = true
                    });
                }

                // only update if changed
                if (updatedVariants.Any(v => v.Price != variant.Price))
                {
                    try
                    {
						await printify.UpdateProductAsync(shop.Id, product.Id,
							new UpdateProductRequest
							{
								Variants = updatedVariants
							});

                        updatedProducts++;
                    }
                    catch
                    {
                        warnings++;
                        continue;
                    }
                }
                foreach (var v in updatedVariants)
                {
                    var oldVariant = product.Variants.First(ov => ov.Id == v.Id);
                    var productionPrice = oldVariant.Price;
                    var newPrice = v.Price;
                    lines.Add(string.Join(",",
                        targetProductId,
                        oldVariant.Id.ToString(CultureInfo.InvariantCulture),
                        shop.Title,
                        productionPrice,
                        shipping,
                        newPrice));
                }
            }

            File.WriteAllLines(outputFile, lines);

            return PhaseExecutionResult.Done(
                $"Phase9 complete. {lines.Count} rows validated. {updatedProducts} products updated. Warnings: {warnings}");
        }

        // 🔒 Deterministic pricing (NO LOOPS)
        private static int CalculateSafePrice(
            int productionPrice,
            int shippingPrice,
            string profile)
        {

            profile = profile.ToLowerInvariant();

            decimal minimumPrice = -1m; //amount to make 0% profit, will be adjusted by profile

            switch (profile)
            {
                case "ebay":
                    minimumPrice = CalcuateEbayMinimumPrice(productionPrice/100m, shippingPrice/100m);
                    break;
                case "my etsy store":
                    minimumPrice = CalcuateEtsyMinimumPrice(productionPrice/100m, shippingPrice/100m);
                    break;
                // add more countries as needed
                default:
                    throw new InvalidOperationException($"Unknown pricing profile: {profile}");
            }


            if (shippingPrice < 0)
                throw new InvalidOperationException("Shipping price cannot be negative.");

            switch (profile)
            {
                case "ebay":
                    minimumPrice *= 1.03m;
                    break;
                case "my etsy store":
                    minimumPrice *= 1.02m;
                    break;
                // add more countries as needed
                default:
                    throw new InvalidOperationException($"Unknown pricing profile: {profile}");
            }

            minimumPrice = RoundUpNicePrice(minimumPrice)*100m;

            return (int)minimumPrice;
        }

        private static decimal CalcuateEbayMinimumPrice(decimal productionPrice, decimal shippingPrice)
        {
            var TaxedCost = productionPrice + shippingPrice;
            TaxedCost *= 1.2m;//Eu

            //ebays cut
            var runningCosts = 0.4m;//sale fee
            runningCosts += TaxedCost*0.136m;//ebay variable fee
            runningCosts += TaxedCost*0.018m;//international fee
            runningCosts *= 1.2m;//vat

            return TaxedCost + runningCosts;
        }

        private static decimal CalcuateEtsyMinimumPrice(decimal productionPrice, decimal shippingPrice)
        {
            //Includes listing fee ($0.20), transaction fee (6.5%), and Payment processing fee (3% + $0.25). Also includes off-site ad fees (if enabled).
            var TaxedCost = productionPrice + shippingPrice;
            TaxedCost *= 1.2m;//Eu

            //Includes listing fee ($0.20), transaction fee (6.5%), and Payment processing fee (3% + $0.25). Also includes off-site ad fees (if enabled).
            var runningCosts = 0.2m;//listing fee
            runningCosts += TaxedCost*0.065m;//transaction fee
            runningCosts += TaxedCost*0.03m + 0.25m;//payment processing fee
            runningCosts *= 1.2m;//vat

            return TaxedCost + runningCosts; //added listing fee
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
                return new("US", 8.5m, 13.6m, 1.35m, 0.40m, 1m);

            if (shopTitle.Contains("etsy", StringComparison.OrdinalIgnoreCase))
                return new("US", 8.5m, 9.5m, 2.5m, 0.20m, 1m);

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