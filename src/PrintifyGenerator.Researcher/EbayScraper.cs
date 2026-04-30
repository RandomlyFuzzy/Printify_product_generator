using PuppeteerSharp;

public class EbayScraper
{
    private readonly Humanizer _human;
    private readonly BrowserEngine _engine;

    public EbayScraper(Humanizer human, BrowserEngine engine)
    {
        _human = human;
        _engine = engine;
    }

    // public async Task<List<EbayListing>> ScrapeSearch(IPage page, string term, int pages = 3)
    // {
    //     var results = new List<EbayListing>();

    //     await page.GoToAsync($"https://www.ebay.com/sch/i.html?_nkw={Uri.EscapeDataString(term)}");

    //     for (int i = 0; i < pages; i++)
    //     {


    //         var items = await page.QuerySelectorAllAsync("li.s-item");

    //         foreach (var item in items)
    //         {
    //             try
    //             {
    //                 var urlEl = await item.QuerySelectorAsync("a.s-item__link");
    //                 var nameEl = await item.QuerySelectorAsync("h3");
    //                 var priceEl = await item.QuerySelectorAsync(".s-item__price");
    //                 var shipEl = await item.QuerySelectorAsync(".s-item__shipping, .s-item__logisticsCost");
    //                 var imgEl = await item.QuerySelectorAsync("img");

    //                 var url = urlEl != null
    //                     ? await page.EvaluateFunctionAsync<string>("e => e.href", urlEl)
    //                     : "";

    //                 results.Add(new EbayListing
    //                 {
    //                     SearchTerm = term,
    //                     Url = url,
    //                     Name = nameEl != null ? await page.EvaluateFunctionAsync<string>("e => e.innerText", nameEl) : "",
    //                     Price = priceEl != null ? await page.EvaluateFunctionAsync<string>("e => e.innerText", priceEl) : "",
    //                     Shipping = shipEl != null ? await page.EvaluateFunctionAsync<string>("e => e.innerText", shipEl) : "",
    //                     Image = imgEl != null ? await page.EvaluateFunctionAsync<string>("e => e.src", imgEl) : "",
    //                     Sponsored = await item.EvaluateFunctionAsync<bool>("e => e.innerText.includes('Sponsored')"),
    //                     ScrapedAt = DateTime.UtcNow
    //                 });
    //             }
    //             catch { }
    //         }
    //         await _human.RandomScroll(page);
    //         await _engine.HumanPause();

    //         var next = await page.QuerySelectorAsync(".pagination__next");
    //         if (next == null) break;

    //         await next.ClickAsync();
    //         await _engine.HumanPause(2000, 5000);
    //     }

    //     return results;
    // }
}