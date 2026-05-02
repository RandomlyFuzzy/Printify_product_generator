using System.Globalization;
using System.Text.RegularExpressions;
using PuppeteerSharp;
using PrintifyGenerator.Scraper.Scraping.Abstractions;

namespace PrintifyGenerator.Scraper.Scraping.Ebay;

public sealed class EbayScraper : IWebScraper<EbayScrapeRequest, EbayScrapeResult>
{
    private static readonly Regex PriceRegex = new("\\d+[\\d,]*\\.?\\d*", RegexOptions.Compiled);
    private readonly IHumanBehaviorSimulator _human;
    private readonly Random _random;

    public EbayScraper(IHumanBehaviorSimulator human, Random random)
    {
        _human = human;
        _random = random;
    }

    public async Task<EbayScrapeResult> ScrapeAsync(EbayScrapeRequest request, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.SearchTerm))
        {
            throw new ArgumentException("Search term is required.", nameof(request));
        }

        var startedAt = DateTimeOffset.UtcNow;
        var listings = new List<EbayListing>();
        var dedupe = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        await new BrowserFetcher().DownloadAsync();

        var launchOptions = new LaunchOptions
        {
            Headless = request.Headless,
            Args =
            [
                "--no-sandbox",
                "--disable-blink-features=AutomationControlled",
                "--disable-dev-shm-usage"
            ]
        };

        await using var browser = await Puppeteer.LaunchAsync(launchOptions);
        await using var page = await browser.NewPageAsync();

        await page.SetUserAgentAsync(RandomUserAgentProvider.GetRandom(_random));
        await page.SetViewportAsync(new ViewPortOptions
        {
            Width = _random.Next(1200, 1680),
            Height = _random.Next(760, 1080)
        });

        await page.GoToAsync("https://www.ebay.com/", new NavigationOptions
        {
            WaitUntil = [WaitUntilNavigation.Networkidle2],
            Timeout = 60000
        });

        await TryAcceptCookiesAsync(page, cancellationToken);
        await _human.RandomMouseMovementAsync(page, cancellationToken: cancellationToken);
        await _human.TypeLikeHumanAsync(page, "input[aria-label='Search for anything']", request.SearchTerm, cancellationToken);
        await _human.RandomDelayAsync(150, 700, cancellationToken);
        await page.Keyboard.PressAsync("Enter");
        await page.WaitForSelectorAsync("li.s-item", new WaitForSelectorOptions { Timeout = 60000 });

        var currentPage = 1;
        while (currentPage <= request.PagesToScrape && listings.Count < request.MaxItems)
        {
            cancellationToken.ThrowIfCancellationRequested();

            await _human.RandomMouseMovementAsync(page, _random.Next(4, 10), cancellationToken);
            await _human.ScrollLikeHumanAsync(page, cancellationToken: cancellationToken);

            var pageItems = await ExtractListingsOnPageAsync(page, request.SearchTerm, currentPage, cancellationToken);
            foreach (var item in pageItems)
            {
                if (listings.Count >= request.MaxItems)
                {
                    break;
                }

                if (string.IsNullOrWhiteSpace(item.Url) || !dedupe.Add(item.Url))
                {
                    continue;
                }

                listings.Add(item);
            }

            if (currentPage >= request.PagesToScrape || listings.Count >= request.MaxItems)
            {
                break;
            }

            var moved = await GoToNextPageAsync(page);
            if (!moved)
            {
                break;
            }

            currentPage++;
            await _human.RandomDelayAsync(1200, 3000, cancellationToken);
        }

        return new EbayScrapeResult
        {
            Request = request,
            Listings = listings,
            StartedAtUtc = startedAt,
            FinishedAtUtc = DateTimeOffset.UtcNow
        };
    }

    private async Task<List<EbayListing>> ExtractListingsOnPageAsync(
        IPage page,
        string searchTerm,
        int currentPage,
        CancellationToken cancellationToken)
    {
        var cards = await page.QuerySelectorAllAsync("li.s-item");
        var results = new List<EbayListing>(cards.Length);

        for (var i = 0; i < cards.Length; i++)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var card = cards[i];
            var title = await GetTextAsync(card, ".s-item__title");
            if (string.IsNullOrWhiteSpace(title) || title.Contains("Shop on eBay", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var url = await GetAttributeAsync(card, "a.s-item__link", "href");
            var priceText = await GetTextAsync(card, ".s-item__price");
            var shippingText = await GetTextAsync(card, ".s-item__shipping, .s-item__logisticsCost");
            var conditionText = await GetTextAsync(card, ".SECONDARY_INFO");
            var subtitle = await GetTextAsync(card, ".s-item__subtitle");
            var locationText = await GetTextAsync(card, ".s-item__location");
            var soldText = await GetTextAsync(card, ".s-item__hotness");
            var imageUrl = await GetAttributeAsync(card, ".s-item__image-img", "src");

            results.Add(new EbayListing
            {
                SearchTerm = searchTerm,
                Title = NormalizeText(title),
                Url = url,
                PriceText = NormalizeText(priceText),
                PriceValue = ParsePrice(priceText),
                ShippingText = NormalizeText(shippingText),
                ConditionText = NormalizeText(conditionText),
                Subtitle = NormalizeText(subtitle),
                LocationText = NormalizeText(locationText),
                SoldText = NormalizeText(soldText),
                ImageUrl = imageUrl,
                ResultsPage = currentPage,
                PositionOnPage = i + 1,
                CapturedAtUtc = DateTimeOffset.UtcNow
            });
        }

        return results;
    }

    private async Task<bool> GoToNextPageAsync(IPage page)
    {
        var nextButton = await page.QuerySelectorAsync("a[aria-label='Go to next search page'], a.pagination__next");
        if (nextButton is null)
        {
            return false;
        }

        var className = await nextButton.EvaluateFunctionAsync<string>("el => el.className || ''");
        var ariaDisabled = await nextButton.EvaluateFunctionAsync<string?>("el => el.getAttribute('aria-disabled')");
        if (className.Contains("disabled", StringComparison.OrdinalIgnoreCase) || string.Equals(ariaDisabled, "true", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        try
        {
            await Task.WhenAll(
                page.WaitForNavigationAsync(new NavigationOptions
                {
                    WaitUntil = [WaitUntilNavigation.Networkidle2],
                    Timeout = 45000
                }),
                nextButton.ClickAsync());
        }
        catch
        {
            await nextButton.ClickAsync();
            await page.WaitForSelectorAsync("li.s-item", new WaitForSelectorOptions
            {
                Timeout = 30000
            });
        }

        return true;
    }

    private async Task TryAcceptCookiesAsync(IPage page, CancellationToken cancellationToken)
    {
        var selectors = new[]
        {
            "button#gdpr-banner-accept",
            "button[aria-label='Accept all']",
            "button[data-testid='gdpr-banner-accept']"
        };

        foreach (var selector in selectors)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var button = await page.QuerySelectorAsync(selector);
            if (button is null)
            {
                continue;
            }

            await _human.RandomDelayAsync(180, 600, cancellationToken);
            await button.ClickAsync();
            await _human.RandomDelayAsync(250, 800, cancellationToken);
            return;
        }
    }

    private static async Task<string> GetTextAsync(IElementHandle parent, string selector)
    {
        var element = await parent.QuerySelectorAsync(selector);
        if (element is null)
        {
            return string.Empty;
        }

        var text = await element.EvaluateFunctionAsync<string>("el => (el.textContent || '').trim()");
        return text ?? string.Empty;
    }

    private static async Task<string> GetAttributeAsync(IElementHandle parent, string selector, string attribute)
    {
        var element = await parent.QuerySelectorAsync(selector);
        if (element is null)
        {
            return string.Empty;
        }

        var value = await element.EvaluateFunctionAsync<string>("(el, attr) => el.getAttribute(attr) || ''", attribute);
        return value ?? string.Empty;
    }

    private static decimal? ParsePrice(string rawPrice)
    {
        if (string.IsNullOrWhiteSpace(rawPrice))
        {
            return null;
        }

        var match = PriceRegex.Match(rawPrice);
        if (!match.Success)
        {
            return null;
        }

        var cleaned = match.Value.Replace(",", string.Empty);
        if (decimal.TryParse(cleaned, NumberStyles.Any, CultureInfo.InvariantCulture, out var parsed))
        {
            return parsed;
        }

        return null;
    }

    private static string NormalizeText(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        return Regex.Replace(value, "\\s+", " ").Trim();
    }
}
