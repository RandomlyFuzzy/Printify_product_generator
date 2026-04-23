using System.Globalization;
using System.Text.Json;

public static partial class PhaseFactory
{
	private sealed class Phase8PricingGenerator : PhaseGeneratorBase
	{
		public Phase8PricingGenerator() : base(8, "Pricing", "id/phase8.txt => PID,ProdId,VID,Shop,ProductionPrice,Price") { }

		protected override bool IsCompleteCore(PhaseBundle bundle)
			=> File.Exists(bundle.ResolvePhaseFile(8, "txt"));

		protected override bool CanRunCore(PhaseBundle bundle)
			=> File.Exists(bundle.ResolvePhaseFile(7, "txt"));

		protected override async Task<PhaseExecutionResult> ExecuteCoreAsync(PhaseBundle bundle, CancellationToken cancellationToken)
		{
			var pids = bundle.ReadPhase4ProductIds();
			var outputFile = bundle.ResolvePhaseFile(8, "txt");
			var pricingTargets = LoadPricingTargets(bundle.ResolvePhaseFile(7, "txt"));
			if (pids.Count == 0)
			{
				File.WriteAllLines(outputFile, Array.Empty<string>());
				return PhaseExecutionResult.Done("No products available for pricing.");
			}

			var runtime = CombinedRuntime.Current;
			var printify = runtime.GetPrintifyClient();
			var stagingShopId = await runtime.ResolveShopIdAsync("Staging");
			var shops = await printify.GetShopsAsync();

			var shippingAddress = new Address
			{
				FirstName = "Combined",
				LastName = "Runner",
				Email = "combined-runner@example.com",
				Phone = "0000000000",
				Country = "US",
				Region = "",
				Address1 = "1 Combined Runner Way",
				Address2 = "",
				City = "Runner",
				Zip = "10001",
			};

			var lines = new List<string>();
			foreach (var pid in pids)
			{
				cancellationToken.ThrowIfCancellationRequested();

				var stagingProduct = await printify.GetProductAsync(stagingShopId, pid);
				var targets = GetTargetsForProduct(pid, pricingTargets, stagingShopId);

				foreach (var target in targets)
				{
					cancellationToken.ThrowIfCancellationRequested();

					var resolvedShopId = ResolveShopId(target, shops) ?? stagingShopId;
					var resolvedProductId = string.IsNullOrWhiteSpace(target.ProductId) ? pid : target.ProductId!;
					var resolvedShopTitle = ResolveShopTitle(target, shops, resolvedShopId);
					var pricingProduct = stagingProduct;
					var pricingShopId = stagingShopId;
					var pricingProductId = pid;

					if (resolvedShopId != stagingShopId || !string.Equals(resolvedProductId, pid, StringComparison.Ordinal))
					{
						try
						{
							pricingProduct = await printify.GetProductAsync(resolvedShopId, resolvedProductId);
							pricingShopId = resolvedShopId;
							pricingProductId = resolvedProductId;
						}
						catch (Exception ex)
						{
							Console.Error.WriteLine($"[phase8] Falling back to staging product {pid} for shop '{resolvedShopTitle}' because target product '{resolvedProductId}' could not be loaded: {ex.Message}");
						}
					}

					var variant = pricingProduct.Variants.FirstOrDefault(v => v.IsEnabled) ?? pricingProduct.Variants.FirstOrDefault();
					if (variant is null)
					{
						continue;
					}

					var shipping = await printify.CalculateShippingAsync(pricingShopId, new ShippingCostRequest
					{
						LineItems = new List<SubmitOrderLineItem>
						{
							new()
							{
								ProductId = pricingProductId,
								VariantId = variant.Id,
								Quantity = 1,
							}
						},
						AddressTo = shippingAddress,
					});

					var shippingCost = new[]
					{
						shipping.Standard,
						shipping.Express,
						shipping.Priority,
						shipping.PrintifyExpress,
						shipping.Economy,
					}
					.Where(cost => cost > 0)
					.DefaultIfEmpty(shipping.Standard)
					.Min();

					var profile = ResolvePricingProfile(resolvedShopTitle);
					var salePrice = CalculatePrice(shippingCost, variant.Cost, profile);
					lines.Add($"{pid},{pricingProductId},{variant.Id},{resolvedShopTitle},{variant.Cost.ToString(CultureInfo.InvariantCulture)},{salePrice.ToString(CultureInfo.InvariantCulture)}");
				}
			}

			File.WriteAllLines(outputFile, lines);
			return PhaseExecutionResult.Done($"Created {Path.GetFileName(outputFile)} with {lines.Count} price row(s).");
		}

		private static List<PricingTarget> GetTargetsForProduct(
			string pid,
			IReadOnlyDictionary<string, List<PricingTarget>> pricingTargets,
			int stagingShopId)
		{
			if (pricingTargets.TryGetValue(pid, out var storedTargets) && storedTargets.Count > 0)
			{
				return storedTargets;
			}

			return new List<PricingTarget>
			{
				new()
				{
					ShopTitle = "Staging",
					ShopId = stagingShopId,
					ProductId = pid,
				}
			};
		}

		private static Dictionary<string, List<PricingTarget>> LoadPricingTargets(string phase7File)
		{
			var targetsByPid = new Dictionary<string, List<PricingTarget>>(StringComparer.OrdinalIgnoreCase);
			if (!File.Exists(phase7File))
			{
				return targetsByPid;
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
						if (!string.IsNullOrWhiteSpace(manifest?.pid))
						{
							targetsByPid[manifest.pid] = (manifest.targets ?? new List<PricingTarget>())
								.Where(target => !string.IsNullOrWhiteSpace(target.ShopTitle) || target.ShopId.HasValue)
								.ToList();
						}
					}
					catch (JsonException)
					{
					}

					continue;
				}

				var parts = line.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
				if (parts.Length < 2)
				{
					continue;
				}

				var pid = parts[0];
				targetsByPid[pid] = parts
					.Skip(1)
					.Select(store => new PricingTarget { ShopTitle = store })
					.ToList();
			}

			return targetsByPid;
		}

		private static int? ResolveShopId(PricingTarget target, IReadOnlyList<Shop> shops)
		{
			if (target.ShopId.HasValue)
			{
				return target.ShopId.Value;
			}

			if (string.IsNullOrWhiteSpace(target.ShopTitle))
			{
				return null;
			}

			return shops.FirstOrDefault(shop =>
				string.Equals(shop.Title, target.ShopTitle, StringComparison.OrdinalIgnoreCase)
				|| shop.Title.Contains(target.ShopTitle, StringComparison.OrdinalIgnoreCase)
				|| target.ShopTitle.Contains(shop.Title, StringComparison.OrdinalIgnoreCase))?.Id;
		}

		private static string ResolveShopTitle(PricingTarget target, IReadOnlyList<Shop> shops, int resolvedShopId)
		{
			if (!string.IsNullOrWhiteSpace(target.ShopTitle))
			{
				return target.ShopTitle!;
			}

			return shops.FirstOrDefault(shop => shop.Id == resolvedShopId)?.Title ?? "Staging";
		}

		private static MarketplacePricingProfile ResolvePricingProfile(string shopTitle)
		{
			if (shopTitle.Contains("staging", StringComparison.OrdinalIgnoreCase))
			{
				return new MarketplacePricingProfile(
					SalesTaxPercent: 0m,
					VariableFeePercent: 0m,
					FixedFeeDollars: 0m,
					TargetProfitPercent: 7.5m);
			}

			if (shopTitle.Contains("ebay", StringComparison.OrdinalIgnoreCase))
			{
				return new MarketplacePricingProfile(
					SalesTaxPercent: 8.0m,
					VariableFeePercent: 16.60m,
					FixedFeeDollars: 0.40m,
					TargetProfitPercent: 7.5m);
			}

			if (shopTitle.Contains("etsy", StringComparison.OrdinalIgnoreCase))
			{
				return new MarketplacePricingProfile(
					SalesTaxPercent: 8.0m,
					VariableFeePercent: 9.50m,
					FixedFeeDollars: 0.25m,
					TargetProfitPercent: 7.5m);
			}

			throw new InvalidOperationException($"No pricing profile available for shop '{shopTitle}'. Add a case for it in {nameof(ResolvePricingProfile)}.");
		}

		private static int CalculatePrice(int shippingPrice, int productionPrice, MarketplacePricingProfile profile)
		{
			var totalCost = (productionPrice + shippingPrice) / 100m;
			var requiredNet = totalCost * (1m + profile.TargetProfitPercent / 100m);
			var variableFeeRate = (1m + profile.SalesTaxPercent / 100m) * (profile.VariableFeePercent / 100m);
			var retainedRevenueRate = 1m - variableFeeRate;

			if (retainedRevenueRate <= 0m)
			{
				throw new InvalidOperationException("Marketplace fee profile retains no revenue. Adjust the configured fee percentages.");
			}

			var requiredSalePrice = (requiredNet + profile.FixedFeeDollars) / retainedRevenueRate;
			var roundedSalePrice = RoundUpNicePrice(requiredSalePrice);
			var minimumPrice = RoundUpNicePrice(totalCost);
			var finalPrice = Math.Max(roundedSalePrice, minimumPrice);
			return checked((int)(finalPrice * 100m));
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

			return rounded;
		}

		private sealed class PricingTargetManifest
		{
			public string pid { get; set; } = string.Empty;
			public List<PricingTarget>? targets { get; set; }
		}

		private sealed class PricingTarget
		{
			public string? ShopTitle { get; set; }
			public int? ShopId { get; set; }
			public string? ProductId { get; set; }
		}

		private sealed record MarketplacePricingProfile(
			decimal SalesTaxPercent,
			decimal VariableFeePercent,
			decimal FixedFeeDollars,
			decimal TargetProfitPercent);
	}
}
