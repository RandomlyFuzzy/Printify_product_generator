using System.Globalization;
using System.Text;
using System.Text.Json;

namespace PrintifyGenerator.Scraper.Scraping.Ebay;

public sealed class EbayScrapeFileWriter
{
    public async Task<(string jsonPath, string csvPath)> WriteAsync(EbayScrapeResult result, CancellationToken cancellationToken = default)
    {
        Directory.CreateDirectory(result.Request.OutputDirectory);

        var safeTerm = Sanitize(result.Request.SearchTerm);
        var timestamp = DateTimeOffset.UtcNow.ToString("yyyyMMdd_HHmmss", CultureInfo.InvariantCulture);

        var jsonPath = Path.Combine(result.Request.OutputDirectory, $"ebay_{safeTerm}_{timestamp}.json");
        var csvPath = Path.Combine(result.Request.OutputDirectory, $"ebay_{safeTerm}_{timestamp}.csv");

        var jsonOptions = new JsonSerializerOptions
        {
            WriteIndented = true
        };

        await using (var jsonStream = File.Create(jsonPath))
        {
            await JsonSerializer.SerializeAsync(jsonStream, result, jsonOptions, cancellationToken);
        }

        await using (var csvStream = new StreamWriter(csvPath, false, Encoding.UTF8))
        {
            await csvStream.WriteLineAsync("SearchTerm,Title,PriceText,PriceValue,Url,ShippingText,ConditionText,Subtitle,LocationText,SoldText,ImageUrl,ResultsPage,PositionOnPage,CapturedAtUtc");

            foreach (var listing in result.Listings)
            {
                var row = string.Join(",",
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
                    listing.ResultsPage.ToString(CultureInfo.InvariantCulture),
                    listing.PositionOnPage.ToString(CultureInfo.InvariantCulture),
                    listing.CapturedAtUtc.ToString("O", CultureInfo.InvariantCulture));

                await csvStream.WriteLineAsync(row);
            }
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
        return string.IsNullOrWhiteSpace(normalized) ? "search" : normalized;
    }
}
