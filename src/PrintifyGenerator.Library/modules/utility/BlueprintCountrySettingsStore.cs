using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;

public static class BlueprintCountrySettingsStore
{
    private static readonly JsonSerializerOptions JsonOptions = new JsonSerializerOptions
    {
        PropertyNameCaseInsensitive = true,
        WriteIndented = true
    };

    public static string GetSettingsPath(string dataBasePath)
    {
        var normalizedDataRoot = Path.GetFullPath(dataBasePath);
        return Path.Combine(normalizedDataRoot, "staging", "blueprint-country-settings.json");
    }

    public static BlueprintCountrySettingsDocument Load(string dataBasePath)
    {
        var settingsPath = GetSettingsPath(dataBasePath);
        if (!File.Exists(settingsPath))
            return new BlueprintCountrySettingsDocument();

        try
        {
            var json = File.ReadAllText(settingsPath);
            var loaded = JsonSerializer.Deserialize<BlueprintCountrySettingsDocument>(json, JsonOptions);
            return Sanitize(loaded ?? new BlueprintCountrySettingsDocument());
        }
        catch
        {
            return new BlueprintCountrySettingsDocument();
        }
    }

    public static void Save(string dataBasePath, BlueprintCountrySettingsDocument settings)
    {
        var sanitized = Sanitize(settings);
        var settingsPath = GetSettingsPath(dataBasePath);
        var directory = Path.GetDirectoryName(settingsPath);
        if (!string.IsNullOrWhiteSpace(directory))
            Directory.CreateDirectory(directory);

        File.WriteAllText(settingsPath, JsonSerializer.Serialize(sanitized, JsonOptions));
    }

    public static string NormalizeCountryCode(string? countryCode)
    {
        return (countryCode ?? string.Empty).Trim().ToUpperInvariant();
    }

    private static BlueprintCountrySettingsDocument Sanitize(BlueprintCountrySettingsDocument settings)
    {
        var sanitized = new BlueprintCountrySettingsDocument();

        foreach (var country in settings.Countries ?? new List<BlueprintCountrySelection>())
        {
            var normalizedCountryCode = NormalizeCountryCode(country.CountryCode);
            if (string.IsNullOrWhiteSpace(normalizedCountryCode))
                continue;

            sanitized.Countries.RemoveAll(existing =>
                string.Equals(existing.CountryCode, normalizedCountryCode, StringComparison.OrdinalIgnoreCase));

            sanitized.Countries.Add(new BlueprintCountrySelection
            {
                CountryCode = normalizedCountryCode,
                UpdatedAtUtc = country.UpdatedAtUtc == default ? DateTime.UtcNow : country.UpdatedAtUtc,
                Blueprints = SanitizeBlueprints(country.Blueprints)
            });
        }

        sanitized.Countries = sanitized.Countries
            .OrderBy(country => country.CountryCode, StringComparer.OrdinalIgnoreCase)
            .ToList();

        return sanitized;
    }

    private static List<BlueprintCountrySelectionEntry> SanitizeBlueprints(List<BlueprintCountrySelectionEntry>? blueprints)
    {
        var sanitized = new List<BlueprintCountrySelectionEntry>();

        foreach (var blueprint in blueprints ?? new List<BlueprintCountrySelectionEntry>())
        {
            if (blueprint.BlueprintId <= 0)
                continue;

            sanitized.RemoveAll(existing => existing.BlueprintId == blueprint.BlueprintId);

            sanitized.Add(new BlueprintCountrySelectionEntry
            {
                BlueprintId = blueprint.BlueprintId,
                Enabled = blueprint.Enabled,
                UpdatedAtUtc = blueprint.UpdatedAtUtc == default ? DateTime.UtcNow : blueprint.UpdatedAtUtc,
                VariantOverrides = SanitizeVariantOverrides(blueprint.VariantOverrides),
                LivePricing = SanitizeLivePricing(blueprint.LivePricing)
            });
        }

        return sanitized
            .OrderBy(entry => entry.BlueprintId)
            .ToList();
    }

    private static List<BlueprintVariantOverrideEntry> SanitizeVariantOverrides(List<BlueprintVariantOverrideEntry>? overrides)
    {
        var sanitized = new List<BlueprintVariantOverrideEntry>();

        foreach (var entry in overrides ?? new List<BlueprintVariantOverrideEntry>())
        {
            if (entry.ProviderId <= 0 || entry.VariantId <= 0)
                continue;

            var normalizedMode = BlueprintVariantSelectionModes.Normalize(entry.Mode);
            if (normalizedMode == BlueprintVariantSelectionModes.Inherit)
                continue;

            sanitized.RemoveAll(existing => existing.ProviderId == entry.ProviderId && existing.VariantId == entry.VariantId);

            sanitized.Add(new BlueprintVariantOverrideEntry
            {
                ProviderId = entry.ProviderId,
                VariantId = entry.VariantId,
                Mode = normalizedMode,
                UpdatedAtUtc = entry.UpdatedAtUtc == default ? DateTime.UtcNow : entry.UpdatedAtUtc
            });
        }

        return sanitized
            .OrderBy(entry => entry.ProviderId)
            .ThenBy(entry => entry.VariantId)
            .ToList();
    }

    private static BlueprintLivePricingSnapshot? SanitizeLivePricing(BlueprintLivePricingSnapshot? livePricing)
    {
        if (livePricing is null)
            return null;

        var sanitized = new BlueprintLivePricingSnapshot
        {
            UpdatedAtUtc = livePricing.UpdatedAtUtc == default ? DateTime.UtcNow : livePricing.UpdatedAtUtc,
            ShopId = livePricing.ShopId > 0 ? livePricing.ShopId : null,
            ShopTitle = (livePricing.ShopTitle ?? string.Empty).Trim(),
            SampleImagePreviewUrl = string.IsNullOrWhiteSpace(livePricing.SampleImagePreviewUrl)
                ? null
                : livePricing.SampleImagePreviewUrl.Trim(),
            Providers = SanitizeLiveProviders(livePricing.Providers)
        };

        return sanitized.Providers.Count == 0 && sanitized.ShopId is null && string.IsNullOrWhiteSpace(sanitized.SampleImagePreviewUrl)
            ? null
            : sanitized;
    }

    private static List<BlueprintProviderLivePricingSnapshot> SanitizeLiveProviders(List<BlueprintProviderLivePricingSnapshot>? providers)
    {
        var sanitized = new List<BlueprintProviderLivePricingSnapshot>();

        foreach (var provider in providers ?? new List<BlueprintProviderLivePricingSnapshot>())
        {
            if (provider.ProviderId <= 0)
                continue;

            sanitized.RemoveAll(existing => existing.ProviderId == provider.ProviderId);

            sanitized.Add(new BlueprintProviderLivePricingSnapshot
            {
                ProviderId = provider.ProviderId,
                ProviderTitle = (provider.ProviderTitle ?? string.Empty).Trim(),
                Currency = (provider.Currency ?? string.Empty).Trim().ToUpperInvariant(),
                Variants = SanitizeLiveVariants(provider.Variants)
            });
        }

        return sanitized
            .OrderBy(provider => provider.ProviderId)
            .ToList();
    }

    private static List<BlueprintVariantLivePricingSnapshot> SanitizeLiveVariants(List<BlueprintVariantLivePricingSnapshot>? variants)
    {
        var sanitized = new List<BlueprintVariantLivePricingSnapshot>();

        foreach (var variant in variants ?? new List<BlueprintVariantLivePricingSnapshot>())
        {
            if (variant.VariantId <= 0)
                continue;

            sanitized.RemoveAll(existing => existing.VariantId == variant.VariantId);

            sanitized.Add(new BlueprintVariantLivePricingSnapshot
            {
                VariantId = variant.VariantId,
                VariantTitle = (variant.VariantTitle ?? string.Empty).Trim(),
                ProductionCost = variant.ProductionCost,
                Currency = (variant.Currency ?? string.Empty).Trim().ToUpperInvariant(),
                ImageUrl = string.IsNullOrWhiteSpace(variant.ImageUrl)
                    ? null
                    : variant.ImageUrl.Trim(),
                IsAvailable = variant.IsAvailable,
                DeliveryCost = variant.DeliveryCost,
                DeliveryCurrency = (variant.DeliveryCurrency ?? string.Empty).Trim().ToUpperInvariant(),
                DeliveryMethod = string.IsNullOrWhiteSpace(variant.DeliveryMethod)
                    ? null
                    : variant.DeliveryMethod.Trim().ToLowerInvariant(),
                PricingProductId = string.IsNullOrWhiteSpace(variant.PricingProductId)
                    ? null
                    : variant.PricingProductId.Trim(),
                PricingProductTitle = string.IsNullOrWhiteSpace(variant.PricingProductTitle)
                    ? null
                    : variant.PricingProductTitle.Trim(),
                PricingProductPageNumber = variant.PricingProductPageNumber > 0
                    ? variant.PricingProductPageNumber
                    : null,
                UpdatedAtUtc = variant.UpdatedAtUtc == default ? DateTime.UtcNow : variant.UpdatedAtUtc
            });
        }

        return sanitized
            .OrderBy(variant => variant.VariantId)
            .ToList();
    }
}

public static class BlueprintVariantSelectionModes
{
    public const string Inherit = "inherit";
    public const string Enabled = "enabled";
    public const string Disabled = "disabled";

    public static string Normalize(string? mode)
    {
        return mode?.Trim().ToLowerInvariant() switch
        {
            Enabled => Enabled,
            Disabled => Disabled,
            _ => Inherit
        };
    }

    public static bool ResolveEffectiveState(bool blueprintEnabled, BlueprintVariantOverrideEntry? variantOverride)
    {
        return Normalize(variantOverride?.Mode) switch
        {
            Enabled => true,
            Disabled => false,
            _ => blueprintEnabled
        };
    }
}

public sealed class BlueprintCountrySettingsDocument
{
    public List<BlueprintCountrySelection> Countries { get; set; } = new();
}

public sealed class BlueprintCountrySelection
{
    public string CountryCode { get; set; } = string.Empty;
    public DateTime UpdatedAtUtc { get; set; }
    public List<BlueprintCountrySelectionEntry> Blueprints { get; set; } = new();
}

public sealed class BlueprintCountrySelectionEntry
{
    public int BlueprintId { get; set; }
    public bool Enabled { get; set; }
    public DateTime UpdatedAtUtc { get; set; }
    public List<BlueprintVariantOverrideEntry> VariantOverrides { get; set; } = new();
    public BlueprintLivePricingSnapshot? LivePricing { get; set; }
}

public sealed class BlueprintVariantOverrideEntry
{
    public int ProviderId { get; set; }
    public int VariantId { get; set; }
    public string Mode { get; set; } = BlueprintVariantSelectionModes.Inherit;
    public DateTime UpdatedAtUtc { get; set; }
}

public sealed class BlueprintLivePricingSnapshot
{
    public DateTime UpdatedAtUtc { get; set; }
    public int? ShopId { get; set; }
    public string ShopTitle { get; set; } = string.Empty;
    public string? SampleImagePreviewUrl { get; set; }
    public List<BlueprintProviderLivePricingSnapshot> Providers { get; set; } = new();
}

public sealed class BlueprintProviderLivePricingSnapshot
{
    public int ProviderId { get; set; }
    public string ProviderTitle { get; set; } = string.Empty;
    public string Currency { get; set; } = string.Empty;
    public List<BlueprintVariantLivePricingSnapshot> Variants { get; set; } = new();
}

public sealed class BlueprintVariantLivePricingSnapshot
{
    public int VariantId { get; set; }
    public string VariantTitle { get; set; } = string.Empty;
    public int? ProductionCost { get; set; }
    public string Currency { get; set; } = string.Empty;
    public string? ImageUrl { get; set; }
    public bool IsAvailable { get; set; }
    public int? DeliveryCost { get; set; }
    public string DeliveryCurrency { get; set; } = string.Empty;
    public string? DeliveryMethod { get; set; }
    public string? PricingProductId { get; set; }
    public string? PricingProductTitle { get; set; }
    public int? PricingProductPageNumber { get; set; }
    public DateTime UpdatedAtUtc { get; set; }
}