namespace PrintifyGenerator.Scraper.Scraping.Abstractions;

public interface IWebScraper<in TRequest, TResult>
{
    Task<TResult> ScrapeAsync(TRequest request, CancellationToken cancellationToken = default);
}
