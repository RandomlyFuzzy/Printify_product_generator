using PrintifyGenerator.Scraper.Scraping.Core;
using PrintifyGenerator.Scraper.Scraping.Ebay;
using PrintifyGenerator.Scraper.Scraping.Abstractions;
using PrintifyGenerator.Scraper.Scraping.Sites.Ebay;
using PrintifyGenerator.Scraper.Scraping.Sites.Etsy;

var cts = new CancellationTokenSource();
Console.CancelKeyPress += (_, e) =>
{
    e.Cancel = true;
    cts.Cancel();
};

try
{
    var options = ScrapeCli.Parse(args);

    Console.WriteLine($"Mode: {(options.Continuous ? "continuous" : "once")}");
    Console.WriteLine($"Sites: {string.Join(", ", options.Request.Sites)}");
    Console.WriteLine($"Search term: {options.Request.SearchTerm}");
    Console.WriteLine($"Pages per run: {options.Request.PagesToScrape}");
    Console.WriteLine($"Max items per site per run: {options.Request.MaxItemsPerSite}");
    Console.WriteLine($"Output dir: {options.Request.OutputDirectory}");
    Console.WriteLine($"Headless: {options.Request.Headless}");
    Console.WriteLine($"Interval minutes: {options.IntervalMinutes}");
    Console.WriteLine($"Max scroll rounds per page: {options.Request.MaxScrollRoundsPerPage}");
    Console.WriteLine($"Scroll stagnation limit: {options.Request.ScrollStagnationLimit}");

    var random = new Random();
    var behavior = new HumanBehaviorSimulator(random);
    var scrollLoader = new DynamicScrollLoader(behavior);
    var scrapers = new ISiteScraper[]
    {
        new EbaySiteScraper(behavior, scrollLoader, random),
        new EtsySiteScraper(behavior, scrollLoader, random)
    };

    var fileWriter = new ScrapeFileWriter();
    var worker = new MultiSiteScrapeWorker(scrapers, fileWriter);

    await worker.RunAsync(options, cts.Token);
}
catch (ArgumentException ex)
{
    Console.WriteLine(ex.Message);
    Environment.ExitCode = 2;
}
catch (OperationCanceledException)
{
    Console.WriteLine("Scraper stopped by cancellation.");
}
