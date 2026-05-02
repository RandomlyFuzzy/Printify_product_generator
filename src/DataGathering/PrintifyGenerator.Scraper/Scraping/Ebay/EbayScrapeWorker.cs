namespace PrintifyGenerator.Scraper.Scraping.Ebay;

public sealed class EbayScrapeWorker
{
    private readonly EbayScraper _scraper;
    private readonly EbayScrapeFileWriter _fileWriter;

    public EbayScrapeWorker(EbayScraper scraper, EbayScrapeFileWriter fileWriter)
    {
        _scraper = scraper;
        _fileWriter = fileWriter;
    }

    public async Task RunAsync(EbayScrapeRunOptions options, CancellationToken cancellationToken = default)
    {
        var runNumber = 1;

        while (!cancellationToken.IsCancellationRequested)
        {
            var started = DateTimeOffset.UtcNow;
            Console.WriteLine($"[{started:O}] Run #{runNumber} started for term '{options.Request.SearchTerm}'.");

            try
            {
                var result = await _scraper.ScrapeAsync(options.Request, cancellationToken);
                var (jsonPath, csvPath) = await _fileWriter.WriteAsync(result, cancellationToken);

                Console.WriteLine($"[{DateTimeOffset.UtcNow:O}] Run #{runNumber} finished. Items: {result.Listings.Count:N0}.");
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
