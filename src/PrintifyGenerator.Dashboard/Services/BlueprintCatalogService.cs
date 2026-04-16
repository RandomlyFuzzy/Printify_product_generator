using System.Globalization;
using System.Text.Json;
using Microsoft.Extensions.Options;
using PrintifyGenerator.Dashboard.Models;

namespace PrintifyGenerator.Dashboard.Services;

public sealed class BlueprintCatalogService
{
    private const string DefaultCountryCode = "GB";
    private const string RestOfWorldCountryCode = "REST_OF_THE_WORLD";
    private const string ProbeImageUrl = "https://dummyimage.com/1200x1200/f6efe3/122029.png&text=Printify+Cost+Probe";
    private const int MaxProbeVariantBatchSize = 100;
    private const int PricingLookupRequestLimitPerMinute = 180;
    private const int MaxDefaultEnabledVariantsPerBlueprint = 5;
    private const int MaxDefaultEnabledShippingCostMinorUnits = 750;

    private readonly IWebHostEnvironment _environment;
    private readonly IOptionsMonitor<DashboardOptions> _options;

    public BlueprintCatalogService(IWebHostEnvironment environment, IOptionsMonitor<DashboardOptions> options)
    {
        _environment = environment;
        _options = options;
    }

    public BlueprintCatalogSnapshot LoadSnapshot(string? countryCode, string? searchTerm, bool enabledOnly)
    {
        var normalizedSearchTerm = (searchTerm ?? string.Empty).Trim();
        var dataRoot = ResolveDataRoot();
        var settings = BlueprintCountrySettingsStore.Load(dataRoot);
        var normalizedCountryCode = NormalizeCountryCode(countryCode, settings);
        var sharedLivePricingByBlueprintId = BuildSharedLivePricingLookup(settings);
        var blueprintApi = CreateQueryApi(dataRoot);
        var countrySelection = FindCountrySelection(settings, normalizedCountryCode);
        if (countrySelection is null)
        {
            countrySelection = CreateDefaultCountrySelection(
                settings,
                normalizedCountryCode,
                blueprintApi,
                sharedLivePricingByBlueprintId);
            BlueprintCountrySettingsStore.Save(dataRoot, settings);
        }

        var blueprints = blueprintApi.GetAllBlueprintDetails()
            .Select(detail => BuildBlueprintNode(
                blueprintApi,
                detail,
                countrySelection,
                sharedLivePricingByBlueprintId,
                normalizedCountryCode))
            .Where(node => node is not null)
            .Select(node => node!)
            .Where(node => MatchesSearch(node, normalizedSearchTerm))
            .Where(node => !enabledOnly || node.IsEnabled || node.EnabledVariantCount > 0)
            .OrderByDescending(node => node.IsEnabled || node.EnabledVariantCount > 0)
            .ThenBy(node => node.Title, StringComparer.OrdinalIgnoreCase)
            .ToList();

        var latestProbe = blueprints
            .Where(node => node.LivePricingUpdatedAtUtc.HasValue)
            .OrderByDescending(node => node.LivePricingUpdatedAtUtc)
            .FirstOrDefault();

        return new BlueprintCatalogSnapshot(
            CountryCode: normalizedCountryCode,
            SearchTerm: normalizedSearchTerm,
            EnabledOnly: enabledOnly,
            SettingsPath: BlueprintCountrySettingsStore.GetSettingsPath(dataRoot),
            SavedCountryCodes: settings.Countries
                .Select(country => country.CountryCode)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(code => code, StringComparer.OrdinalIgnoreCase)
                .ToList(),
            Blueprints: blueprints,
            BlueprintCount: blueprints.Count,
            ProviderCount: blueprints.Sum(node => node.Providers.Count),
            VariantCount: blueprints.Sum(node => node.VariantCount),
            EnabledBlueprintCount: blueprints.Count(node => node.IsEnabled),
            EnabledVariantCount: blueprints.Sum(node => node.EnabledVariantCount),
            LivePricedBlueprintCount: blueprints.Count(node => node.LivePricingUpdatedAtUtc.HasValue),
            LastLiveProbeAtUtc: latestProbe?.LivePricingUpdatedAtUtc,
            LastProbeShopLabel: latestProbe?.ProbeShopLabel);
    }

    public void SetBlueprintEnabled(string countryCode, int blueprintId, bool isEnabled)
    {
        if (blueprintId <= 0)
            throw new InvalidOperationException("A valid blueprint ID is required.");

        var dataRoot = ResolveDataRoot();
        var settings = BlueprintCountrySettingsStore.Load(dataRoot);
        var normalizedCountryCode = NormalizeCountryCode(countryCode, settings);
        var blueprintApi = CreateQueryApi(dataRoot);
        var sharedLivePricingByBlueprintId = BuildSharedLivePricingLookup(settings);
        var countrySelection = GetOrCreateCountrySelection(
            settings,
            normalizedCountryCode,
            blueprintApi,
            sharedLivePricingByBlueprintId);
        var blueprintSelection = GetOrCreateBlueprintSelection(countrySelection, blueprintId);

        blueprintSelection.Enabled = isEnabled;
        blueprintSelection.UpdatedAtUtc = DateTime.UtcNow;
        countrySelection.UpdatedAtUtc = DateTime.UtcNow;

        BlueprintCountrySettingsStore.Save(dataRoot, settings);
    }

    public void SetVariantMode(string countryCode, int blueprintId, int providerId, int variantId, string? mode)
    {
        if (blueprintId <= 0 || providerId <= 0 || variantId <= 0)
            throw new InvalidOperationException("Blueprint, provider, and variant IDs are required.");

        var dataRoot = ResolveDataRoot();
        var settings = BlueprintCountrySettingsStore.Load(dataRoot);
        var normalizedCountryCode = NormalizeCountryCode(countryCode, settings);
        var blueprintApi = CreateQueryApi(dataRoot);
        var sharedLivePricingByBlueprintId = BuildSharedLivePricingLookup(settings);
        var countrySelection = GetOrCreateCountrySelection(
            settings,
            normalizedCountryCode,
            blueprintApi,
            sharedLivePricingByBlueprintId);
        var blueprintSelection = GetOrCreateBlueprintSelection(countrySelection, blueprintId);

        blueprintSelection.VariantOverrides.RemoveAll(entry =>
            entry.ProviderId == providerId && entry.VariantId == variantId);

        var normalizedMode = BlueprintVariantSelectionModes.Normalize(mode);
        if (normalizedMode != BlueprintVariantSelectionModes.Inherit)
        {
            blueprintSelection.VariantOverrides.Add(new BlueprintVariantOverrideEntry
            {
                ProviderId = providerId,
                VariantId = variantId,
                Mode = normalizedMode,
                UpdatedAtUtc = DateTime.UtcNow
            });
        }

        blueprintSelection.UpdatedAtUtc = DateTime.UtcNow;
        countrySelection.UpdatedAtUtc = DateTime.UtcNow;

        BlueprintCountrySettingsStore.Save(dataRoot, settings);
    }

    public void SetProviderEnabled(string countryCode, int blueprintId, int providerId, bool isEnabled)
    {
        if (blueprintId <= 0 || providerId <= 0)
            throw new InvalidOperationException("Blueprint and provider IDs are required.");

        var dataRoot = ResolveDataRoot();
        var settings = BlueprintCountrySettingsStore.Load(dataRoot);
        var normalizedCountryCode = NormalizeCountryCode(countryCode, settings);
        var blueprintApi = CreateQueryApi(dataRoot);
        var sharedLivePricingByBlueprintId = BuildSharedLivePricingLookup(settings);
        var countrySelection = GetOrCreateCountrySelection(
            settings,
            normalizedCountryCode,
            blueprintApi,
            sharedLivePricingByBlueprintId);
        var blueprintSelection = GetOrCreateBlueprintSelection(countrySelection, blueprintId);
        var detail = blueprintApi.GetBlueprintDetail(blueprintId);
        var providerDetail = detail.PrintProviders.FirstOrDefault(provider => provider.Provider.Id == providerId)
            ?? throw new InvalidOperationException($"Provider {providerId} was not found on blueprint {blueprintId}.");
        var allowedVariantIds = BuildCountryShippingLookup(providerDetail.Shipping, normalizedCountryCode)
            .Keys
            .ToHashSet();

        if (allowedVariantIds.Count == 0)
            throw new InvalidOperationException($"Provider {providerId} does not ship to {normalizedCountryCode}.");

        blueprintSelection.VariantOverrides.RemoveAll(entry =>
            entry.ProviderId == providerId && allowedVariantIds.Contains(entry.VariantId));

        foreach (var variantId in allowedVariantIds.OrderBy(id => id))
        {
            blueprintSelection.VariantOverrides.Add(new BlueprintVariantOverrideEntry
            {
                ProviderId = providerId,
                VariantId = variantId,
                Mode = isEnabled ? BlueprintVariantSelectionModes.Enabled : BlueprintVariantSelectionModes.Disabled,
                UpdatedAtUtc = DateTime.UtcNow
            });
        }

        blueprintSelection.UpdatedAtUtc = DateTime.UtcNow;
        countrySelection.UpdatedAtUtc = DateTime.UtcNow;

        BlueprintCountrySettingsStore.Save(dataRoot, settings);
    }

    public async Task<BlueprintPricingRefreshResult> RefreshLivePricingAsync(
        string countryCode,
        int? blueprintId,
        CancellationToken cancellationToken)
    {
        var dataRoot = ResolveDataRoot();
        var settings = BlueprintCountrySettingsStore.Load(dataRoot);
        var normalizedCountryCode = NormalizeCountryCode(countryCode, settings);
        var blueprintApi = CreateQueryApi(dataRoot);
        var sharedLivePricingByBlueprintId = BuildSharedLivePricingLookup(settings);
        var countrySelection = GetOrCreateCountrySelection(
            settings,
            normalizedCountryCode,
            blueprintApi,
            sharedLivePricingByBlueprintId);
        var targetBlueprintIds = ResolveTargetBlueprintIds(countrySelection, blueprintId);

        if (targetBlueprintIds.Count == 0)
        {
            throw new InvalidOperationException(
                "Enable at least one blueprint or variant before refreshing pricing lookups.");
        }

        var repositoryRoot = ResolveRepositoryRoot();
        var envFilePath = Path.Combine(repositoryRoot, "main.env");
        var token = ReadRequiredEnvValue(envFilePath, "TOKEN");
        var printify = new PrintifyClient(token);
        var shops = await printify.GetShopsAsync();
        var shop = ResolveProbeShop(envFilePath, shops)
            ?? throw new InvalidOperationException("No Printify shop is available for published pricing-product lookups.");

        var cachePath = PrintifyPricingProductCacheStore.GetCachePath(dataRoot, shop.Id);
        var pricingProductCache = PrintifyPricingProductCacheStore.Load(cachePath);
        if (pricingProductCache.Entries.Count == 0)
        {
            throw new InvalidOperationException(
                $"No published pricing products were found for {shop.Title} (#{shop.Id}). Build them first with dotnet run --project src/PrintifyGenerator.CacheGenerator -- pricing-products --shop-id {shop.Id}.");
        }

        var warnings = new List<string>();
        var refreshedBlueprintCount = 0;
        var refreshedProviderCount = 0;
        var refreshedVariantCount = 0;
        var requestCount = 0;
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();

        async Task ThrottleAsync()
        {
            requestCount++;

            if (requestCount % PricingLookupRequestLimitPerMinute != 0)
                return;

            var elapsed = stopwatch.Elapsed;
            if (elapsed.TotalSeconds < 60)
            {
                var delay = TimeSpan.FromSeconds(62) - elapsed;
                if (delay > TimeSpan.Zero)
                    await Task.Delay(delay, cancellationToken);
            }

            stopwatch.Restart();
            requestCount = 0;
        }

        foreach (var currentBlueprintId in targetBlueprintIds)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var livePricingResult = await BuildLivePricingSnapshotAsync(
                blueprintApi,
                printify,
                shop,
                pricingProductCache,
                normalizedCountryCode,
                currentBlueprintId,
                warnings,
                cancellationToken,
                ThrottleAsync);

            if (livePricingResult is null)
            {
                continue;
            }

            var blueprintSelection = GetOrCreateBlueprintSelection(countrySelection, currentBlueprintId);
            var mergedProviders = MergeProviderSnapshots(blueprintSelection.LivePricing, livePricingResult.Value.LivePricing.Providers);

            blueprintSelection.LivePricing = new BlueprintLivePricingSnapshot
            {
                UpdatedAtUtc = livePricingResult.Value.LivePricing.UpdatedAtUtc,
                ShopId = livePricingResult.Value.LivePricing.ShopId,
                ShopTitle = livePricingResult.Value.LivePricing.ShopTitle,
                SampleImagePreviewUrl = livePricingResult.Value.LivePricing.SampleImagePreviewUrl,
                Providers = mergedProviders
            };
            blueprintSelection.UpdatedAtUtc = DateTime.UtcNow;

            refreshedBlueprintCount++;
            refreshedProviderCount += livePricingResult.Value.ProviderCount;
            refreshedVariantCount += livePricingResult.Value.VariantCount;
        }

        countrySelection.UpdatedAtUtc = DateTime.UtcNow;
        BlueprintCountrySettingsStore.Save(dataRoot, settings);

        if (refreshedBlueprintCount == 0)
        {
            var warningSuffix = warnings.Count == 0
                ? string.Empty
                : $" {warnings[0]}";

            throw new InvalidOperationException(
                $"No pricing lookups were refreshed for {normalizedCountryCode}.{warningSuffix}".Trim());
        }

        var message = $"Refreshed production and delivery pricing for {refreshedBlueprintCount} blueprint(s), {refreshedProviderCount} provider(s), and {refreshedVariantCount} variant(s) using published pricing products in {shop.Title} (#{shop.Id}).";
        if (warnings.Count > 0)
        {
            message += $" {warnings.Count} lookup(s) were skipped.";
        }

        return new BlueprintPricingRefreshResult(
            message,
            refreshedBlueprintCount,
            refreshedProviderCount,
            refreshedVariantCount,
            warnings);
    }

    private string ResolveDataRoot()
    {
        return DashboardOptions.ResolveDataRoot(_options.CurrentValue.DataRoot, _environment.ContentRootPath);
    }

    private static string NormalizeCountryCode(string? countryCode, BlueprintCountrySettingsDocument settings)
    {
        var normalizedCountryCode = BlueprintCountrySettingsStore.NormalizeCountryCode(countryCode);
        if (!string.IsNullOrWhiteSpace(normalizedCountryCode))
            return normalizedCountryCode;

        return settings.Countries.FirstOrDefault()?.CountryCode ?? DefaultCountryCode;
    }

    private static BlueprintCountrySelection? FindCountrySelection(BlueprintCountrySettingsDocument settings, string countryCode)
    {
        return settings.Countries.FirstOrDefault(country =>
            string.Equals(country.CountryCode, countryCode, StringComparison.OrdinalIgnoreCase));
    }

    private static BlueprintCountrySelection GetOrCreateCountrySelection(
        BlueprintCountrySettingsDocument settings,
        string countryCode,
        PrintifyBlueprintQueryApi blueprintApi,
        IReadOnlyDictionary<int, BlueprintLivePricingSnapshot> sharedLivePricingByBlueprintId)
    {
        var existing = FindCountrySelection(settings, countryCode);
        if (existing is not null)
            return existing;

        return CreateDefaultCountrySelection(settings, countryCode, blueprintApi, sharedLivePricingByBlueprintId);
    }

    private static BlueprintCountrySelection CreateDefaultCountrySelection(
        BlueprintCountrySettingsDocument settings,
        string countryCode,
        PrintifyBlueprintQueryApi blueprintApi,
        IReadOnlyDictionary<int, BlueprintLivePricingSnapshot> sharedLivePricingByBlueprintId)
    {
        var createdAtUtc = DateTime.UtcNow;
        var created = new BlueprintCountrySelection
        {
            CountryCode = countryCode,
            UpdatedAtUtc = createdAtUtc,
            Blueprints = BuildDefaultBlueprintSelections(
                blueprintApi,
                sharedLivePricingByBlueprintId,
                countryCode,
                createdAtUtc)
        };

        settings.Countries.Add(created);
        return created;
    }

    private static BlueprintCountrySelectionEntry GetOrCreateBlueprintSelection(BlueprintCountrySelection countrySelection, int blueprintId)
    {
        var existing = countrySelection.Blueprints.FirstOrDefault(entry => entry.BlueprintId == blueprintId);
        if (existing is not null)
            return existing;

        var created = new BlueprintCountrySelectionEntry
        {
            BlueprintId = blueprintId,
            UpdatedAtUtc = DateTime.UtcNow
        };

        countrySelection.Blueprints.Add(created);
        return created;
    }

    private static List<int> ResolveTargetBlueprintIds(BlueprintCountrySelection countrySelection, int? blueprintId)
    {
        if (blueprintId.HasValue)
            return new List<int> { blueprintId.Value };

        return countrySelection.Blueprints
            .Where(entry => entry.Enabled || entry.VariantOverrides.Any(variant => variant.Mode == BlueprintVariantSelectionModes.Enabled))
            .Select(entry => entry.BlueprintId)
            .Distinct()
            .OrderBy(id => id)
            .ToList();
    }

    private static List<BlueprintCountrySelectionEntry> BuildDefaultBlueprintSelections(
        PrintifyBlueprintQueryApi blueprintApi,
        IReadOnlyDictionary<int, BlueprintLivePricingSnapshot> sharedLivePricingByBlueprintId,
        string countryCode,
        DateTime updatedAtUtc)
    {
        var selections = new List<BlueprintCountrySelectionEntry>();

        foreach (var detail in blueprintApi.GetAllBlueprintDetails())
        {
            var defaultVariants = FindDefaultVariantSelections(detail, sharedLivePricingByBlueprintId, countryCode);
            if (defaultVariants.Count == 0)
            {
                continue;
            }

            selections.Add(new BlueprintCountrySelectionEntry
            {
                BlueprintId = detail.Blueprint.Id,
                Enabled = false,
                UpdatedAtUtc = updatedAtUtc,
                VariantOverrides = defaultVariants
                    .Select(selection => new BlueprintVariantOverrideEntry
                    {
                        ProviderId = selection.ProviderId,
                        VariantId = selection.VariantId,
                        Mode = BlueprintVariantSelectionModes.Enabled,
                        UpdatedAtUtc = updatedAtUtc
                    })
                    .ToList()
            });
        }

        return selections;
    }

    private static List<DefaultBlueprintVariantSelection> FindDefaultVariantSelections(
        PrintifyCachedBlueprintDetail detail,
        IReadOnlyDictionary<int, BlueprintLivePricingSnapshot> sharedLivePricingByBlueprintId,
        string countryCode)
    {
        sharedLivePricingByBlueprintId.TryGetValue(detail.Blueprint.Id, out var sharedLivePricing);
        var candidates = new List<DefaultBlueprintVariantSelectionCandidate>();

        foreach (var providerDetail in detail.PrintProviders)
        {
            var shippingLookup = BuildCountryShippingLookup(providerDetail.Shipping, countryCode);
            if (shippingLookup.Count == 0)
            {
                continue;
            }

            var sharedProviderPricing = sharedLivePricing?.Providers.FirstOrDefault(provider =>
                provider.ProviderId == providerDetail.Provider.Id);
            var sharedVariantsById = (sharedProviderPricing?.Variants ?? new List<BlueprintVariantLivePricingSnapshot>())
                .ToDictionary(variant => variant.VariantId);

            foreach (var variant in providerDetail.Variants.Variants)
            {
                if (!shippingLookup.TryGetValue(variant.Id, out var shipping) ||
                    !shipping.FirstItemCost.HasValue ||
                    shipping.FirstItemCost.Value > MaxDefaultEnabledShippingCostMinorUnits)
                {
                    continue;
                }

                sharedVariantsById.TryGetValue(variant.Id, out var sharedVariantPricing);
                if (sharedVariantPricing is not null && !sharedVariantPricing.IsAvailable)
                {
                    continue;
                }

                var productionCost = sharedVariantPricing?.ProductionCost ?? variant.Cost;
                if (!productionCost.HasValue)
                {
                    continue;
                }

                var normalizedOptions = NormalizeVariantOptions(variant.Options);
                var colorKey = GetColorOptionValue(normalizedOptions);
                var typeOptions = GetVariantTypeOptions(normalizedOptions);

                candidates.Add(new DefaultBlueprintVariantSelectionCandidate(
                    ProviderId: providerDetail.Provider.Id,
                    VariantId: variant.Id,
                    ColorKey: colorKey,
                    VariantTypeKey: BuildVariantTypeKey(typeOptions),
                    VariantTypeOptions: typeOptions,
                    TotalCost: productionCost.Value + shipping.FirstItemCost.Value,
                    ProductionCost: productionCost.Value,
                    ShippingCost: shipping.FirstItemCost.Value));
            }
        }

        if (candidates.Count == 0)
        {
            return new List<DefaultBlueprintVariantSelection>();
        }

        var cheapestPerColorAndType = candidates
            .GroupBy(candidate => (candidate.ColorKey, candidate.VariantTypeKey))
            .Select(group => group
                .OrderBy(candidate => candidate.TotalCost)
                .ThenBy(candidate => candidate.ProductionCost)
                .ThenBy(candidate => candidate.ShippingCost)
                .ThenBy(candidate => candidate.ProviderId)
                .ThenBy(candidate => candidate.VariantId)
                .First())
            .ToList();

        var colorSelections = cheapestPerColorAndType
            .GroupBy(candidate => candidate.ColorKey, StringComparer.OrdinalIgnoreCase)
            .Select(group => new DefaultBlueprintColorSelection(
                ColorKey: group.Key,
                AverageTotalCost: group.Average(candidate => candidate.TotalCost),
                VariantCount: group.Count(),
                Variants: group.ToList()))
            .ToList();

        if (colorSelections.Count == 0)
        {
            return new List<DefaultBlueprintVariantSelection>();
        }

        var chosenColor = colorSelections
            .OrderBy(selection => selection.AverageTotalCost)
            .ThenByDescending(selection => selection.VariantCount)
            .ThenBy(selection => selection.ColorKey, StringComparer.OrdinalIgnoreCase)
            .First();

        return chosenColor.Variants
            .OrderBy(candidate => candidate, DefaultBlueprintVariantTypeComparer.Instance)
            .ThenBy(candidate => candidate.TotalCost)
            .ThenBy(candidate => candidate.ProviderId)
            .ThenBy(candidate => candidate.VariantId)
            .Take(MaxDefaultEnabledVariantsPerBlueprint)
            .Select(candidate => new DefaultBlueprintVariantSelection(
                candidate.ProviderId,
                candidate.VariantId,
                candidate.TotalCost,
                candidate.ProductionCost,
                candidate.ShippingCost))
            .ToList();
    }

    private static IReadOnlyList<DefaultBlueprintVariantOption> NormalizeVariantOptions(Dictionary<string, object>? options)
    {
        if (options is null || options.Count == 0)
        {
            return Array.Empty<DefaultBlueprintVariantOption>();
        }

        return options
            .Select(option => new DefaultBlueprintVariantOption(
                option.Key.Trim(),
                FormatOptionValue(option.Value).Trim()))
            .OrderBy(option => option.Key, StringComparer.OrdinalIgnoreCase)
            .ThenBy(option => option.Value, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private static string GetColorOptionValue(IReadOnlyList<DefaultBlueprintVariantOption> options)
    {
        return options
            .FirstOrDefault(option => IsColorOptionKey(option.Key))
            .Value ?? string.Empty;
    }

    private static IReadOnlyList<DefaultBlueprintVariantOption> GetVariantTypeOptions(
        IReadOnlyList<DefaultBlueprintVariantOption> options)
    {
        return options
            .Where(option => !IsColorOptionKey(option.Key))
            .ToList();
    }

    private static string BuildVariantTypeKey(IReadOnlyList<DefaultBlueprintVariantOption> options)
    {
        if (options.Count == 0)
        {
            return "default";
        }

        return string.Join(
            "|",
            options.Select(option => $"{option.Key.ToLowerInvariant()}={option.Value.ToLowerInvariant()}"));
    }

    private static bool IsColorOptionKey(string key)
    {
        return key.Contains("color", StringComparison.OrdinalIgnoreCase) ||
               key.Contains("colour", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsSizeOptionKey(string key)
    {
        return key.Contains("size", StringComparison.OrdinalIgnoreCase);
    }

    private static int CompareVariantOptionValues(string key, string leftValue, string rightValue)
    {
        if (IsSizeOptionKey(key) &&
            TryGetSizeSortRank(leftValue, out var leftRank) &&
            TryGetSizeSortRank(rightValue, out var rightRank) &&
            leftRank != rightRank)
        {
            return leftRank.CompareTo(rightRank);
        }

        return string.Compare(leftValue, rightValue, StringComparison.OrdinalIgnoreCase);
    }

    private static bool TryGetSizeSortRank(string value, out int rank)
    {
        var normalized = value.Trim().ToUpperInvariant();
        rank = normalized switch
        {
            "XXXS" => 0,
            "3XS" => 0,
            "XXS" => 1,
            "2XS" => 1,
            "XS" => 2,
            "S" => 3,
            "M" => 4,
            "L" => 5,
            "XL" => 6,
            "XXL" => 7,
            "2XL" => 7,
            "XXXL" => 8,
            "3XL" => 8,
            "4XL" => 9,
            "5XL" => 10,
            "6XL" => 11,
            _ => int.MaxValue
        };

        return rank != int.MaxValue;
    }

    private BlueprintCatalogBlueprintNode? BuildBlueprintNode(
        PrintifyBlueprintQueryApi blueprintApi,
        PrintifyCachedBlueprintDetail detail,
        BlueprintCountrySelection? countrySelection,
        IReadOnlyDictionary<int, BlueprintLivePricingSnapshot> sharedLivePricingByBlueprintId,
        string countryCode)
    {
        var blueprintSelection = countrySelection?.Blueprints.FirstOrDefault(entry => entry.BlueprintId == detail.Blueprint.Id);
        sharedLivePricingByBlueprintId.TryGetValue(detail.Blueprint.Id, out var sharedLivePricing);
        var providerNodes = new List<BlueprintCatalogProviderNode>();

        foreach (var providerDetail in detail.PrintProviders.OrderBy(provider => provider.Provider.Title, StringComparer.OrdinalIgnoreCase))
        {
            var shippingLookup = BuildCountryShippingLookup(providerDetail.Shipping, countryCode);
            if (shippingLookup.Count == 0)
                continue;

            var liveProviderPricing = blueprintSelection?.LivePricing?.Providers.FirstOrDefault(provider =>
                provider.ProviderId == providerDetail.Provider.Id);
            var sharedProviderPricing = sharedLivePricing?.Providers.FirstOrDefault(provider =>
                provider.ProviderId == providerDetail.Provider.Id);

            var variants = providerDetail.Variants.Variants
                .Where(variant => shippingLookup.ContainsKey(variant.Id))
                .OrderBy(variant => variant.Title, StringComparer.OrdinalIgnoreCase)
                .ThenBy(variant => variant.Id)
                .Select(variant => BuildVariantNode(
                    variant,
                    providerDetail.Provider.Id,
                    blueprintSelection,
                    liveProviderPricing,
                    sharedProviderPricing,
                    shippingLookup[variant.Id],
                    blueprintApi.GetVariantImageUrl(detail.Blueprint.Id, providerDetail.Provider.Id, variant.Id)))
                .ToList();

            if (variants.Count == 0)
                continue;

            providerNodes.Add(new BlueprintCatalogProviderNode(
                ProviderId: providerDetail.Provider.Id,
                Title: providerDetail.Provider.Title,
                HandlingTimeText: FormatHandlingTime(providerDetail.Shipping.HandlingTime),
                ShippingCurrency: variants.Select(variant => variant.ShippingCurrency).FirstOrDefault(currency => !string.IsNullOrWhiteSpace(currency)) ?? string.Empty,
                LowestFirstItemShippingCost: variants.Where(variant => variant.ShippingFirstItemCost.HasValue).Select(variant => variant.ShippingFirstItemCost!.Value).DefaultIfEmpty().Min(),
                VariantCount: variants.Count,
                EnabledVariantCount: variants.Count(variant => variant.IsEffectivelyEnabled),
                Variants: variants));
        }

        if (providerNodes.Count == 0)
            return null;

        var cachedExampleImageUrl = providerNodes
            .Select(provider => blueprintApi.GetProviderExampleImageUrl(detail.Blueprint.Id, provider.ProviderId))
            .FirstOrDefault(url => !string.IsNullOrWhiteSpace(url));
        var imageUrl = !string.IsNullOrWhiteSpace(cachedExampleImageUrl)
            ? cachedExampleImageUrl
            : detail.Blueprint.Images.FirstOrDefault();
        var livePricing = blueprintSelection?.LivePricing;
        var displayLivePricing = sharedLivePricing ?? livePricing;

        return new BlueprintCatalogBlueprintNode(
            BlueprintId: detail.Blueprint.Id,
            Title: detail.Blueprint.Title,
            Brand: detail.Blueprint.Brand,
            Model: detail.Blueprint.Model,
            ImageUrl: string.IsNullOrWhiteSpace(imageUrl) ? displayLivePricing?.SampleImagePreviewUrl : imageUrl,
            IsEnabled: blueprintSelection?.Enabled ?? false,
            ProviderCount: providerNodes.Count,
            VariantCount: providerNodes.Sum(provider => provider.VariantCount),
            EnabledVariantCount: providerNodes.Sum(provider => provider.EnabledVariantCount),
            LivePricingUpdatedAtUtc: displayLivePricing?.UpdatedAtUtc,
            ProbeShopLabel: FormatProbeShopLabel(displayLivePricing),
            Providers: providerNodes);
    }

    private static BlueprintCatalogVariantNode BuildVariantNode(
        Variant variant,
        int providerId,
        BlueprintCountrySelectionEntry? blueprintSelection,
        BlueprintProviderLivePricingSnapshot? liveProviderPricing,
        BlueprintProviderLivePricingSnapshot? sharedProviderPricing,
        ShippingQuoteSummary shipping,
        string? cachedImageUrl)
    {
        var variantOverride = blueprintSelection?.VariantOverrides.FirstOrDefault(entry =>
            entry.ProviderId == providerId && entry.VariantId == variant.Id);
        var liveVariantPricing = liveProviderPricing?.Variants.FirstOrDefault(entry => entry.VariantId == variant.Id);
        var sharedVariantPricing = sharedProviderPricing?.Variants.FirstOrDefault(entry => entry.VariantId == variant.Id);
        var productionVariantPricing = sharedVariantPricing ?? liveVariantPricing;
        var isEffectivelyEnabled = BlueprintVariantSelectionModes.ResolveEffectiveState(blueprintSelection?.Enabled ?? false, variantOverride);

        return new BlueprintCatalogVariantNode(
            VariantId: variant.Id,
            Title: variant.Title,
            ImageUrl: !string.IsNullOrWhiteSpace(liveVariantPricing?.ImageUrl)
                ? liveVariantPricing.ImageUrl
                : !string.IsNullOrWhiteSpace(productionVariantPricing?.ImageUrl)
                    ? productionVariantPricing.ImageUrl
                : cachedImageUrl,
            OptionSummary: FormatVariantOptions(variant.Options),
            IsAvailable: liveVariantPricing?.IsAvailable ?? productionVariantPricing?.IsAvailable ?? true,
            SelectionMode: BlueprintVariantSelectionModes.Normalize(variantOverride?.Mode),
            IsEffectivelyEnabled: isEffectivelyEnabled,
            ProductionCost: productionVariantPricing?.ProductionCost ?? variant.Cost,
            ProductionCurrency: !string.IsNullOrWhiteSpace(productionVariantPricing?.Currency)
                ? productionVariantPricing!.Currency
                : shipping.Currency,
            ProductionCostUpdatedAtUtc: productionVariantPricing?.UpdatedAtUtc,
            ShippingFirstItemCost: shipping.FirstItemCost,
            ShippingAdditionalItemCost: shipping.AdditionalItemCost,
                ShippingCurrency: shipping.Currency,
                LiveShippingCost: liveVariantPricing?.DeliveryCost,
                LiveShippingCurrency: liveVariantPricing?.DeliveryCurrency ?? string.Empty,
                LiveShippingMethod: liveVariantPricing?.DeliveryMethod,
                PricingProductId: productionVariantPricing?.PricingProductId,
                PricingProductTitle: productionVariantPricing?.PricingProductTitle,
                PricingProductPageNumber: productionVariantPricing?.PricingProductPageNumber);
    }

    private static bool MatchesSearch(BlueprintCatalogBlueprintNode node, string searchTerm)
    {
        if (string.IsNullOrWhiteSpace(searchTerm))
            return true;

        if (ContainsSearch(node.Title, searchTerm) ||
            ContainsSearch(node.Brand, searchTerm) ||
            ContainsSearch(node.Model, searchTerm) ||
            node.BlueprintId.ToString(CultureInfo.InvariantCulture).Contains(searchTerm, StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        foreach (var provider in node.Providers)
        {
            if (ContainsSearch(provider.Title, searchTerm) ||
                provider.ProviderId.ToString(CultureInfo.InvariantCulture).Contains(searchTerm, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            foreach (var variant in provider.Variants)
            {
                if (ContainsSearch(variant.Title, searchTerm) ||
                    ContainsSearch(variant.OptionSummary, searchTerm) ||
                    variant.VariantId.ToString(CultureInfo.InvariantCulture).Contains(searchTerm, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }
        }

        return false;
    }

    private static bool ContainsSearch(string? source, string searchTerm)
    {
        return !string.IsNullOrWhiteSpace(source) &&
               source.Contains(searchTerm, StringComparison.OrdinalIgnoreCase);
    }

    private static Dictionary<int, ShippingQuoteSummary> BuildCountryShippingLookup(ShippingInfo shipping, string countryCode)
    {
        var exactLookup = new Dictionary<int, ShippingQuoteSummary>();
        var restOfWorldLookup = new Dictionary<int, ShippingQuoteSummary>();

        foreach (var profile in shipping.Profiles)
        {
            if (ProfileContainsCountry(profile, countryCode))
            {
                AddShippingProfile(exactLookup, profile);
                continue;
            }

            if (ProfileContainsRestOfWorld(profile))
            {
                AddShippingProfile(restOfWorldLookup, profile);
            }
        }

        foreach (var entry in restOfWorldLookup)
        {
            if (!exactLookup.ContainsKey(entry.Key))
            {
                exactLookup[entry.Key] = entry.Value;
            }
        }

        return exactLookup;
    }

    private static void AddShippingProfile(Dictionary<int, ShippingQuoteSummary> lookup, ShippingProfile profile)
    {
        var candidate = new ShippingQuoteSummary(
            profile.FirstItem?.Cost,
            profile.AdditionalItems?.Cost,
            profile.FirstItem?.Currency ?? profile.AdditionalItems?.Currency ?? string.Empty);

        foreach (var variantId in profile.VariantIds)
        {
            if (!lookup.TryGetValue(variantId, out var existing) || CompareShipping(candidate, existing) < 0)
            {
                lookup[variantId] = candidate;
            }
        }
    }

    private static bool ProfileContainsCountry(ShippingProfile profile, string countryCode)
    {
        return profile.Countries.Contains(countryCode, StringComparer.OrdinalIgnoreCase);
    }

    private static bool ProfileContainsRestOfWorld(ShippingProfile profile)
    {
        return profile.Countries.Any(IsRestOfWorldCountryCode);
    }

    private static bool IsRestOfWorldCountryCode(string? countryCode)
    {
        return string.Equals(NormalizeShippingCountryCode(countryCode), RestOfWorldCountryCode, StringComparison.Ordinal);
    }

    private static string NormalizeShippingCountryCode(string? countryCode)
    {
        if (string.IsNullOrWhiteSpace(countryCode))
            return string.Empty;

        return string.Join('_', countryCode
            .Trim()
            .Split(new[] { '-', '_', ' ' }, StringSplitOptions.RemoveEmptyEntries))
            .ToUpperInvariant();
    }

    private static int CompareShipping(ShippingQuoteSummary left, ShippingQuoteSummary right)
    {
        var leftCost = left.FirstItemCost ?? int.MaxValue;
        var rightCost = right.FirstItemCost ?? int.MaxValue;
        var costComparison = leftCost.CompareTo(rightCost);
        if (costComparison != 0)
            return costComparison;

        var leftAdditional = left.AdditionalItemCost ?? int.MaxValue;
        var rightAdditional = right.AdditionalItemCost ?? int.MaxValue;
        return leftAdditional.CompareTo(rightAdditional);
    }

    private static string FormatHandlingTime(HandlingTime? handlingTime)
    {
        if (handlingTime is null || handlingTime.Value <= 0 || string.IsNullOrWhiteSpace(handlingTime.Unit))
            return string.Empty;

        return $"{handlingTime.Value} {handlingTime.Unit}";
    }

    private static string FormatProbeShopLabel(BlueprintLivePricingSnapshot? livePricing)
    {
        if (livePricing is null)
            return string.Empty;

        if (livePricing.ShopId.HasValue && !string.IsNullOrWhiteSpace(livePricing.ShopTitle))
            return $"{livePricing.ShopTitle} (#{livePricing.ShopId.Value})";

        if (livePricing.ShopId.HasValue)
            return $"Shop #{livePricing.ShopId.Value}";

        return livePricing.ShopTitle;
    }

    private static string FormatVariantOptions(Dictionary<string, object>? options)
    {
        if (options is null || options.Count == 0)
            return "(none)";

        return string.Join(", ", options
            .Select(option => $"{option.Key}={FormatOptionValue(option.Value)}")
            .OrderBy(text => text, StringComparer.OrdinalIgnoreCase));
    }

    private static string FormatOptionValue(object? value)
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

    private static PrintifyBlueprintQueryApi CreateQueryApi(string dataRoot)
    {
        return PrintifyBlueprintDatabase.CreateQueryApi(Path.Combine(dataRoot, "Cached", "blueprint_details"));
    }

    private string ResolveRepositoryRoot()
    {
        var probeRoots = new[]
        {
            _environment.ContentRootPath,
            Directory.GetCurrentDirectory(),
            AppContext.BaseDirectory
        };

        foreach (var probeRoot in probeRoots.Where(path => !string.IsNullOrWhiteSpace(path)).Distinct(StringComparer.OrdinalIgnoreCase))
        {
            var current = new DirectoryInfo(Path.GetFullPath(probeRoot));
            while (current is not null)
            {
                var envPath = Path.Combine(current.FullName, "main.env");
                if (File.Exists(envPath))
                    return current.FullName;

                current = current.Parent;
            }
        }

        throw new FileNotFoundException("Could not locate main.env from the dashboard project root.");
    }

    private static string ReadRequiredEnvValue(string envFilePath, string key)
    {
        var value = ReadOptionalEnvValue(envFilePath, key) ?? Environment.GetEnvironmentVariable(key);
        if (!string.IsNullOrWhiteSpace(value))
            return value.Trim();

        throw new InvalidOperationException($"{key} is required in main.env for published pricing-product lookups.");
    }

    private static string? ReadOptionalEnvValue(string envFilePath, string key)
    {
        if (!File.Exists(envFilePath))
            return null;

        foreach (var line in File.ReadLines(envFilePath))
        {
            if (!line.StartsWith($"{key}=", StringComparison.OrdinalIgnoreCase))
                continue;

            return line[(key.Length + 1)..].Trim();
        }

        return null;
    }

    private static int? ReadOptionalIntEnvValue(string envFilePath, string key)
    {
        var value = ReadOptionalEnvValue(envFilePath, key) ?? Environment.GetEnvironmentVariable(key);
        return int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsedValue)
            ? parsedValue
            : null;
    }

    private static Shop? ResolveProbeShop(string envFilePath, IReadOnlyList<Shop> shops)
    {
        var configuredShopId = ReadOptionalIntEnvValue(envFilePath, "PRICE_UPDATER_SHOP_ID")
            ?? ReadOptionalIntEnvValue(envFilePath, "SHOP_ID")
            ?? ReadOptionalIntEnvValue(envFilePath, "SHOPID");

        if (configuredShopId.HasValue)
        {
            return shops.FirstOrDefault(shop => shop.Id == configuredShopId.Value);
        }

        return shops.FirstOrDefault(shop => !string.Equals(shop.SalesChannel, "custom_integration", StringComparison.OrdinalIgnoreCase))
            ?? shops.FirstOrDefault();
    }

    private static async Task<UploadedImage> UploadProbeImageAsync(PrintifyClient client)
    {
        return await client.UploadImageByUrlAsync(
            $"blueprint-country-probe-{DateTime.UtcNow:yyyyMMdd-HHmmss}.png",
            ProbeImageUrl);
    }

    private static async Task ArchiveProbeImageSafeAsync(PrintifyClient client, string imageId)
    {
        try
        {
            await client.ArchiveUploadAsync(imageId);
        }
        catch
        {
            // Keep dashboard actions resilient if cleanup fails.
        }
    }

    private static async Task DeleteProbeProductSafeAsync(PrintifyClient client, int shopId, string productId)
    {
        try
        {
            await client.DeleteProductAsync(shopId, productId);
        }
        catch
        {
            // Keep dashboard actions resilient if cleanup fails.
        }
    }

    private static async Task<BlueprintLivePricingBuildResult?> BuildLivePricingSnapshotAsync(
        PrintifyBlueprintQueryApi blueprintApi,
        PrintifyClient client,
        Shop shop,
        PrintifyPricingProductCacheDocument pricingProductCache,
        string countryCode,
        int blueprintId,
        ICollection<string> warnings,
        CancellationToken cancellationToken,
        Func<Task> throttleAsync)
    {
        var detail = blueprintApi.GetBlueprintDetail(blueprintId);
        var providers = new List<BlueprintProviderLivePricingSnapshot>();
        var providerCount = 0;
        var variantCount = 0;

        foreach (var providerDetail in detail.PrintProviders)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var allowedVariantIds = BuildCountryShippingLookup(providerDetail.Shipping, countryCode)
                .Keys
                .ToHashSet();

            if (allowedVariantIds.Count == 0)
                continue;

            try
            {
                var providerEntries = pricingProductCache.Entries
                    .Where(entry =>
                        entry.BlueprintId == blueprintId &&
                        entry.ProviderId == providerDetail.Provider.Id &&
                        !string.IsNullOrWhiteSpace(entry.ProductId))
                    .OrderBy(entry => entry.PageNumber)
                    .ToList();

                if (providerEntries.Count == 0)
                {
                    warnings.Add($"Provider {providerDetail.Provider.Title} on blueprint {blueprintId} has no published pricing product pages in shop {shop.Id}.");
                    continue;
                }

                var currency = DetermineProviderCurrency(providerDetail.Shipping, countryCode);
                var variants = await LoadPublishedProductVariantDetailsAsync(
                    client,
                    shop.Id,
                    countryCode,
                    blueprintId,
                    providerDetail.Provider.Id,
                    providerDetail.Variants.Variants,
                    allowedVariantIds,
                    providerEntries,
                    currency,
                    variantId => blueprintApi.GetVariantImageUrl(blueprintId, providerDetail.Provider.Id, variantId),
                    warnings,
                    throttleAsync,
                    cancellationToken);

                if (variants.Count == 0)
                    continue;

                providers.Add(new BlueprintProviderLivePricingSnapshot
                {
                    ProviderId = providerDetail.Provider.Id,
                    ProviderTitle = providerDetail.Provider.Title,
                    Currency = currency,
                    Variants = variants
                });

                providerCount++;
                variantCount += variants.Count;
            }
            catch (Exception ex)
            {
                warnings.Add($"Provider {providerDetail.Provider.Title} on blueprint {blueprintId} could not be probed: {ex.Message}");
            }
        }

        if (providers.Count == 0)
            return null;

        var livePricing = new BlueprintLivePricingSnapshot
        {
            UpdatedAtUtc = DateTime.UtcNow,
            ShopId = shop.Id,
            ShopTitle = shop.Title,
            SampleImagePreviewUrl = pricingProductCache.ProbeUploadPreviewUrl,
            Providers = providers
        };

        return new BlueprintLivePricingBuildResult(livePricing, providerCount, variantCount);
    }

    private static async Task<List<BlueprintVariantLivePricingSnapshot>> LoadPublishedProductVariantDetailsAsync(
        PrintifyClient client,
        int shopId,
        string countryCode,
        int blueprintId,
        int providerId,
        IReadOnlyList<Variant> cachedVariants,
        HashSet<int> allowedVariantIds,
        IReadOnlyList<PrintifyPricingProductCacheEntry> pricingProductEntries,
        string currency,
        Func<int, string?>? resolveCachedImageUrl,
        ICollection<string> warnings,
        Func<Task> throttleAsync,
        CancellationToken cancellationToken)
    {
        var cachedVariantsById = cachedVariants.ToDictionary(variant => variant.Id);
        var snapshotsByVariantId = new Dictionary<int, BlueprintVariantLivePricingSnapshot>();

        foreach (var pricingProductEntry in pricingProductEntries)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var relevantVariantIds = pricingProductEntry.VariantIds
                .Where(allowedVariantIds.Contains)
                .Distinct()
                .OrderBy(variantId => variantId)
                .ToList();

            if (relevantVariantIds.Count == 0)
                continue;

            Product product;
            try
            {
                await throttleAsync();
                product = await client.GetProductAsync(shopId, pricingProductEntry.ProductId);
            }
            catch (Exception ex)
            {
                warnings.Add($"Pricing product {pricingProductEntry.Title} for blueprint {blueprintId}, provider {providerId} could not be loaded: {ex.Message}");
                continue;
            }

            var optionLookup = BuildProductOptionLookup(product);
            var imageLookup = BuildProductVariantImageLookup(product);
            var productVariantsById = product.Variants.ToDictionary(variant => variant.Id);

            foreach (var variantId in relevantVariantIds)
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (snapshotsByVariantId.ContainsKey(variantId))
                    continue;

                if (!productVariantsById.TryGetValue(variantId, out var productVariant))
                {
                    warnings.Add($"Pricing product {pricingProductEntry.Title} is missing variant {variantId}.");
                    continue;
                }

                var shippingSummary = default(ShippingOptionSummary);

                try
                {
                    await throttleAsync();
                    var shipping = await client.CalculateShippingAsync(shopId, new ShippingCostRequest
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
                        AddressTo = new Address
                        {
                            Country = countryCode
                        }
                    });

                    shippingSummary = ResolveLowestShippingOption(shipping);
                }
                catch (Exception ex)
                {
                    warnings.Add($"Delivery lookup failed for variant {variantId} on pricing product {pricingProductEntry.Title}: {ex.Message}");
                }

                cachedVariantsById.TryGetValue(variantId, out var cachedVariant);
                var optionsText = FormatProductVariantOptions(productVariant.Options, optionLookup);
                var fallbackTitle = cachedVariant?.Title ?? string.Empty;
                var variantTitle = string.IsNullOrWhiteSpace(productVariant.Title)
                    ? fallbackTitle
                    : productVariant.Title;

                if (!string.IsNullOrWhiteSpace(optionsText) && optionsText != "(none)" &&
                    !variantTitle.Contains(optionsText, StringComparison.OrdinalIgnoreCase))
                {
                    variantTitle = string.IsNullOrWhiteSpace(variantTitle)
                        ? optionsText
                        : $"{variantTitle} ({optionsText})";
                }

                var resolvedImageUrl = imageLookup.TryGetValue(variantId, out var imageUrl)
                    ? imageUrl
                    : resolveCachedImageUrl?.Invoke(variantId);

                snapshotsByVariantId[variantId] = new BlueprintVariantLivePricingSnapshot
                {
                    VariantId = variantId,
                    VariantTitle = variantTitle,
                    ProductionCost = productVariant.Cost,
                    Currency = currency,
                    ImageUrl = string.IsNullOrWhiteSpace(resolvedImageUrl) ? null : resolvedImageUrl,
                    IsAvailable = productVariant.IsAvailable,
                    DeliveryCost = shippingSummary.Cost,
                    DeliveryCurrency = shippingSummary.Cost.HasValue ? currency : string.Empty,
                    DeliveryMethod = shippingSummary.Method,
                    PricingProductId = product.Id,
                    PricingProductTitle = string.IsNullOrWhiteSpace(product.Title) ? pricingProductEntry.Title : product.Title,
                    PricingProductPageNumber = pricingProductEntry.PageNumber > 0 ? pricingProductEntry.PageNumber : null,
                    UpdatedAtUtc = DateTime.UtcNow
                };
            }
        }

        return snapshotsByVariantId.Values
            .OrderBy(snapshot => snapshot.VariantTitle, StringComparer.OrdinalIgnoreCase)
            .ThenBy(snapshot => snapshot.VariantId)
            .ToList();
    }

    private static List<BlueprintProviderLivePricingSnapshot> MergeProviderSnapshots(
        BlueprintLivePricingSnapshot? existing,
        IReadOnlyList<BlueprintProviderLivePricingSnapshot> refreshedProviders)
    {
        var merged = (existing?.Providers ?? new List<BlueprintProviderLivePricingSnapshot>())
            .ToDictionary(provider => provider.ProviderId, CloneProviderSnapshot);

        foreach (var provider in refreshedProviders)
        {
            if (merged.TryGetValue(provider.ProviderId, out var existingProvider))
            {
                merged[provider.ProviderId] = MergeProviderSnapshot(existingProvider, provider);
                continue;
            }

            merged[provider.ProviderId] = CloneProviderSnapshot(provider);
        }

        return merged.Values
            .OrderBy(provider => provider.ProviderTitle, StringComparer.OrdinalIgnoreCase)
            .ThenBy(provider => provider.ProviderId)
            .ToList();
    }

    private static BlueprintProviderLivePricingSnapshot CloneProviderSnapshot(BlueprintProviderLivePricingSnapshot provider)
    {
        return new BlueprintProviderLivePricingSnapshot
        {
            ProviderId = provider.ProviderId,
            ProviderTitle = provider.ProviderTitle,
            Currency = provider.Currency,
            Variants = provider.Variants
                .Select(CloneVariantSnapshot)
                .ToList()
        };
    }

    private static BlueprintProviderLivePricingSnapshot MergeProviderSnapshot(
        BlueprintProviderLivePricingSnapshot existing,
        BlueprintProviderLivePricingSnapshot refreshed)
    {
        var mergedVariants = existing.Variants
            .ToDictionary(variant => variant.VariantId, CloneVariantSnapshot);

        foreach (var variant in refreshed.Variants)
        {
            if (mergedVariants.TryGetValue(variant.VariantId, out var existingVariant))
            {
                mergedVariants[variant.VariantId] = MergeVariantSnapshot(existingVariant, variant);
                continue;
            }

            mergedVariants[variant.VariantId] = CloneVariantSnapshot(variant);
        }

        return new BlueprintProviderLivePricingSnapshot
        {
            ProviderId = refreshed.ProviderId,
            ProviderTitle = string.IsNullOrWhiteSpace(refreshed.ProviderTitle)
                ? existing.ProviderTitle
                : refreshed.ProviderTitle,
            Currency = string.IsNullOrWhiteSpace(refreshed.Currency)
                ? existing.Currency
                : refreshed.Currency,
            Variants = mergedVariants.Values
                .OrderBy(variant => variant.VariantTitle, StringComparer.OrdinalIgnoreCase)
                .ThenBy(variant => variant.VariantId)
                .ToList()
        };
    }

    private static BlueprintVariantLivePricingSnapshot CloneVariantSnapshot(BlueprintVariantLivePricingSnapshot variant)
    {
        return new BlueprintVariantLivePricingSnapshot
        {
            VariantId = variant.VariantId,
            VariantTitle = variant.VariantTitle,
            ProductionCost = variant.ProductionCost,
            Currency = variant.Currency,
            ImageUrl = variant.ImageUrl,
            IsAvailable = variant.IsAvailable,
            DeliveryCost = variant.DeliveryCost,
            DeliveryCurrency = variant.DeliveryCurrency,
            DeliveryMethod = variant.DeliveryMethod,
            PricingProductId = variant.PricingProductId,
            PricingProductTitle = variant.PricingProductTitle,
            PricingProductPageNumber = variant.PricingProductPageNumber,
            UpdatedAtUtc = variant.UpdatedAtUtc
        };
    }

    private static BlueprintVariantLivePricingSnapshot MergeVariantSnapshot(
        BlueprintVariantLivePricingSnapshot existing,
        BlueprintVariantLivePricingSnapshot refreshed)
    {
        return new BlueprintVariantLivePricingSnapshot
        {
            VariantId = refreshed.VariantId,
            VariantTitle = string.IsNullOrWhiteSpace(refreshed.VariantTitle)
                ? existing.VariantTitle
                : refreshed.VariantTitle,
            ProductionCost = refreshed.ProductionCost ?? existing.ProductionCost,
            Currency = string.IsNullOrWhiteSpace(refreshed.Currency)
                ? existing.Currency
                : refreshed.Currency,
            ImageUrl = string.IsNullOrWhiteSpace(refreshed.ImageUrl)
                ? existing.ImageUrl
                : refreshed.ImageUrl,
            IsAvailable = refreshed.IsAvailable,
            DeliveryCost = refreshed.DeliveryCost ?? existing.DeliveryCost,
            DeliveryCurrency = string.IsNullOrWhiteSpace(refreshed.DeliveryCurrency)
                ? existing.DeliveryCurrency
                : refreshed.DeliveryCurrency,
            DeliveryMethod = string.IsNullOrWhiteSpace(refreshed.DeliveryMethod)
                ? existing.DeliveryMethod
                : refreshed.DeliveryMethod,
            PricingProductId = string.IsNullOrWhiteSpace(refreshed.PricingProductId)
                ? existing.PricingProductId
                : refreshed.PricingProductId,
            PricingProductTitle = string.IsNullOrWhiteSpace(refreshed.PricingProductTitle)
                ? existing.PricingProductTitle
                : refreshed.PricingProductTitle,
            PricingProductPageNumber = refreshed.PricingProductPageNumber ?? existing.PricingProductPageNumber,
            UpdatedAtUtc = refreshed.UpdatedAtUtc == default
                ? existing.UpdatedAtUtc
                : refreshed.UpdatedAtUtc
        };
    }

    private static async Task<List<BlueprintVariantLivePricingSnapshot>> LoadDraftProductVariantDetailsAsync(
        PrintifyClient client,
        int shopId,
        int blueprintId,
        int providerId,
        IReadOnlyList<PrintifyBlueprintSubvariant> subvariants,
        string uploadedImageId,
        string currency)
    {
        var printableSubvariants = subvariants
            .Where(subvariant => subvariant.Placeholders.Count > 0)
            .ToList();

        _ = printableSubvariants.FirstOrDefault()
            ?? throw new InvalidOperationException(
                $"Blueprint {blueprintId}, provider {providerId} has no placeholders available for temporary product creation.");

        var cachedSubvariantsById = subvariants.ToDictionary(subvariant => subvariant.VariantId);
        var snapshotsByVariantId = new Dictionary<int, BlueprintVariantLivePricingSnapshot>();

        foreach (var batch in printableSubvariants.Chunk(MaxProbeVariantBatchSize))
        {
            var batchList = batch.ToList();
            var seedSubvariant = batchList[0];
            var request = new CreateProductRequest
            {
                Title = $"Blueprint Country Probe - {seedSubvariant.BlueprintTitle} - {providerId}",
                Description = "Temporary product used to inspect Printify production cost per variant.",
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
                var optionLookup = BuildProductOptionLookup(product);
                var imageLookup = BuildProductVariantImageLookup(product);

                foreach (var variant in product.Variants.OrderBy(variant => variant.Title, StringComparer.OrdinalIgnoreCase).ThenBy(variant => variant.Id))
                {
                    cachedSubvariantsById.TryGetValue(variant.Id, out var cachedSubvariant);
                    var optionsText = FormatProductVariantOptions(variant.Options, optionLookup);
                    var fallbackTitle = cachedSubvariant?.VariantTitle ?? string.Empty;
                    var variantTitle = string.IsNullOrWhiteSpace(variant.Title)
                        ? fallbackTitle
                        : variant.Title;

                    if (!string.IsNullOrWhiteSpace(optionsText) && optionsText != "(none)" &&
                        !variantTitle.Contains(optionsText, StringComparison.OrdinalIgnoreCase))
                    {
                        variantTitle = string.IsNullOrWhiteSpace(variantTitle)
                            ? optionsText
                            : $"{variantTitle} ({optionsText})";
                    }

                    snapshotsByVariantId[variant.Id] = new BlueprintVariantLivePricingSnapshot
                    {
                        VariantId = variant.Id,
                        VariantTitle = variantTitle,
                        ProductionCost = variant.Cost,
                        Currency = currency,
                        ImageUrl = imageLookup.TryGetValue(variant.Id, out var imageUrl) ? imageUrl : null,
                        IsAvailable = variant.IsAvailable,
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

        return snapshotsByVariantId.Values
            .OrderBy(snapshot => snapshot.VariantTitle, StringComparer.OrdinalIgnoreCase)
            .ThenBy(snapshot => snapshot.VariantId)
            .ToList();
    }

    private static List<PrintArea> BuildProbePrintAreas(
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

    private static string BuildPlaceholderSignature(IReadOnlyList<VariantPlaceholder> placeholders)
    {
        return string.Join(
            "||",
            placeholders
                .OrderBy(placeholder => placeholder.Position, StringComparer.OrdinalIgnoreCase)
                .ThenBy(placeholder => placeholder.DecorationMethod, StringComparer.OrdinalIgnoreCase)
                .ThenBy(placeholder => placeholder.Width)
                .ThenBy(placeholder => placeholder.Height)
                .Select(placeholder =>
                    $"{placeholder.Position.Trim()}|{(placeholder.DecorationMethod ?? string.Empty).Trim()}|{placeholder.Width}|{placeholder.Height}"));
    }

    private static async Task<Product> WaitForProductImagesAsync(PrintifyClient client, int shopId, Product product)
    {
        var current = product;
        if ((current.Images?.Count ?? 0) > 0)
            return current;

        for (var attempt = 0; attempt < 5; attempt++)
        {
            await Task.Delay(TimeSpan.FromSeconds(1.5));
            current = await client.GetProductAsync(shopId, current.Id);

            if ((current.Images?.Count ?? 0) > 0)
                return current;
        }

        return current;
    }

    private static Dictionary<int, (string Name, string Title)> BuildProductOptionLookup(Product product)
    {
        var optionLookup = new Dictionary<int, (string Name, string Title)>();

        if (product.Options is null)
            return optionLookup;

        foreach (var option in product.Options)
        {
            if (option.Values is null)
                continue;

            foreach (var value in option.Values)
            {
                optionLookup[value.Id] = (option.Type, value.Title);
            }
        }

        return optionLookup;
    }

    private static Dictionary<int, string> BuildProductVariantImageLookup(Product product)
    {
        var lookup = new Dictionary<int, (string Url, int Rank)>();

        foreach (var image in product.Images ?? Enumerable.Empty<ProductMockupImage>())
        {
            var imageUrl = image.Src?.Trim();
            if (string.IsNullOrWhiteSpace(imageUrl))
                continue;

            var rank = 0;
            if (!image.IsDefault)
                rank += 10;

            if (!string.Equals(image.Position, "front", StringComparison.OrdinalIgnoreCase))
                rank += 5;

            foreach (var variantId in image.VariantIds)
            {
                if (variantId <= 0)
                    continue;

                if (lookup.TryGetValue(variantId, out var existing) && existing.Rank <= rank)
                    continue;

                lookup[variantId] = (imageUrl, rank);
            }
        }

        return lookup.ToDictionary(entry => entry.Key, entry => entry.Value.Url);
    }

    private static string FormatProductVariantOptions(
        IReadOnlyList<int>? optionIds,
        IReadOnlyDictionary<int, (string Name, string Title)> optionLookup)
    {
        if (optionIds is null || optionIds.Count == 0)
            return "(none)";

        return string.Join(", ", optionIds
            .Select(optionId => optionLookup.TryGetValue(optionId, out var option)
                ? (Name: option.Name, Title: option.Title)
                : (Name: $"option_{optionId}", Title: optionId.ToString(CultureInfo.InvariantCulture)))
            .OrderBy(option => option.Name, StringComparer.OrdinalIgnoreCase)
            .Select(option => $"{option.Name}={option.Title}"));
    }

    private static string DetermineProviderCurrency(ShippingInfo shipping, string countryCode)
    {
        return shipping.Profiles
            .Where(profile => ProfileContainsCountry(profile, countryCode))
            .Select(profile => profile.FirstItem?.Currency ?? profile.AdditionalItems?.Currency ?? string.Empty)
            .FirstOrDefault(currency => !string.IsNullOrWhiteSpace(currency))
            ?? shipping.Profiles
                .Where(ProfileContainsRestOfWorld)
            .Select(profile => profile.FirstItem?.Currency ?? profile.AdditionalItems?.Currency ?? string.Empty)
            .FirstOrDefault(currency => !string.IsNullOrWhiteSpace(currency))
            ?? string.Empty;
    }

    private static IReadOnlyDictionary<int, BlueprintLivePricingSnapshot> BuildSharedLivePricingLookup(
        BlueprintCountrySettingsDocument settings)
    {
        var lookup = new Dictionary<int, BlueprintLivePricingSnapshot>();

        foreach (var blueprintSelection in settings.Countries
            .SelectMany(country => country.Blueprints)
            .Where(entry => entry.BlueprintId > 0 && entry.LivePricing is not null)
            .OrderBy(entry => entry.LivePricing!.UpdatedAtUtc))
        {
            var livePricing = blueprintSelection.LivePricing!;

            if (lookup.TryGetValue(blueprintSelection.BlueprintId, out var existing))
            {
                lookup[blueprintSelection.BlueprintId] = MergeLivePricingSnapshot(existing, livePricing);
                continue;
            }

            lookup[blueprintSelection.BlueprintId] = CloneLivePricingSnapshot(livePricing);
        }

        return lookup;
    }

    private static BlueprintLivePricingSnapshot CloneLivePricingSnapshot(BlueprintLivePricingSnapshot livePricing)
    {
        return new BlueprintLivePricingSnapshot
        {
            UpdatedAtUtc = livePricing.UpdatedAtUtc,
            ShopId = livePricing.ShopId,
            ShopTitle = livePricing.ShopTitle,
            SampleImagePreviewUrl = livePricing.SampleImagePreviewUrl,
            Providers = livePricing.Providers
                .Select(CloneProviderSnapshot)
                .ToList()
        };
    }

    private static BlueprintLivePricingSnapshot MergeLivePricingSnapshot(
        BlueprintLivePricingSnapshot existing,
        BlueprintLivePricingSnapshot refreshed)
    {
        return new BlueprintLivePricingSnapshot
        {
            UpdatedAtUtc = refreshed.UpdatedAtUtc > existing.UpdatedAtUtc
                ? refreshed.UpdatedAtUtc
                : existing.UpdatedAtUtc,
            ShopId = refreshed.ShopId ?? existing.ShopId,
            ShopTitle = string.IsNullOrWhiteSpace(refreshed.ShopTitle)
                ? existing.ShopTitle
                : refreshed.ShopTitle,
            SampleImagePreviewUrl = string.IsNullOrWhiteSpace(refreshed.SampleImagePreviewUrl)
                ? existing.SampleImagePreviewUrl
                : refreshed.SampleImagePreviewUrl,
            Providers = MergeProviderSnapshots(existing, refreshed.Providers)
        };
    }

    private readonly record struct ShippingQuoteSummary(int? FirstItemCost, int? AdditionalItemCost, string Currency);
    private readonly record struct ShippingOptionSummary(string? Method, int? Cost);
    private readonly record struct DefaultBlueprintVariantSelection(
        int ProviderId,
        int VariantId,
        int TotalCost,
        int ProductionCost,
        int ShippingCost);

    private readonly record struct DefaultBlueprintVariantSelectionCandidate(
        int ProviderId,
        int VariantId,
        string ColorKey,
        string VariantTypeKey,
        IReadOnlyList<DefaultBlueprintVariantOption> VariantTypeOptions,
        int TotalCost,
        int ProductionCost,
        int ShippingCost);

    private readonly record struct DefaultBlueprintVariantOption(string Key, string Value);

    private readonly record struct DefaultBlueprintColorSelection(
        string ColorKey,
        double AverageTotalCost,
        int VariantCount,
        IReadOnlyList<DefaultBlueprintVariantSelectionCandidate> Variants);

    private sealed class DefaultBlueprintVariantTypeComparer : IComparer<DefaultBlueprintVariantSelectionCandidate>
    {
        public static DefaultBlueprintVariantTypeComparer Instance { get; } = new();

        public int Compare(DefaultBlueprintVariantSelectionCandidate left, DefaultBlueprintVariantSelectionCandidate right)
        {
            var optionCountComparison = left.VariantTypeOptions.Count.CompareTo(right.VariantTypeOptions.Count);
            if (optionCountComparison != 0)
            {
                return optionCountComparison;
            }

            for (var index = 0; index < left.VariantTypeOptions.Count; index++)
            {
                var leftOption = left.VariantTypeOptions[index];
                var rightOption = right.VariantTypeOptions[index];

                var keyComparison = string.Compare(leftOption.Key, rightOption.Key, StringComparison.OrdinalIgnoreCase);
                if (keyComparison != 0)
                {
                    return keyComparison;
                }

                var valueComparison = CompareVariantOptionValues(leftOption.Key, leftOption.Value, rightOption.Value);
                if (valueComparison != 0)
                {
                    return valueComparison;
                }
            }

            return string.Compare(left.VariantTypeKey, right.VariantTypeKey, StringComparison.OrdinalIgnoreCase);
        }
    }

    private static ShippingOptionSummary ResolveLowestShippingOption(ShippingCostResponse shipping)
    {
        var bestOption = new[]
        {
            (Method: "standard", Cost: shipping.Standard, Rank: 0),
            (Method: "economy", Cost: shipping.Economy, Rank: 1),
            (Method: "express", Cost: shipping.Express, Rank: 2),
            (Method: "priority", Cost: shipping.Priority, Rank: 3),
            (Method: "printify_express", Cost: shipping.PrintifyExpress, Rank: 4)
        }
        .Where(option => option.Cost > 0)
        .OrderBy(option => option.Cost)
        .ThenBy(option => option.Rank)
        .FirstOrDefault();

        return bestOption == default
            ? default
            : new ShippingOptionSummary(bestOption.Method, bestOption.Cost);
    }

    private readonly record struct BlueprintLivePricingBuildResult(
        BlueprintLivePricingSnapshot LivePricing,
        int ProviderCount,
        int VariantCount);
}

public sealed record BlueprintCatalogSnapshot(
    string CountryCode,
    string SearchTerm,
    bool EnabledOnly,
    string SettingsPath,
    IReadOnlyList<string> SavedCountryCodes,
    IReadOnlyList<BlueprintCatalogBlueprintNode> Blueprints,
    int BlueprintCount,
    int ProviderCount,
    int VariantCount,
    int EnabledBlueprintCount,
    int EnabledVariantCount,
    int LivePricedBlueprintCount,
    DateTime? LastLiveProbeAtUtc,
    string? LastProbeShopLabel);

public sealed record BlueprintCatalogBlueprintNode(
    int BlueprintId,
    string Title,
    string Brand,
    string Model,
    string? ImageUrl,
    bool IsEnabled,
    int ProviderCount,
    int VariantCount,
    int EnabledVariantCount,
    DateTime? LivePricingUpdatedAtUtc,
    string ProbeShopLabel,
    IReadOnlyList<BlueprintCatalogProviderNode> Providers);

public sealed record BlueprintCatalogProviderNode(
    int ProviderId,
    string Title,
    string HandlingTimeText,
    string ShippingCurrency,
    int LowestFirstItemShippingCost,
    int VariantCount,
    int EnabledVariantCount,
    IReadOnlyList<BlueprintCatalogVariantNode> Variants);

public sealed record BlueprintCatalogVariantNode(
    int VariantId,
    string Title,
    string? ImageUrl,
    string OptionSummary,
    bool IsAvailable,
    string SelectionMode,
    bool IsEffectivelyEnabled,
    int? ProductionCost,
    string ProductionCurrency,
    DateTime? ProductionCostUpdatedAtUtc,
    int? ShippingFirstItemCost,
    int? ShippingAdditionalItemCost,
    string ShippingCurrency,
    int? LiveShippingCost,
    string LiveShippingCurrency,
    string? LiveShippingMethod,
    string? PricingProductId,
    string? PricingProductTitle,
    int? PricingProductPageNumber);

public sealed record BlueprintPricingRefreshResult(
    string Message,
    int BlueprintCount,
    int ProviderCount,
    int VariantCount,
    IReadOnlyList<string> Warnings);