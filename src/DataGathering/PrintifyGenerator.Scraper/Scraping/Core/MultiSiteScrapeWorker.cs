using PrintifyGenerator.Scraper.Scraping.Abstractions;

namespace PrintifyGenerator.Scraper.Scraping.Core;

public sealed class MultiSiteScrapeWorker
{
    private readonly IReadOnlyDictionary<string, ISiteScraper> _siteScrapers;
    private readonly ScrapeFileWriter _fileWriter;

    public MultiSiteScrapeWorker(IEnumerable<ISiteScraper> siteScrapers, ScrapeFileWriter fileWriter)
    {
        _siteScrapers = siteScrapers.ToDictionary(s => s.SiteName, StringComparer.OrdinalIgnoreCase);
        _fileWriter = fileWriter;
    }

    public async Task RunAsync(ScrapeRunOptions options, CancellationToken cancellationToken = default)
    {
        var runNumber = 1;

        while (!cancellationToken.IsCancellationRequested)
        {
            var started = DateTimeOffset.UtcNow;
            Console.WriteLine($"[{started:O}] Run #{runNumber} started for term '{options.Request.SearchTerm}'.");

            try
            {
                var listings = new List<ScrapeListing>();

                foreach (var site in options.Request.Sites)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    if (!_siteScrapers.TryGetValue(site, out var scraper))
                    {
                        Console.WriteLine($"[{DateTimeOffset.UtcNow:O}] Site '{site}' has no registered scraper. Skipping.");
                        continue;
                    }

                    Console.WriteLine($"[{DateTimeOffset.UtcNow:O}] Scraping site '{site}'...");
                    var siteResults = await scraper.ScrapeAsync(options.Request, cancellationToken);
                    listings.AddRange(siteResults);
                    Console.WriteLine($"[{DateTimeOffset.UtcNow:O}] Site '{site}' produced {siteResults.Count:N0} listings.");
                }

                var result = new ScrapeResult
                {
                    Request = options.Request,
                    Listings = listings,
                    StartedAtUtc = started,
                    FinishedAtUtc = DateTimeOffset.UtcNow
                };

                var (jsonPath, csvPath) = await _fileWriter.WriteAsync(result, cancellationToken);

                Console.WriteLine($"[{DateTimeOffset.UtcNow:O}] Run #{runNumber} finished. Total items: {result.Listings.Count:N0}.");
                Console.WriteLine($"JSON: {jsonPath}");
                Console.WriteLine($"CSV:  {csvPath}");
            }
            catch (OperationCanceledException)
            {
                Console.WriteLine("Cancellation requested. Stopping scraper worker.");
                break;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[{DateTimeOffset.UtcNow:O}] Run #{runNumber} failed: {ex.Message}");
            }

            if (!options.Continuous)
            {
                break;
            }

            runNumber++;
            Console.WriteLine($"Waiting {options.IntervalMinutes} minute(s) before next run...");
            await Task.Delay(TimeSpan.FromMinutes(options.IntervalMinutes), cancellationToken);
        }
    }
}
