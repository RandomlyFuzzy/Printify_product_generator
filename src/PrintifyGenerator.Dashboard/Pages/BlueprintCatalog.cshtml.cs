using System.Globalization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using PrintifyGenerator.Dashboard.Services;

namespace PrintifyGenerator.Dashboard.Pages;

public sealed class BlueprintCatalogModel : PageModel
{
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

    [TempData]
    public string? FlashMessage { get; set; }

    [TempData]
    public string? FlashErrorMessage { get; set; }

    public BlueprintCatalogSnapshot Snapshot { get; private set; } = null!;

    public void OnGet()
    {
        LoadSnapshot();
    }

    public IActionResult OnPostSetBlueprintEnabled(
        string countryCode,
        string? searchTerm,
        bool enabledOnly,
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

        return RedirectToPage(new { countryCode, searchTerm, enabledOnly });
    }

    public IActionResult OnPostSetVariantMode(
        string countryCode,
        string? searchTerm,
        bool enabledOnly,
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

        return RedirectToPage(new { countryCode, searchTerm, enabledOnly });
    }

    public IActionResult OnPostSetProviderEnabled(
        string countryCode,
        string? searchTerm,
        bool enabledOnly,
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

        return RedirectToPage(new { countryCode, searchTerm, enabledOnly });
    }

    public async Task<IActionResult> OnPostRefreshBlueprintPricingAsync(
        string countryCode,
        string? searchTerm,
        bool enabledOnly,
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

        return RedirectToPage(new { countryCode, searchTerm, enabledOnly });
    }

    public async Task<IActionResult> OnPostRefreshEnabledPricingAsync(
        string countryCode,
        string? searchTerm,
        bool enabledOnly)
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

        return RedirectToPage(new { countryCode, searchTerm, enabledOnly });
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
        CountryCode = Snapshot.CountryCode;
        SearchTerm = Snapshot.SearchTerm;
        EnabledOnly = Snapshot.EnabledOnly;
    }

    private static string NormalizeCountryCode(string? countryCode)
    {
        return BlueprintCountrySettingsStore.NormalizeCountryCode(countryCode);
    }
}