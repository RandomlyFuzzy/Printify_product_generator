namespace PrintifyGenerator.Scraper.Scraping.Core;

public sealed class ScrapeListing
{
    public string ProductId { get; init; } = string.Empty;
    public string Site { get; init; } = string.Empty;
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
    public bool IsAuction { get; init; }
    public bool IsPromotion { get; init; }
    public bool istopSeller { get; init; }
    public int ResultsPage { get; init; }
    public int PositionOnPage { get; init; }
    public DateTimeOffset CapturedAtUtc { get; init; }


    public override string ToString()
    {
        return $"{Site} - {Title} - {PriceText} - {Url}";
    }
}
