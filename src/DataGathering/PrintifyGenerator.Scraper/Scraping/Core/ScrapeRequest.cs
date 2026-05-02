namespace PrintifyGenerator.Scraper.Scraping.Core;

public sealed class ScrapeRequest
{
    public IReadOnlyList<string> Sites { get; init; } = ["ebay"];
    public string SearchTerm { get; init; } = string.Empty;
    public int PagesToScrape { get; init; } = 1;
    public int MaxItemsPerSite { get; init; } = 120;
    public bool Headless { get; init; } = false;
    public string OutputDirectory { get; init; } = "./DataSets/RawData";
    public int MaxScrollRoundsPerPage { get; init; } = 8;
    public int ScrollStagnationLimit { get; init; } = 3;
}
