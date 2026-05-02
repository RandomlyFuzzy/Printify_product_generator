using PrintifyGenerator.Scraper.Scraping.Core;

namespace PrintifyGenerator.Scraper.Scraping.Abstractions;

public interface ISiteScraper
{
    string SiteName { get; }
    Task<IReadOnlyList<ScrapeListing>> ScrapeAsync(ScrapeRequest request, CancellationToken cancellationToken = default);
}
