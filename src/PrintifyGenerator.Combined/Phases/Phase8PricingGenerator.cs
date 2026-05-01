using System.Globalization;
using System.Text.Json;
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
            var intelligence = runtime.FeatureIntelligence;
            var phase1Insight = ReadPhase0Insight(bundle);

            // Write market price guidance artifact so downstream analysis can reference it.
            var priceGuidance = intelligence.GetMarketPriceGuidance(phase1Insight?.Definition);
            var guidancePath = Path.Combine(bundle.DirectoryPath, "phase8.guidance.json");
            File.WriteAllText(guidancePath, JsonSerializer.Serialize(priceGuidance, PrettyJson));

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

                    var priceDecision = CalculateSafePrice(
                        productionPrice,
                        shipping,
                        ResolvePricingProfile(shop.Title),
                        priceGuidance);

                    if (!priceDecision.IsCompetitiveAndProfitable)
                    {
                        // The minimum profitable price for this variant exceeds the market-competitive
                        // ceiling (normalized from Amazon research data). Pushing it at an above-market
                        // price would hurt sales rank and conversion, so we skip the variant entirely.
                        warnings++;
                        Console.WriteLine(
                            $"[Phase8Pricing] SKIPPED product {targetProductId} variant {v.Id}: " +
                            $"minimum profitable price ${priceDecision.ChosenPriceUsd:F2} " +
                            $"exceeds competitive cap ${priceDecision.CompetitiveCapUsd:F2} " +
                            $"(hard cost ${priceDecision.ChosenPriceUsd - priceDecision.EstimatedProfitUsd - priceDecision.MinimumProfitUsd:F2}, " +
                            $"required profit ${priceDecision.MinimumProfitUsd:F2}).");
                        continue;
                    }

                    updatedVariants.Add(new CreateProductVariant
                    {
                        Id = v.Id,
                        Price = priceDecision.ChosenPriceCents,
                        IsEnabled = true
                    });
                }

                if (updatedVariants.Count == 0)
                {
                    Console.WriteLine(
                        $"[Phase8Pricing] Product {targetProductId} ENTIRELY SKIPPED: no variants survived the competitive-profit check.");
                    continue;
                }

                // only update if changed
                if (updatedVariants.Any(v =>
                    product.Variants.Any(ov => ov.Id == v.Id && ov.Price != v.Price)))
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
                    var productionPrice = oldVariant.Cost;
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

        private static readonly ResearchFeeProfile AmazonResearchFeeProfile = new(
            VariableFeePercent: 15.0m,
            FixedFeeDollars: 0.30m,
            SalesTaxPercent: 0.0m,
            InternationalFeePercent: 0.0m);

        private static PricingDecision CalculateSafePrice(
            int productionPrice,
            int shippingPrice,
            MarketplacePricingProfile profile,
            MarketPriceGuidance marketGuidance)
        {
            if (shippingPrice < 0)
                throw new InvalidOperationException("Shipping price cannot be negative.");

            var productionUsd = productionPrice / 100m;
            var shippingUsd = shippingPrice / 100m;
            var hardCostUsd = productionUsd + shippingUsd;

            var minimumProfitablePriceUsd = SolveRequiredRetailPrice(
                hardCostUsd,
                profile.MinimumProfitDollars,
                profile);

            // Research competitiveness comes largely from Amazon data. Convert that consumer price
            // to an equivalent target-marketplace price so fee differences do not underprice us.
            var competitiveAnchorUsd = NormalizeAmazonMarketPriceToMarketplace(
                (decimal)marketGuidance.MedianCompetitivePrice,
                profile);
            var competitiveMaxUsd = NormalizeAmazonMarketPriceToMarketplace(
                (decimal)marketGuidance.SuggestedMaxPrice,
                profile);

            var competitiveCapUsd = 0m;
            if (competitiveAnchorUsd > 0m || competitiveMaxUsd > 0m)
            {
                var fallbackCap = competitiveAnchorUsd > 0m
                    ? competitiveAnchorUsd * 1.10m
                    : competitiveMaxUsd;

                var knownMax = competitiveMaxUsd > 0m ? competitiveMaxUsd : fallbackCap;
                competitiveCapUsd = Math.Max(knownMax, hardCostUsd);
            }

            var canBeCompetitiveAndProfitable =
                competitiveCapUsd <= 0m || minimumProfitablePriceUsd <= competitiveCapUsd;

            var targetUsd = canBeCompetitiveAndProfitable && competitiveCapUsd > 0m
                ? Math.Max(minimumProfitablePriceUsd, Math.Min(competitiveAnchorUsd, competitiveCapUsd))
                : minimumProfitablePriceUsd;

            if (targetUsd <= 0m)
            {
                targetUsd = minimumProfitablePriceUsd;
            }

            var roundedUsd = RoundUpNicePrice(targetUsd);
            var estimatedProfitUsd = EstimateNetProfit(roundedUsd, hardCostUsd, profile);

            return new PricingDecision(
                ChosenPriceCents: (int)(roundedUsd * 100m),
                ChosenPriceUsd: roundedUsd,
                CompetitiveCapUsd: competitiveCapUsd,
                MinimumProfitUsd: profile.MinimumProfitDollars,
                EstimatedProfitUsd: estimatedProfitUsd,
                IsCompetitiveAndProfitable: canBeCompetitiveAndProfitable);
        }

        private static decimal NormalizeAmazonMarketPriceToMarketplace(
            decimal researchConsumerPriceUsd,
            MarketplacePricingProfile targetProfile)
        {
            if (researchConsumerPriceUsd <= 0m)
            {
                return 0m;
            }

            var amazonRetention = 1m - ToRatio(AmazonResearchFeeProfile.SalesTaxPercent)
                                    - ToRatio(AmazonResearchFeeProfile.VariableFeePercent)
                                    - ToRatio(AmazonResearchFeeProfile.InternationalFeePercent);
            if (amazonRetention <= 0m)
            {
                return 0m;
            }

            var targetRetention = 1m - ToRatio(targetProfile.SalesTaxPercent)
                                    - ToRatio(targetProfile.VariableFeePercent)
                                    - ToRatio(targetProfile.InternationalFeePercent);
            if (targetRetention <= 0m)
            {
                return 0m;
            }

            var amazonNetBeforeCost = researchConsumerPriceUsd * amazonRetention - AmazonResearchFeeProfile.FixedFeeDollars;
            if (amazonNetBeforeCost <= 0m)
            {
                return 0m;
            }

            return (amazonNetBeforeCost + targetProfile.FixedFeeDollars) / targetRetention;
        }

        private static decimal SolveRequiredRetailPrice(
            decimal hardCostUsd,
            decimal minimumProfitUsd,
            MarketplacePricingProfile profile)
        {
            var retainedRevenueRatio = 1m - ToRatio(profile.SalesTaxPercent)
                                         - ToRatio(profile.VariableFeePercent)
                                         - ToRatio(profile.InternationalFeePercent);
            if (retainedRevenueRatio <= 0m)
            {
                throw new InvalidOperationException("Retained revenue ratio must be greater than zero.");
            }

            return (hardCostUsd + minimumProfitUsd + profile.FixedFeeDollars) / retainedRevenueRatio;
        }

        private static decimal EstimateNetProfit(
            decimal listingPriceUsd,
            decimal hardCostUsd,
            MarketplacePricingProfile profile)
        {
            var retainedRevenue = listingPriceUsd * (1m - ToRatio(profile.SalesTaxPercent)
                                                       - ToRatio(profile.VariableFeePercent)
                                                       - ToRatio(profile.InternationalFeePercent));
            return retainedRevenue - profile.FixedFeeDollars - hardCostUsd;
        }

        private static decimal ToRatio(decimal percent)
            => percent / 100m;

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
                return new("US", 8.5m, 13.6m, 1.35m, 0.40m, 6m);

            if (shopTitle.Contains("etsy", StringComparison.OrdinalIgnoreCase))
                return new("US", 8.5m, 9.5m, 2.5m, 0.20m, 6m);

            throw new InvalidOperationException($"Unknown shop: {shopTitle}");
        }

        private sealed record ResearchFeeProfile(
            decimal VariableFeePercent,
            decimal FixedFeeDollars,
            decimal SalesTaxPercent,
            decimal InternationalFeePercent);

        private sealed record PricingDecision(
            int ChosenPriceCents,
            decimal ChosenPriceUsd,
            decimal CompetitiveCapUsd,
            decimal MinimumProfitUsd,
            decimal EstimatedProfitUsd,
            bool IsCompetitiveAndProfitable);

        private sealed record MarketplacePricingProfile(
            string CountryCode,
            decimal SalesTaxPercent,
            decimal VariableFeePercent,
            decimal InternationalFeePercent,
            decimal FixedFeeDollars,
            decimal MinimumProfitDollars);
    }
}