namespace PrintifyGenerator.Scraper.Scraping.Ebay;

public sealed class EbayScrapeResult
{
    public required EbayScrapeRequest Request { get; init; }
    public required IReadOnlyList<EbayListing> Listings { get; init; }
    public required DateTimeOffset StartedAtUtc { get; init; }
    public required DateTimeOffset FinishedAtUtc { get; init; }
}
