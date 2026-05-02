namespace PrintifyGenerator.Scraper.Scraping.Ebay;

public sealed class EbayScrapeRunOptions
{
    public required EbayScrapeRequest Request { get; init; }
    public int IntervalMinutes { get; init; } = 30;
    public bool Continuous { get; init; } = true;
}
