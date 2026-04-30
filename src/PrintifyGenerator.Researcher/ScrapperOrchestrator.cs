public class ScrapeOrchestrator
{
    private readonly BrowserEngine _engine;
    private readonly EbayScraper _scraper;
    private readonly ListingRepository _repo;

    public ScrapeOrchestrator(BrowserEngine engine, EbayScraper scraper, ListingRepository repo)
    {
        _engine = engine;
        _scraper = scraper;
        _repo = repo;
    }

    public async Task RunAsync(List<string> searches)
    {
        // await _engine.InitAsync();

        // foreach (var term in searches)
        // {
        //     var page = await _engine.NewPageAsync();

        //     Console.WriteLine($"Searching: {term}");

        //     var data = await _scraper.ScrapeSearch(page, term, 3);

        //     _repo.Save(data);

        //     await _engine.HumanPause(5000, 12000);

        //     await page.CloseAsync();
        // }

        await _engine.CloseAsync();
    }
}