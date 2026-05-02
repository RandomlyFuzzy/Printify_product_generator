namespace PrintifyGenerator.Scraper.Scraping.Ebay;

public sealed class EbayListing
{
    public string SearchTerm { get; init; } = string.Empty;
    public string Title { get; init; } = string.Empty;
    public string PriceText { get; init; } = string.Empty;
    public decimal? PriceValue { get; init; }
    public string Url { get; init; } = string.Empty;
    public string ShippingText { get; init; } = string.Empty;
    public string ConditionText { get; init; } = string.Empty;
    public string Subtitle { get; init; } = string.Empty;
    public string LocationText { get; init; } = string.Empty;
    public string SoldText { get; init; } = string.Empty;
    public string ImageUrl { get; init; } = string.Empty;
    public int ResultsPage { get; init; }
    public int PositionOnPage { get; init; }
    public DateTimeOffset CapturedAtUtc { get; init; }
}
