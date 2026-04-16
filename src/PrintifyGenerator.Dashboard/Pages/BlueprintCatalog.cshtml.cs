using System.Globalization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using PrintifyGenerator.Dashboard.Models;
using PrintifyGenerator.Dashboard.Services;

namespace PrintifyGenerator.Dashboard.Pages;

public sealed class BlueprintCatalogModel : PageModel
{
    private const int BlueprintPageSize = 12;
    public const string SortByName = "name";
    public const string SortByBlueprintId = "blueprint-id";
    public const string SortByMinimumProductionCost = "minimum-production-cost";
    public const string SortByMinimumTotalPrice = "minimum-total-price";

    private readonly BlueprintCatalogService _blueprintCatalogService;

    public BlueprintCatalogModel(BlueprintCatalogService blueprintCatalogService)
    {
        _blueprintCatalogService = blueprintCatalogService;
    }

    [BindProperty(SupportsGet = true)]
    public string? CountryCode { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? SearchTerm { get; set; }

    [BindProperty(SupportsGet = true)]
    public bool EnabledOnly { get; set; }

    [BindProperty(SupportsGet = true, Name = "sortBy")]
    public string SortBy { get; set; } = SortByName;

    [BindProperty(SupportsGet = true, Name = "pageNumber")]
    public int PageNumber { get; set; } = 1;

    [TempData]
    public string? FlashMessage { get; set; }

    [TempData]
    public string? FlashErrorMessage { get; set; }

    public BlueprintCatalogSnapshot Snapshot { get; private set; } = null!;

    public PrintifyGenerator.Dashboard.Models.PaginationState Pagination { get; private set; } = PrintifyGenerator.Dashboard.Models.PaginationState.Empty;

    public IReadOnlyList<BlueprintCatalogBlueprintNode> VisibleBlueprints { get; private set; } = Array.Empty<BlueprintCatalogBlueprintNode>();

    public void OnGet()
    {
        LoadSnapshot();
    }

    public IActionResult OnPostSetBlueprintEnabled(
        string countryCode,
        string? searchTerm,
        bool enabledOnly,
        string? sortBy,
        int pageNumber,
        int blueprintId,
        bool isEnabled)
    {
        try
        {
            _blueprintCatalogService.SetBlueprintEnabled(countryCode, blueprintId, isEnabled);
            FlashMessage = isEnabled
                ? $"Blueprint {blueprintId} enabled for {NormalizeCountryCode(countryCode)}."
                : $"Blueprint {blueprintId} disabled for {NormalizeCountryCode(countryCode)}.";
        }
        catch (Exception ex)
        {
            FlashErrorMessage = ex.Message;
        }

        return RedirectToPage(new { countryCode, searchTerm, enabledOnly, sortBy = NormalizeSortBy(sortBy), pageNumber = NormalizePageNumber(pageNumber) });
    }

    public IActionResult OnPostSetVariantMode(
        string countryCode,
        string? searchTerm,
        bool enabledOnly,
        string? sortBy,
        int pageNumber,
        int blueprintId,
        int providerId,
        int variantId,
        string mode)
    {
        try
        {
            _blueprintCatalogService.SetVariantMode(countryCode, blueprintId, providerId, variantId, mode);
            FlashMessage = $"Variant {variantId} updated for {NormalizeCountryCode(countryCode)}.";
        }
        catch (Exception ex)
        {
            FlashErrorMessage = ex.Message;
        }

        return RedirectToPage(new { countryCode, searchTerm, enabledOnly, sortBy = NormalizeSortBy(sortBy), pageNumber = NormalizePageNumber(pageNumber) });
    }

    public IActionResult OnPostSetProviderEnabled(
        string countryCode,
        string? searchTerm,
        bool enabledOnly,
        string? sortBy,
        int pageNumber,
        int blueprintId,
        int providerId,
        bool isEnabled)
    {
        try
        {
            _blueprintCatalogService.SetProviderEnabled(countryCode, blueprintId, providerId, isEnabled);
            FlashMessage = isEnabled
                ? $"Provider {providerId} variants enabled for {NormalizeCountryCode(countryCode)}."
                : $"Provider {providerId} variants disabled for {NormalizeCountryCode(countryCode)}.";
        }
        catch (Exception ex)
        {
            FlashErrorMessage = ex.Message;
        }

        return RedirectToPage(new { countryCode, searchTerm, enabledOnly, sortBy = NormalizeSortBy(sortBy), pageNumber = NormalizePageNumber(pageNumber) });
    }

    public async Task<IActionResult> OnPostRefreshBlueprintPricingAsync(
        string countryCode,
        string? searchTerm,
        bool enabledOnly,
        string? sortBy,
        int pageNumber,
        int blueprintId)
    {
        try
        {
            var result = await _blueprintCatalogService.RefreshLivePricingAsync(
                countryCode,
                blueprintId,
                HttpContext.RequestAborted);

            FlashMessage = result.Message;
        }
        catch (Exception ex)
        {
            FlashErrorMessage = ex.Message;
        }

        return RedirectToPage(new { countryCode, searchTerm, enabledOnly, sortBy = NormalizeSortBy(sortBy), pageNumber = NormalizePageNumber(pageNumber) });
    }

    public async Task<IActionResult> OnPostRefreshEnabledPricingAsync(
        string countryCode,
        string? searchTerm,
        bool enabledOnly,
        string? sortBy)
    {
        try
        {
            var result = await _blueprintCatalogService.RefreshLivePricingAsync(
                countryCode,
                blueprintId: null,
                HttpContext.RequestAborted);

            FlashMessage = result.Message;
        }
        catch (Exception ex)
        {
            FlashErrorMessage = ex.Message;
        }

        return RedirectToPage(new { countryCode, searchTerm, enabledOnly, sortBy = NormalizeSortBy(sortBy), pageNumber = PageNumber });
    }

    public static string FormatMoney(int? minorUnits, string? currency)
    {
        if (!minorUnits.HasValue)
            return "Unavailable";

        var normalizedCurrency = string.IsNullOrWhiteSpace(currency)
            ? "N/A"
            : currency.Trim().ToUpperInvariant();

        return $"{normalizedCurrency} {(minorUnits.Value / 100m).ToString("0.00", CultureInfo.InvariantCulture)}";
    }

    public static string FormatTimestamp(DateTime? value)
    {
        return value.HasValue
            ? value.Value.ToLocalTime().ToString("yyyy-MM-dd HH:mm:ss")
            : "Not refreshed yet";
    }

    public static string FormatMoneyRange(IEnumerable<int?> minorUnits, string? currency)
    {
        var values = minorUnits
            .Where(value => value.HasValue)
            .Select(value => value!.Value)
            .Distinct()
            .OrderBy(value => value)
            .ToList();

        if (values.Count == 0)
            return "Unavailable";

        if (values.Count == 1)
            return FormatMoney(values[0], currency);

        var normalizedCurrency = string.IsNullOrWhiteSpace(currency)
            ? "N/A"
            : currency.Trim().ToUpperInvariant();

        return $"{normalizedCurrency} {(values[0] / 100m).ToString("0.00", CultureInfo.InvariantCulture)}-{(values[^1] / 100m).ToString("0.00", CultureInfo.InvariantCulture)}";
    }

    public static string FormatBlueprintProductionRange(BlueprintCatalogBlueprintNode blueprint)
    {
        var variants = blueprint.Providers
            .SelectMany(provider => provider.Variants)
            .ToList();
        var currency = variants
            .Select(variant => variant.ProductionCurrency)
            .FirstOrDefault(value => !string.IsNullOrWhiteSpace(value))
            ?? blueprint.Providers
                .Select(provider => provider.ShippingCurrency)
                .FirstOrDefault(value => !string.IsNullOrWhiteSpace(value));

        return FormatMoneyRange(variants.Select(variant => variant.ProductionCost), currency);
    }

    public static string FormatBlueprintDeliveryRange(BlueprintCatalogBlueprintNode blueprint)
    {
        var variants = blueprint.Providers
            .SelectMany(provider => provider.Variants)
            .ToList();
        var currency = variants
            .Select(ResolveDisplayedDeliveryCurrency)
            .FirstOrDefault(value => !string.IsNullOrWhiteSpace(value))
            ?? blueprint.Providers
                .Select(provider => provider.ShippingCurrency)
                .FirstOrDefault(value => !string.IsNullOrWhiteSpace(value));

        return FormatMoneyRange(variants.Select(ResolveDisplayedDeliveryCost), currency);
    }

    public static int? ResolveDisplayedDeliveryCost(BlueprintCatalogVariantNode variant)
    {
        return variant.LiveShippingCost ?? variant.ShippingFirstItemCost;
    }

    public static string ResolveDisplayedDeliveryCurrency(BlueprintCatalogVariantNode variant)
    {
        return !string.IsNullOrWhiteSpace(variant.LiveShippingCurrency)
            ? variant.LiveShippingCurrency
            : variant.ShippingCurrency;
    }

    public static string DescribeDeliverySource(BlueprintCatalogVariantNode variant)
    {
        if (variant.LiveShippingCost.HasValue)
        {
            var shippingMethod = FormatShippingMethod(variant.LiveShippingMethod);
            return string.IsNullOrWhiteSpace(shippingMethod)
                ? "delivery"
                : $"delivery · {shippingMethod}";
        }

        return "catalog shipping";
    }

    public static string FormatShippingMethod(string? method)
    {
        if (string.IsNullOrWhiteSpace(method))
            return string.Empty;

        return CultureInfo.InvariantCulture.TextInfo.ToTitleCase(
            method.Trim().Replace('_', ' ').ToLowerInvariant());
    }

    public static string DescribeVariantMode(string mode)
    {
        return BlueprintVariantSelectionModes.Normalize(mode) switch
        {
            BlueprintVariantSelectionModes.Enabled => "Always enabled",
            BlueprintVariantSelectionModes.Disabled => "Always disabled",
            _ => "Inherit blueprint"
        };
    }

    private void LoadSnapshot()
    {
        Snapshot = _blueprintCatalogService.LoadSnapshot(CountryCode, SearchTerm, EnabledOnly);
        SortBy = NormalizeSortBy(SortBy);
        var orderedBlueprints = SortBlueprints(Snapshot.Blueprints, SortBy).ToList();
        Pagination = PrintifyGenerator.Dashboard.Models.PaginationState.Create(PageNumber, BlueprintPageSize, orderedBlueprints.Count);
        VisibleBlueprints = orderedBlueprints
            .Skip(Pagination.SkipCount)
            .Take(Pagination.PageSize)
            .ToList();
        CountryCode = Snapshot.CountryCode;
        SearchTerm = Snapshot.SearchTerm;
        EnabledOnly = Snapshot.EnabledOnly;
        PageNumber = Pagination.CurrentPage;
    }

    private static string NormalizeCountryCode(string? countryCode)
    {
        return BlueprintCountrySettingsStore.NormalizeCountryCode(countryCode);
    }

    private static int NormalizePageNumber(int page)
    {
        return page <= 0 ? 1 : page;
    }

    private static string NormalizeSortBy(string? sortBy)
    {
        return sortBy?.Trim().ToLowerInvariant() switch
        {
            SortByBlueprintId => SortByBlueprintId,
            SortByMinimumProductionCost => SortByMinimumProductionCost,
            SortByMinimumTotalPrice => SortByMinimumTotalPrice,
            _ => SortByName
        };
    }

    private static IEnumerable<BlueprintCatalogBlueprintNode> SortBlueprints(
        IEnumerable<BlueprintCatalogBlueprintNode> blueprints,
        string sortBy)
    {
        return NormalizeSortBy(sortBy) switch
        {
            SortByBlueprintId => blueprints
                .OrderBy(blueprint => blueprint.BlueprintId)
                .ThenBy(blueprint => blueprint.Title, StringComparer.OrdinalIgnoreCase),
            SortByMinimumProductionCost => blueprints
                .OrderBy(blueprint => ResolveMinimumProductionCost(blueprint) ?? int.MaxValue)
                .ThenBy(blueprint => blueprint.Title, StringComparer.OrdinalIgnoreCase)
                .ThenBy(blueprint => blueprint.BlueprintId),
            SortByMinimumTotalPrice => blueprints
                .OrderBy(blueprint => ResolveMinimumTotalPrice(blueprint) ?? int.MaxValue)
                .ThenBy(blueprint => blueprint.Title, StringComparer.OrdinalIgnoreCase)
                .ThenBy(blueprint => blueprint.BlueprintId),
            _ => blueprints
                .OrderBy(blueprint => blueprint.Title, StringComparer.OrdinalIgnoreCase)
                .ThenBy(blueprint => blueprint.BlueprintId)
        };
    }

    private static int? ResolveMinimumProductionCost(BlueprintCatalogBlueprintNode blueprint)
    {
        return blueprint.Providers
            .SelectMany(provider => provider.Variants)
            .Where(variant => variant.ProductionCost.HasValue)
            .Select(variant => variant.ProductionCost!.Value)
            .DefaultIfEmpty()
            .Min() is var minimum && minimum > 0
                ? minimum
                : null;
    }

    private static int? ResolveMinimumTotalPrice(BlueprintCatalogBlueprintNode blueprint)
    {
        return blueprint.Providers
            .SelectMany(provider => provider.Variants)
            .Select(variant =>
            {
                var deliveryCost = ResolveDisplayedDeliveryCost(variant);
                return variant.ProductionCost.HasValue && deliveryCost.HasValue
                    ? variant.ProductionCost.Value + deliveryCost.Value
                    : (int?)null;
            })
            .Where(totalCost => totalCost.HasValue)
            .Select(totalCost => totalCost!.Value)
            .DefaultIfEmpty()
            .Min() is var minimum && minimum > 0
                ? minimum
                : null;
    }
}