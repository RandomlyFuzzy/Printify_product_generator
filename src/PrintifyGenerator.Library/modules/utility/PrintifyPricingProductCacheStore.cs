using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;

public static class PrintifyPricingProductCacheStore
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true,
        WriteIndented = true
    };

    public static string GetCachePath(string dataRoot, int shopId)
    {
        if (shopId <= 0)
            throw new ArgumentOutOfRangeException(nameof(shopId), "Shop ID must be greater than zero.");

        var normalizedDataRoot = Path.GetFullPath(dataRoot);
        return Path.Combine(normalizedDataRoot, "Cached", $"pricing_products_shop_{shopId}.json");
    }

    public static PrintifyPricingProductCacheDocument Load(string filePath)
    {
        if (!File.Exists(filePath))
            return new PrintifyPricingProductCacheDocument();

        try
        {
            var json = File.ReadAllText(filePath);
            var loaded = JsonSerializer.Deserialize<PrintifyPricingProductCacheDocument>(json, JsonOptions);
            return Sanitize(loaded ?? new PrintifyPricingProductCacheDocument());
        }
        catch
        {
            return new PrintifyPricingProductCacheDocument();
        }
    }

    public static void Save(string filePath, PrintifyPricingProductCacheDocument document)
    {
        var sanitized = Sanitize(document);
        var directory = Path.GetDirectoryName(filePath);
        if (!string.IsNullOrWhiteSpace(directory))
            Directory.CreateDirectory(directory);

        File.WriteAllText(filePath, JsonSerializer.Serialize(sanitized, JsonOptions));
    }

    private static PrintifyPricingProductCacheDocument Sanitize(PrintifyPricingProductCacheDocument document)
    {
        return new PrintifyPricingProductCacheDocument
        {
            GeneratedAtUtc = document.GeneratedAtUtc,
            ShopId = document.ShopId > 0 ? document.ShopId : 0,
            ShopTitle = (document.ShopTitle ?? string.Empty).Trim(),
            ProbeUploadId = NormalizeOptionalString(document.ProbeUploadId),
            ProbeUploadPreviewUrl = NormalizeOptionalString(document.ProbeUploadPreviewUrl),
            Entries = SanitizeEntries(document.Entries)
        };
    }

    private static List<PrintifyPricingProductCacheEntry> SanitizeEntries(List<PrintifyPricingProductCacheEntry>? entries)
    {
        var sanitized = new List<PrintifyPricingProductCacheEntry>();

        foreach (var entry in entries ?? new List<PrintifyPricingProductCacheEntry>())
        {
            if (entry.ShopId <= 0 ||
                entry.BlueprintId <= 0 ||
                entry.ProviderId <= 0 ||
                entry.PageNumber <= 0 ||
                string.IsNullOrWhiteSpace(entry.Title) ||
                string.IsNullOrWhiteSpace(entry.ProductId))
            {
                continue;
            }

            sanitized.RemoveAll(existing =>
                existing.ShopId == entry.ShopId &&
                existing.BlueprintId == entry.BlueprintId &&
                existing.ProviderId == entry.ProviderId &&
                existing.PageNumber == entry.PageNumber);

            sanitized.Add(new PrintifyPricingProductCacheEntry
            {
                ShopId = entry.ShopId,
                BlueprintId = entry.BlueprintId,
                BlueprintTitle = (entry.BlueprintTitle ?? string.Empty).Trim(),
                ProviderId = entry.ProviderId,
                ProviderTitle = (entry.ProviderTitle ?? string.Empty).Trim(),
                PageNumber = entry.PageNumber,
                Title = entry.Title.Trim(),
                ProductId = entry.ProductId.Trim(),
                Visible = entry.Visible,
                IsPublished = entry.IsPublished,
                ExternalId = NormalizeOptionalString(entry.ExternalId),
                ExternalHandle = NormalizeOptionalString(entry.ExternalHandle),
                UpdatedAtUtc = entry.UpdatedAtUtc,
                VariantIds = (entry.VariantIds ?? new List<int>())
                    .Where(variantId => variantId > 0)
                    .Distinct()
                    .OrderBy(variantId => variantId)
                    .ToList()
            });
        }

        return sanitized
            .OrderBy(entry => entry.BlueprintId)
            .ThenBy(entry => entry.ProviderId)
            .ThenBy(entry => entry.PageNumber)
            .ToList();
    }

    private static string? NormalizeOptionalString(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }
}

public sealed class PrintifyPricingProductCacheDocument
{
    public DateTime GeneratedAtUtc { get; set; }
    public int ShopId { get; set; }
    public string ShopTitle { get; set; } = string.Empty;
    public string? ProbeUploadId { get; set; }
    public string? ProbeUploadPreviewUrl { get; set; }
    public List<PrintifyPricingProductCacheEntry> Entries { get; set; } = new();
}

public sealed class PrintifyPricingProductCacheEntry
{
    public int ShopId { get; set; }
    public int BlueprintId { get; set; }
    public string BlueprintTitle { get; set; } = string.Empty;
    public int ProviderId { get; set; }
    public string ProviderTitle { get; set; } = string.Empty;
    public int PageNumber { get; set; }
    public string Title { get; set; } = string.Empty;
    public string ProductId { get; set; } = string.Empty;
    public bool Visible { get; set; }
    public bool IsPublished { get; set; }
    public string? ExternalId { get; set; }
    public string? ExternalHandle { get; set; }
    public DateTime UpdatedAtUtc { get; set; }
    public List<int> VariantIds { get; set; } = new();
}
