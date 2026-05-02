using System.Globalization;
using System.Text.RegularExpressions;
using System.Text.Json;
using PuppeteerSharp;
using PrintifyGenerator.Scraper.Scraping.Abstractions;
using PrintifyGenerator.Scraper.Scraping.Core;
using PrintifyGenerator.Scraper.Scraping.Ebay;

namespace PrintifyGenerator.Scraper.Scraping.Sites.Ebay;

public sealed partial class EbaySiteScraper : ISiteScraper
{
    private static readonly Regex PriceRegex = new("\\d+[\\d,]*\\.?\\d*", RegexOptions.Compiled);

    private readonly IHumanBehaviorSimulator _human;
    private readonly DynamicScrollLoader _scrollLoader;
    private readonly Random _random;

    public EbaySiteScraper(IHumanBehaviorSimulator human, DynamicScrollLoader scrollLoader, Random random)
    {
        _human = human;
        _scrollLoader = scrollLoader;
        _random = random;
    }

    public string SiteName => "ebay";

    public async Task<IReadOnlyList<ScrapeListing>> ScrapeAsync(ScrapeRequest request, CancellationToken cancellationToken = default)
    {
        Console.WriteLine($"Task: Initializing Puppeteer-based browser session for eBay (Headless: {request.Headless})");
        var listings = new List<ScrapeListing>();
        var dedupe = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        try
        {
            await new BrowserFetcher().DownloadAsync();

            var launchOptions = new LaunchOptions
            {
                Headless = request.Headless,
                Args =
                [
                    "--no-sandbox",
                    "--disable-blink-features=AutomationControlled",
                    "--disable-dev-shm-usage",
                    "--disable-features=IsolateOrigins,site-per-process",
                    "--disable-site-isolation-trials"
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

            Console.WriteLine("Task: Handling cookie consent banner");
            await TryAcceptCookiesAsync(page, cancellationToken);

            Console.WriteLine("Task: Simulating human behavior - random mouse movements");
            await _human.RandomMouseMovementAsync(page, cancellationToken: cancellationToken);

            Console.WriteLine($"Task: Typing search term '{request.SearchTerm}' with human-like delays");
            await _human.TypeLikeHumanAsync(page, "input[aria-label='Search for anything']", request.SearchTerm, cancellationToken);
            await _human.RandomDelayAsync(150, 700, cancellationToken);
            await page.Keyboard.PressAsync("Enter");

            await WaitForListingsAsync(page, cancellationToken);

            var currentPage = 1;
            while (currentPage <= request.PagesToScrape && listings.Count < request.MaxItemsPerSite)
            {
                cancellationToken.ThrowIfCancellationRequested();
                Console.WriteLine($"Task: Scanning eBay results page {currentPage}");

                await _human.RandomMouseMovementAsync(page, _random.Next(4, 10), cancellationToken);
                await _human.ScrollLikeHumanAsync(page, cancellationToken: cancellationToken);

                Console.WriteLine("Task: Loading lazy-loaded products with human-like scrolling");
                await _scrollLoader.WarmUpPageAsync(
                    page,
                    listingSelector: ".s-card",
                    maxRounds: request.MaxScrollRoundsPerPage,
                    stagnationLimit: request.ScrollStagnationLimit,
                    cancellationToken: cancellationToken);

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
                    Console.WriteLine($"Task: Adding listing - {item.Title[..Math.Min(50, item.Title.Length)]}...");
                    listings.Add(item);
                }

                Console.WriteLine($"Task: Page {currentPage} complete. Total unique products: {listings.Count:N0}");

                if (currentPage >= request.PagesToScrape || listings.Count >= request.MaxItemsPerSite)
                {
                    break;
                }

                Console.WriteLine("Task: Navigating to next page with human-like delay");
                var moved = await GoToNextPageAsync(page);
                if (!moved)
                {
                    Console.WriteLine("Task: No more pages available");
                    break;
                }

                currentPage++;
                await _human.RandomDelayAsync(1200, 3000, cancellationToken);
            }

            await browser.CloseAsync();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Task: Browser scraping failed ({ex.Message}).");
        }

        Console.WriteLine($"Task: eBay scrape complete for '{request.SearchTerm}'. Total unique products: {listings.Count:N0}");
        return listings;
    }

    private async Task<List<ScrapeListing>> ExtractListingsOnPageAsync(
        IPage page,
        string searchTerm,
        int currentPage,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var rawJson = await page.EvaluateFunctionAsync<string>(@"() => {
            const out = [];
            const seen = new Set();
            const itemIdRegex = /\/itm\/(?:[^/?#]+\/)?(\d{9,13})/;

            const cards = Array.from(document.querySelectorAll('.s-card, li.s-item'));

            for (const card of cards) {
                if (!card.querySelector('svg')) continue;
                const link = card.querySelector('a[href*=""/itm/""]');
                if (!link) continue;

                const href = link.href || link.getAttribute('href') || '';
                if (!href) continue;

                const idMatch = href.match(itemIdRegex);
                if (!idMatch) continue;

                const productId = idMatch[1];
                if (!productId || seen.has(productId)) continue;
                seen.add(productId);

                const img = card.querySelector('img');
                const imageUrl = img?.src || img?.getAttribute('data-src') || '';

                let title = '';
                const titleEl = card.querySelector('.s-item__title, span[role=""heading""]');
                if (titleEl) title = (titleEl.textContent || '').replace(/[\u2063\u200B]/g, '').replace(/\s+/g, ' ').trim();

                if (!title) continue;

                const priceEl = card.querySelector('.s-item__price, .s-card__price');
                const priceText = (priceEl?.innerText || '').replace(/[\u2063\u200B]/g, '').replace(/\s+/g, ' ').trim();

                const subtitleEl = card.querySelector('.s-item__subtitle, .s-card__subtitle');
                const subtitle = (subtitleEl?.textContent || '').replace(/[\u2063\u200B]/g, '').replace(/\s+/g, ' ').trim();

                const shippingEl = card.querySelector('.s-item__shipping, .s-card__shipping');
                const shippingText = (shippingEl?.innerText || '').replace(/[\u2063\u200B]/g, '').replace(/\s+/g, ' ').trim();

                const locationEl = card.querySelector('.s-item__location, .s-card__location');
                const locationText = (locationEl?.textContent || '').replace(/[\u2063\u200B]/g, '').replace(/\s+/g, ' ').trim();

                const conditionEl = card.querySelector('.s-item__condition, .s-card__condition');
                const conditionText = (conditionEl?.textContent || '').replace(/[\u2063\u200B]/g, '').replace(/\s+/g, ' ').trim();

                const isAuction = card.querySelector('.s-item__bidStatus') !== null;
                const isPromotion = card.querySelector('.s-item__promoted') !== null;

                out.push({
                    ProductId: productId,
                    Title: title,
                    Url: href,
                    PriceText: priceText,
                    ShippingText: shippingText,
                    ConditionText: conditionText,
                    Subtitle: subtitle,
                    LocationText: locationText,
                    ImageUrl: imageUrl,
                    IsAuction: isAuction,
                    IsPromotion: isPromotion
                });

                if (out.length >= 500) break;
            }

            return JSON.stringify(out);
        }");

        var extracted = JsonSerializer.Deserialize<List<EbayExtractedListing>>(
            rawJson,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? [];
        var results = new List<ScrapeListing>(extracted.Count);

        for (var i = 0; i < extracted.Count; i++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var item = extracted[i];

            results.Add(new ScrapeListing
            {
                ProductId = item.ProductId,
                Site = SiteName,
                SearchTerm = searchTerm,
                Title = NormalizeText(item.Title),
                Url = item.Url,
                PriceText = NormalizeText(item.PriceText),
                PriceValue = ParsePrice(item.PriceText),
                ShippingText = NormalizeText(item.ShippingText),
                ConditionText = NormalizeText(item.ConditionText),
                Subtitle = NormalizeText(item.Subtitle),
                LocationText = NormalizeText(item.LocationText),
                ImageUrl = NormalizeText(item.ImageUrl),
                IsAuction = item.IsAuction,
                IsPromotion = item.IsPromotion,
                ResultsPage = currentPage,
                PositionOnPage = i + 1,
                CapturedAtUtc = DateTimeOffset.UtcNow
            });
        }

        return results;
    }

    private static async Task WaitForListingsAsync(IPage page, CancellationToken cancellationToken)
    {
        var selectors = new[]
        {
            ".s-card",
            "a[href*='/itm/']",
            "li.s-item"
        };

        for (var attempt = 0; attempt < 30; attempt++)
        {
            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                foreach (var selector in selectors)
                {
                    var count = await page.EvaluateFunctionAsync<int>("sel => document.querySelectorAll(sel).length", selector);
                    if (count > 0)
                    {
                        return;
                    }
                }
            }
            catch
            {
            }

            await Task.Delay(1000, cancellationToken);
        }

        throw new TimeoutException("Could not detect eBay listing cards after search.");
    }

    private static async Task<bool> GoToNextPageAsync(IPage page)
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
            await page.WaitForSelectorAsync(".s-card, a[href*='/itm/']", new WaitForSelectorOptions { Timeout = 30000 });
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

    private sealed class EbayExtractedListing
    {
        public string ProductId { get; set; } = string.Empty;
        public string Url { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string PriceText { get; set; } = string.Empty;
        public string ShippingText { get; set; } = string.Empty;
        public string ConditionText { get; set; } = string.Empty;
        public string Subtitle { get; set; } = string.Empty;
        public string LocationText { get; set; } = string.Empty;
        public string ImageUrl { get; set; } = string.Empty;
        public bool IsAuction { get; set; }
        public bool IsPromotion { get; set; }
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

        // Remove eBay's obfuscation character (U+2063) and other invisible characters
        value = Regex.Replace(value, "[\u2063\u200B\u200C\u200D]", "");
        return Regex.Replace(value, "\\s+", " ").Trim();
    }
}
