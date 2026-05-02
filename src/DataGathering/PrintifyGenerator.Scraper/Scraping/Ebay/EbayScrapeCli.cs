namespace PrintifyGenerator.Scraper.Scraping.Ebay;

public static class EbayScrapeCli
{
    public static EbayScrapeRunOptions Parse(string[] args)
    {
        string? term = null;
        var pages = 1;
        var maxItems = 120;
        var headless = true;
        var outputDirectory = "./DataSets/RawData";
        var intervalMinutes = 30;
        var continuous = true;

        for (var i = 0; i < args.Length; i++)
        {
            var token = args[i];
            switch (token)
            {
                case "--term":
                case "-t":
                    term = ReadValue(args, ref i, token);
                    break;
                case "--pages":
                case "-p":
                    pages = ReadIntValue(args, ref i, token, minValue: 1, maxValue: 25);
                    break;
                case "--max-items":
                case "-m":
                    maxItems = ReadIntValue(args, ref i, token, minValue: 1, maxValue: 2000);
                    break;
                case "--output-dir":
                case "-o":
                    outputDirectory = ReadValue(args, ref i, token);
                    break;
                case "--interval-minutes":
                case "-i":
                    intervalMinutes = ReadIntValue(args, ref i, token, minValue: 1, maxValue: 1440);
                    break;
                case "--once":
                    continuous = false;
                    break;
                case "--continuous":
                    continuous = true;
                    break;
                case "--headed":
                    headless = false;
                    break;
                case "--headless":
                    headless = true;
                    break;
                case "--help":
                case "-h":
                    throw new ArgumentException(GetHelpText());
            }
        }

        if (string.IsNullOrWhiteSpace(term))
        {
            throw new ArgumentException("Missing required --term value.\n\n" + GetHelpText());
        }

        return new EbayScrapeRunOptions
        {
            Request = new EbayScrapeRequest
            {
                SearchTerm = term,
                PagesToScrape = pages,
                MaxItems = maxItems,
                Headless = headless,
                OutputDirectory = outputDirectory
            },
            IntervalMinutes = intervalMinutes,
            Continuous = continuous
        };
    }

    public static string GetHelpText()
    {
        return string.Join(Environment.NewLine,
            "Usage:",
            "  dotnet run --project src/DataGathering/PrintifyGenerator.Scraper -- --term \"vintage hoodie\" [options]",
            string.Empty,
            "Options:",
            "  --term, -t              Required search term to query on eBay",
            "  --pages, -p             Number of result pages to scrape per run (default: 1, max: 25)",
            "  --max-items, -m         Maximum listing rows to keep per run (default: 120, max: 2000)",
            "  --output-dir, -o        Output folder for JSON and CSV files (default: ./DataSets/RawData)",
            "  --interval-minutes, -i  Delay between runs in continuous mode (default: 30)",
            "  --once                  Run one scrape and exit",
            "  --continuous            Keep running forever (default)",
            "  --headed                Run with browser UI visible",
            "  --headless              Force headless mode (default)",
            "  --help, -h              Show this help text"
        );
    }

    private static string ReadValue(string[] args, ref int index, string flag)
    {
        if (index + 1 >= args.Length)
        {
            throw new ArgumentException($"Missing value for {flag}.");
        }

        index++;
        return args[index];
    }

    private static int ReadIntValue(string[] args, ref int index, string flag, int minValue, int maxValue)
    {
        var raw = ReadValue(args, ref index, flag);
        if (!int.TryParse(raw, out var parsed) || parsed < minValue || parsed > maxValue)
        {
            throw new ArgumentException($"Invalid value '{raw}' for {flag}. Expected integer in range {minValue}..{maxValue}.");
        }

        return parsed;
    }
}
