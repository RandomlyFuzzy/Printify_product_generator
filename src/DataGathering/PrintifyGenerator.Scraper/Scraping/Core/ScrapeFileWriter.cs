using System.Globalization;
using System.Text;
using System.Text.Json;

namespace PrintifyGenerator.Scraper.Scraping.Core;

public sealed class ScrapeFileWriter
{
    public async Task<(string jsonPath, string csvPath)> WriteAsync(ScrapeResult result, CancellationToken cancellationToken = default)
    {
        Directory.CreateDirectory(result.Request.OutputDirectory);

        var timestamp = DateTimeOffset.UtcNow.ToString("yyyyMMdd_HHmmss", CultureInfo.InvariantCulture);
        var safeSite = result.Listings.FirstOrDefault()?.Site ?? "unknown";
        var safeTerm = Sanitize(result.Request.SearchTerm);

        var jsonPath = Path.Combine(result.Request.OutputDirectory, $"{safeSite}_{safeTerm}_{timestamp}.json");
        var csvPath = Path.Combine(result.Request.OutputDirectory, $"{safeSite}_{safeTerm}_{timestamp}.csv");

        // --- JSON: write listings ---
        var jsonOptions = new JsonSerializerOptions { WriteIndented = true };
        await using (var jsonStream = File.Create(jsonPath))
        {
            await JsonSerializer.SerializeAsync(jsonStream, result.Listings, jsonOptions, cancellationToken);
        }

        // --- CSV: write header only when creating the file, then append rows ---
        var csvExists = File.Exists(csvPath);
        await using var csvStream = new StreamWriter(csvPath, append: true, Encoding.UTF8);
        if (!csvExists)
        {
            await csvStream.WriteLineAsync("ProductId,Site,SearchTerm,Title,PriceText,PriceValue,Url,ShippingText,ConditionText,Subtitle,LocationText,SoldText,ImageUrl,IsAuction,IsPromotion,ResultsPage,PositionOnPage,CapturedAtUtc");
        }

        foreach (var listing in result.Listings)
        {
            var row = string.Join(",",
                EscapeCsv(listing.ProductId),
                EscapeCsv(listing.Site),
                EscapeCsv(listing.SearchTerm),
                EscapeCsv(listing.Title),
                EscapeCsv(listing.PriceText),
                listing.PriceValue?.ToString(CultureInfo.InvariantCulture) ?? string.Empty,
                EscapeCsv(listing.Url),
                EscapeCsv(listing.ShippingText),
                EscapeCsv(listing.ConditionText),
                EscapeCsv(listing.Subtitle),
                EscapeCsv(listing.LocationText),
                EscapeCsv(listing.SoldText),
                EscapeCsv(listing.ImageUrl),
                listing.IsAuction ? "true" : "false",
                listing.IsPromotion ? "true" : "false",
                listing.ResultsPage.ToString(CultureInfo.InvariantCulture),
                listing.PositionOnPage.ToString(CultureInfo.InvariantCulture),
                listing.CapturedAtUtc.ToString("O", CultureInfo.InvariantCulture));

            await csvStream.WriteLineAsync(row);
        }

        return (jsonPath, csvPath);
    }

    private static string EscapeCsv(string value)
    {
        var sanitized = value.Replace("\r", " ").Replace("\n", " ").Trim();
        return $"\"{sanitized.Replace("\"", "\"\"")}\"";
    }

    private static string Sanitize(string value)
    {
        var parts = value.Split(Path.GetInvalidFileNameChars(), StringSplitOptions.RemoveEmptyEntries);
        var normalized = string.Join("_", parts).Trim().ToLowerInvariant();
        return string.IsNullOrWhiteSpace(normalized) ? "data" : normalized;
    }
}
