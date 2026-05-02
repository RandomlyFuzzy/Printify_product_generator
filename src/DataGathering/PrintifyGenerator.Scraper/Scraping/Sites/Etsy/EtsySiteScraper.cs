using System.Globalization;
using System.Text.RegularExpressions;
using PuppeteerSharp;
using PrintifyGenerator.Scraper.Scraping.Abstractions;
using PrintifyGenerator.Scraper.Scraping.Core;
using PrintifyGenerator.Scraper.Scraping.Ebay;

namespace PrintifyGenerator.Scraper.Scraping.Sites.Etsy;

public sealed class EtsySiteScraper : ISiteScraper
{
    private static readonly Regex PriceRegex = new("\\d+[\\d,]*\\.?\\d*", RegexOptions.Compiled);

    private readonly IHumanBehaviorSimulator _human;
    private readonly DynamicScrollLoader _scrollLoader;
    private readonly Random _random;

    public EtsySiteScraper(IHumanBehaviorSimulator human, DynamicScrollLoader scrollLoader, Random random)
    {
        _human = human;
        _scrollLoader = scrollLoader;
        _random = random;
    }

    public string SiteName => "etsy";

    private sealed class EtsyDomListing
    {
        public string Url { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string PriceText { get; set; } = string.Empty;
        public string ImageUrl { get; set; } = string.Empty;
    }

    public async Task<IReadOnlyList<ScrapeListing>> ScrapeAsync(ScrapeRequest request, CancellationToken cancellationToken = default)
    {
        Console.WriteLine("Task: Initializing browser session for Etsy");
        var listings = new List<ScrapeListing>();
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

        var encodedTerm = Uri.EscapeDataString(request.SearchTerm);
        var pageUrl = $"https://www.etsy.com/search?q={encodedTerm}";

        var currentPage = 1;
        while (currentPage <= request.PagesToScrape && listings.Count < request.MaxItemsPerSite)
        {
            cancellationToken.ThrowIfCancellationRequested();
            Console.WriteLine($"Task: Scanning Etsy results page {currentPage}");

            var url = currentPage == 1
                ? pageUrl
                : $"https://www.etsy.com/search?q={encodedTerm}&page={currentPage}";

            await page.GoToAsync(url, new NavigationOptions
            {
                WaitUntil = [WaitUntilNavigation.Networkidle2],
                Timeout = 60000
            });

            await TryAcceptCookiesAsync(page, cancellationToken);
            await WaitForListingsAsync(page, cancellationToken);

            Console.WriteLine("Task: Pretending to be human - random mouse movement");
            await _human.RandomMouseMovementAsync(page, _random.Next(5, 11), cancellationToken);

            Console.WriteLine("Task: Pretending to be human - dynamic scrolling for lazy-loaded products");
            await _scrollLoader.WarmUpPageAsync(
                page,
                listingSelector: "a[href*='/listing/']",
                maxRounds: request.MaxScrollRoundsPerPage,
                stagnationLimit: request.ScrollStagnationLimit,
                cancellationToken: cancellationToken);

            Console.WriteLine("Task: Checking product reviews (listing-level signal only)");
            Console.WriteLine("Task: Getting product sale per variation (not available on listing page, skipping)");
            var pageItems = await ExtractListingsOnPageAsync(page, request.SearchTerm, currentPage, cancellationToken);
            foreach (var item in pageItems)
            {
                if (listings.Count >= request.MaxItemsPerSite)
                {
                    break;
                }

                if (string.IsNullOrWhiteSpace(item.Url) || !dedupe.Add(item.Url))
                {
                    continue;
                }

                listings.Add(item);
            }

            Console.WriteLine($"Task: Page {currentPage} extraction complete. Unique products so far: {listings.Count:N0}");

            currentPage++;
            await _human.RandomDelayAsync(900, 2200, cancellationToken);
        }

        Console.WriteLine($"Task: Etsy scrape complete for '{request.SearchTerm}'. Total unique products: {listings.Count:N0}");

        return listings;
    }

    private async Task<List<ScrapeListing>> ExtractListingsOnPageAsync(
        IPage page,
        string searchTerm,
        int currentPage,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var domListings = await page.EvaluateFunctionAsync<EtsyDomListing[]>(@"() => {
            const anchors = Array.from(document.querySelectorAll(""a[href*='/listing/']""));
            const unique = new Map();

            for (const anchor of anchors) {
                const href = (anchor.getAttribute('href') || '').trim();
                if (!href || unique.has(href)) {
                    continue;
                }

                const card = anchor.closest('[data-listing-id], li, div') || anchor;
                const titleNode = card.querySelector('h3, .v2-listing-card__title, .wt-text-caption');
                const priceNode = card.querySelector('.currency-value, .wt-text-title-01, .wt-text-caption strong');
                const imageNode = card.querySelector('img');

                const title = (titleNode?.textContent || anchor.getAttribute('aria-label') || anchor.textContent || '').trim();
                const priceText = (priceNode?.textContent || '').trim();
                const imageUrl = (imageNode?.getAttribute('src') || imageNode?.getAttribute('data-src') || '').trim();

                unique.set(href, {
                    url: href,
                    title,
                    priceText,
                    imageUrl
                });
            }

            return Array.from(unique.values());
        }");

        var results = new List<ScrapeListing>(domListings.Length);
        for (var i = 0; i < domListings.Length; i++)
        {
            var item = domListings[i];
            if (string.IsNullOrWhiteSpace(item.Url) || string.IsNullOrWhiteSpace(item.Title))
            {
                continue;
            }

            results.Add(new ScrapeListing
            {
                Site = SiteName,
                SearchTerm = searchTerm,
                Title = NormalizeText(item.Title),
                Url = item.Url,
                PriceText = NormalizeText(item.PriceText),
                PriceValue = ParsePrice(item.PriceText),
                ImageUrl = item.ImageUrl,
                ResultsPage = currentPage,
                PositionOnPage = i + 1,
                CapturedAtUtc = DateTimeOffset.UtcNow
            });
        }

        return results;
    }

    private async Task WaitForListingsAsync(IPage page, CancellationToken cancellationToken)
    {
        for (var attempt = 0; attempt < 25; attempt++)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var listingCount = await page.EvaluateFunctionAsync<int>("() => document.querySelectorAll(\"a[href*='/listing/']\").length");
            if (listingCount > 0)
            {
                return;
            }

            var title = await page.GetTitleAsync();
            if (title.Contains("Security", StringComparison.OrdinalIgnoreCase) ||
                title.Contains("Robot", StringComparison.OrdinalIgnoreCase))
            {
                Console.WriteLine("Task: Etsy anti-bot challenge detected. Listings may be unavailable for this run.");
                return;
            }

            await Task.Delay(1000, cancellationToken);
        }
    }

    private async Task TryAcceptCookiesAsync(IPage page, CancellationToken cancellationToken)
    {
        var selectors = new[]
        {
            "button[data-gdpr-single-choice-accept='true']",
            "button[aria-label='Accept']",
            "button[aria-label='Accept all']"
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
