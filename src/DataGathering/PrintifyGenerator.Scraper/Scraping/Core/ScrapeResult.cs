namespace PrintifyGenerator.Scraper.Scraping.Core;

public sealed class ScrapeResult
{
    public required ScrapeRequest Request { get; init; }
    public required IReadOnlyList<ScrapeListing> Listings { get; init; }
    public required DateTimeOffset StartedAtUtc { get; init; }
    public required DateTimeOffset FinishedAtUtc { get; init; }
}
