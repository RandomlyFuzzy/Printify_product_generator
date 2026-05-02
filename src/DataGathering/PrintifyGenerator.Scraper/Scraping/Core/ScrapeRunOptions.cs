namespace PrintifyGenerator.Scraper.Scraping.Core;

public sealed class ScrapeRunOptions
{
    public required ScrapeRequest Request { get; init; }
    public int IntervalMinutes { get; init; } = 30;
    public bool Continuous { get; init; } = true;
}
