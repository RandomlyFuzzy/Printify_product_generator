using System.Globalization;
using System.Text.Json;

public static partial class PhaseFactory
{
	private sealed class Phase8PricingGenerator : PhaseGeneratorBase
	{
		public Phase8PricingGenerator() : base(8, "Pricing", "id/phase8.txt => NPID,ProducerId,VID,Shop,ProductionPrice,ShippingPrice,MyPricing") { }

		protected override bool IsCompleteCore(PhaseBundle bundle)
			=> File.Exists(bundle.ResolvePhaseFile(8, "txt"));

		protected override bool CanRunCore(PhaseBundle bundle)
			=> File.Exists(bundle.ResolvePhaseFile(7, "txt"));

		protected override async Task<PhaseExecutionResult> ExecuteCoreAsync(PhaseBundle bundle, CancellationToken cancellationToken)
		{
			var outputFile = bundle.ResolvePhaseFile(8, "txt");
			var pricingTargets = LoadPricingTargets(bundle.ResolvePhaseFile(7, "txt"));

			if (pricingTargets.Count == 0)
			{
				File.WriteAllLines(outputFile, Array.Empty<string>());
				return PhaseExecutionResult.Done("No phase7 pricing targets available.");
			}

			var runtime = CombinedRuntime.Current;
			var printify = runtime.GetPrintifyClient();
			var shops = await printify.GetShopsAsync();

			var lines = new List<string>();
			var pricedTargetCount = 0;
			var warningCount = 0;

			foreach (var pricingTarget in pricingTargets)
			{
				cancellationToken.ThrowIfCancellationRequested();

				var targetShop = ResolveShop(pricingTarget.ShopTitle, shops);
				if (targetShop is null)
				{
					warningCount++;
					Console.Error.WriteLine($"[phase8] Could not resolve target shop '{pricingTarget.ShopTitle}' for source product {pricingTarget.SourceProductId}.");
					continue;
				}

				Product product;
				try
				{
					product = await printify.GetProductAsync(targetShop.Id, pricingTarget.TargetProductId);
				}
				catch (Exception ex)
				{
					warningCount++;
					Console.Error.WriteLine($"[phase8] Failed to load target product {pricingTarget.TargetProductId} in shop '{targetShop.Title}' ({targetShop.Id}): {ex.Message}");
					continue;
				}

				if (product.Variants.Count == 0)
				{
					warningCount++;
					Console.Error.WriteLine($"[phase8] Target product {pricingTarget.TargetProductId} in shop '{targetShop.Title}' has no variants to price.");
					continue;
				}

				var pricingProfile = ResolvePricingProfile(targetShop.Title);
				var shippingAddress = CreateShippingAddress(pricingProfile.CountryCode);
				var variantUpdates = new List<CreateProductVariant>(product.Variants.Count);
				var targetLines = new List<string>(product.Variants.Count);

				try
				{
					foreach (var variant in product.Variants.OrderBy(variant => variant.Id))
					{
						cancellationToken.ThrowIfCancellationRequested();

						var shippingPrice = await ResolveShippingPriceAsync(
							printify,
							targetShop.Id,
							product,
							variant.Id,
							pricingProfile.CountryCode,
							shippingAddress);

						var salePrice = CalculatePrice(variant.Cost, shippingPrice, pricingProfile);

						variantUpdates.Add(new CreateProductVariant
						{
							Id = variant.Id,
							Price = salePrice,
							IsEnabled = variant.IsEnabled,
						});

						targetLines.Add(string.Join(",",
							FormatCsvField(pricingTarget.TargetProductId),
							FormatCsvField(pricingTarget.SourceProductId),
							variant.Id.ToString(CultureInfo.InvariantCulture),
							FormatCsvField(targetShop.Title),
							variant.Cost.ToString(CultureInfo.InvariantCulture),
							shippingPrice.ToString(CultureInfo.InvariantCulture),
							salePrice.ToString(CultureInfo.InvariantCulture)));
					}

					await printify.UpdateProductAsync(targetShop.Id, product.Id, new UpdateProductRequest
					{
						Variants = variantUpdates
					});

					lines.AddRange(targetLines);
					pricedTargetCount++;
				}
				catch (Exception ex)
				{
					warningCount++;
					Console.Error.WriteLine($"[phase8] Failed to update priced variants for target product {pricingTarget.TargetProductId} in shop '{targetShop.Title}' ({targetShop.Id}): {ex.Message}");
				}
			}

			File.WriteAllLines(outputFile, lines);
			var warningSuffix = warningCount > 0
				? $" Warning-only target failures: {warningCount}."
				: string.Empty;
			return PhaseExecutionResult.Done($"Created {Path.GetFileName(outputFile)} with {lines.Count} price row(s) across {pricedTargetCount} priced target product(s).{warningSuffix}");
		}

		private static List<PricingTarget> LoadPricingTargets(string phase7File)
		{
			var pricingTargets = new List<PricingTarget>();
			if (!File.Exists(phase7File))
			{
				return pricingTargets;
			}

			foreach (var rawLine in File.ReadLines(phase7File))
			{
				var line = rawLine.Trim();
				if (string.IsNullOrWhiteSpace(line))
				{
					continue;
				}

				if (line.StartsWith('{'))
				{
					try
					{
						var manifest = JsonSerializer.Deserialize<PricingTargetManifest>(line);
						if (string.IsNullOrWhiteSpace(manifest?.pid) || manifest.targets is null)
						{
							continue;
						}

						pricingTargets.AddRange(manifest.targets
							.Where(target => !string.IsNullOrWhiteSpace(target.ProductId) && !string.IsNullOrWhiteSpace(target.ShopTitle))
							.Select(target => new PricingTarget(
								SourceProductId: manifest.pid,
								ShopTitle: target.ShopTitle!.Trim(),
								TargetProductId: target.ProductId!.Trim())));
					}
					catch (JsonException)
					{
					}

					continue;
				}

				var parts = line.Split(',', 3, StringSplitOptions.TrimEntries);
				if (parts.Length < 3 || string.IsNullOrWhiteSpace(parts[0]) || string.IsNullOrWhiteSpace(parts[1]) || string.IsNullOrWhiteSpace(parts[2]))
				{
					continue;
				}

				pricingTargets.Add(new PricingTarget(
					SourceProductId: parts[0],
					ShopTitle: parts[1],
					TargetProductId: parts[2]));
			}

			return pricingTargets;
		}

		private static Shop? ResolveShop(string shopTitle, IReadOnlyList<Shop> shops)
		{
			if (string.IsNullOrWhiteSpace(shopTitle))
			{
				return null;
			}

			return shops.FirstOrDefault(shop =>
				string.Equals(shop.Title, shopTitle, StringComparison.OrdinalIgnoreCase)
				|| shop.Title.Contains(shopTitle, StringComparison.OrdinalIgnoreCase)
				|| shopTitle.Contains(shop.Title, StringComparison.OrdinalIgnoreCase));
		}

		private static async Task<int> ResolveShippingPriceAsync(
			PrintifyClient printify,
			int shopId,
			Product product,
			int variantId,
			string countryCode,
			Address shippingAddress)
		{
			var cachedShippingPrice = ResolveCachedShippingPrice(product.BlueprintId, product.PrintProviderId, variantId, countryCode);
			if (cachedShippingPrice > 0)
			{
				return cachedShippingPrice;
			}

			var shipping = await printify.CalculateShippingAsync(shopId, new ShippingCostRequest
			{
				LineItems = new List<SubmitOrderLineItem>
				{
					new()
					{
						ProductId = product.Id,
						VariantId = variantId,
						Quantity = 1,
					}
				},
				AddressTo = shippingAddress,
			});

			return new[]
			{
				shipping.Standard,
				shipping.Express,
				shipping.Priority,
				shipping.PrintifyExpress,
				shipping.Economy,
			}
			.Where(cost => cost > 0)
			.DefaultIfEmpty(0)
			.Max();
		}

		private static int ResolveCachedShippingPrice(int blueprintId, int printProviderId, int variantId, string countryCode)
		{
			var quotes = PrintifyBlueprintDatabase.GetShippingQuotes(blueprintId, printProviderId)
				.Where(quote => quote.VariantId == variantId)
				.ToList();

			if (quotes.Count == 0)
			{
				return 0;
			}

			var exactCountryMatch = quotes
				.Where(quote => string.Equals(quote.Region, countryCode, StringComparison.OrdinalIgnoreCase))
				.Select(quote => quote.FirstItemCost ?? 0)
				.Where(cost => cost > 0)
				.ToList();
			if (exactCountryMatch.Count > 0)
			{
				return exactCountryMatch.Max();
			}

			var restOfWorldMatch = quotes
				.Where(quote => string.Equals(quote.Region, "REST_OF_THE_WORLD", StringComparison.OrdinalIgnoreCase))
				.Select(quote => quote.FirstItemCost ?? 0)
				.Where(cost => cost > 0)
				.ToList();
			if (restOfWorldMatch.Count > 0)
			{
				return restOfWorldMatch.Max();
			}

			var fallbackMatch = quotes
				.Select(quote => quote.FirstItemCost ?? 0)
				.Where(cost => cost > 0)
				.ToList();
			return fallbackMatch.Count > 0 ? fallbackMatch.Max() : 0;
		}

		private static Address CreateShippingAddress(string countryCode)
		{
			var normalizedCountryCode = string.IsNullOrWhiteSpace(countryCode) ? "US" : countryCode.Trim().ToUpperInvariant();
			return new Address
			{
				FirstName = "Combined",
				LastName = "Runner",
				Email = "combined-runner@example.com",
				Phone = "0000000000",
				Country = normalizedCountryCode,
				Region = normalizedCountryCode == "US" ? "NY" : string.Empty,
				Address1 = "1 Combined Runner Way",
				Address2 = string.Empty,
				City = "Runner",
				Zip = normalizedCountryCode == "US" ? "10001" : "10001",
			};
		}

		private static MarketplacePricingProfile ResolvePricingProfile(string shopTitle)
		{
			if (shopTitle.Contains("ebay", StringComparison.OrdinalIgnoreCase))
			{
				return new MarketplacePricingProfile(
					CountryCode: "US",
					SalesTaxPercent: 8.5m,
					VariableFeePercent: 13.60m,
					InternationalFeePercent: 1.35m,
					FixedFeeDollars: 0.40m,
					MinimumProfitDollars: 40.00m);
			}

			if (shopTitle.Contains("etsy", StringComparison.OrdinalIgnoreCase))
			{
				return new MarketplacePricingProfile(
					CountryCode: "US",
					SalesTaxPercent: 8.5m,
					VariableFeePercent: 9.50m,
					InternationalFeePercent: 2.50m,
					FixedFeeDollars: 0.25m,
					MinimumProfitDollars: 40.00m);
			}
			throw new InvalidOperationException($"Unrecognized shop title '{shopTitle}' for pricing profile resolution. Expected to contain 'Etsy' or 'eBay'.");

			return new MarketplacePricingProfile(
				CountryCode: "US",
				SalesTaxPercent: 0m,
				VariableFeePercent: 0m,
				InternationalFeePercent: 0m,
				FixedFeeDollars: 0m,
				MinimumProfitDollars: 40.00m);
		}

		private static int CalculatePrice(int productionPrice, int shippingPrice, MarketplacePricingProfile profile)
		{
			if (productionPrice < 0)
			{
				throw new ArgumentOutOfRangeException(nameof(productionPrice), "Production price cannot be negative.");
			}

			var normalizedShippingPrice = Math.Max(0, shippingPrice);
			var totalCost = (productionPrice + normalizedShippingPrice) / 100m;
			var variableFeeRate = (profile.VariableFeePercent + profile.InternationalFeePercent) / 100m;
			var retainedRevenueRate = 1m - ((1m + (profile.SalesTaxPercent / 100m)) * variableFeeRate);

			if (retainedRevenueRate <= 0m)
			{
				throw new InvalidOperationException("Marketplace fee profile retains no revenue. Adjust the configured fee percentages.");
			}

			var requiredSalePrice = (totalCost + profile.FixedFeeDollars + profile.MinimumProfitDollars) / retainedRevenueRate;
			var roundedSalePrice = RoundUpNicePrice(requiredSalePrice);
			while (CalculateEstimatedProfit(roundedSalePrice, totalCost, profile, retainedRevenueRate) < profile.MinimumProfitDollars)
			{
				roundedSalePrice = RoundUpNicePrice(roundedSalePrice + 1m);
			}

			return checked((int)Math.Ceiling(roundedSalePrice * 100m));
		}

		private static decimal CalculateEstimatedProfit(
			decimal salePrice,
			decimal totalCost,
			MarketplacePricingProfile profile,
			decimal retainedRevenueRate)
		{
			return (salePrice * retainedRevenueRate) - profile.FixedFeeDollars - totalCost;
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
			{
				rounded += 1m;
			}

			return decimal.Round(rounded, 2, MidpointRounding.AwayFromZero);
		}

		private static string FormatCsvField(string value)
		{
			if (!value.Contains(',') && !value.Contains('"'))
			{
				return value;
			}

			return $"\"{value.Replace("\"", "\"\"", StringComparison.Ordinal)}\"";
		}

		private sealed class PricingTargetManifest
		{
			public string pid { get; set; } = string.Empty;
			public List<LegacyPricingTarget>? targets { get; set; }
		}

		private sealed class LegacyPricingTarget
		{
			public string? ShopTitle { get; set; }
			public int? ShopId { get; set; }
			public string? ProductId { get; set; }
		}

		private sealed record PricingTarget(string SourceProductId, string ShopTitle, string TargetProductId);

		private sealed record MarketplacePricingProfile(
			string CountryCode,
			decimal SalesTaxPercent,
			decimal VariableFeePercent,
			decimal InternationalFeePercent,
			decimal FixedFeeDollars,
			decimal MinimumProfitDollars);
	}
		
}
