namespace PrintifyGenerator.Scraper.Scraping.Ebay;

public sealed class EbayScrapeRequest
{
    public string SearchTerm { get; init; } = string.Empty;
    public int PagesToScrape { get; init; } = 1;
    public int MaxItems { get; init; } = 120;
    public bool Headless { get; init; } = true;
    public string OutputDirectory { get; init; } = "./DataSets/RawData";
}
