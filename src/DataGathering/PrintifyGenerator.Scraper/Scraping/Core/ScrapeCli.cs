namespace PrintifyGenerator.Scraper.Scraping.Core;

public static class ScrapeCli
{
    private static readonly HashSet<string> SupportedSites = new(StringComparer.OrdinalIgnoreCase)
    {
        "ebay",
        "etsy"
    };

    public static ScrapeRunOptions Parse(string[] args)
    {
        string? term = null;
        var sites = new List<string> { "ebay" };
        var pages = 1;
        var maxItemsPerSite = 120;
        var headless = true;
        var outputDirectory = "./DataSets/my_data";
        var intervalMinutes = 30;
        var continuous = true;
        var maxScrollRounds = 8;
        var scrollStagnationLimit = 3;

        for (var i = 0; i < args.Length; i++)
        {
            var token = args[i];
            switch (token)
            {
                case "--term":
                case "-t":
                    term = ReadValue(args, ref i, token);
                    break;
                case "--sites":
                case "-s":
                    sites = ParseSites(ReadValue(args, ref i, token));
                    break;
                case "--pages":
                case "-p":
                    pages = ReadIntValue(args, ref i, token, minValue: 1, maxValue: 25);
                    break;
                case "--max-items-per-site":
                case "-m":
                    maxItemsPerSite = ReadIntValue(args, ref i, token, minValue: 1, maxValue: 5000);
                    break;
                case "--output-dir":
                case "-o":
                    outputDirectory = ReadValue(args, ref i, token);
                    break;
                case "--interval-minutes":
                case "-i":
                    intervalMinutes = ReadIntValue(args, ref i, token, minValue: 1, maxValue: 1440);
                    break;
                case "--max-scroll-rounds":
                    maxScrollRounds = ReadIntValue(args, ref i, token, minValue: 1, maxValue: 60);
                    break;
                case "--scroll-stagnation-limit":
                    scrollStagnationLimit = ReadIntValue(args, ref i, token, minValue: 1, maxValue: 20);
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

        return new ScrapeRunOptions
        {
            Request = new ScrapeRequest
            {
                Sites = sites,
                SearchTerm = term,
                PagesToScrape = pages,
                MaxItemsPerSite = maxItemsPerSite,
                Headless = headless,
                OutputDirectory = outputDirectory,
                MaxScrollRoundsPerPage = maxScrollRounds,
                ScrollStagnationLimit = scrollStagnationLimit
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
            "  --term, -t                   Required search term",
            "  --sites, -s                  Comma-separated list: ebay,etsy or all (default: ebay)",
            "  --pages, -p                  Number of result pages per run (default: 1)",
            "  --max-items-per-site, -m     Maximum listing rows per site per run (default: 120)",
            "  --output-dir, -o             Output folder for JSON and CSV files (default: ./DataSets/my_data)",
            "  --interval-minutes, -i       Delay between runs in continuous mode (default: 30)",
            "  --max-scroll-rounds          Dynamic scroll attempts per page (default: 8)",
            "  --scroll-stagnation-limit    Stop scroll when item count does not grow (default: 3)",
            "  --once                       Run one cycle and exit",
            "  --continuous                 Keep running forever (default)",
            "  --headed                     Run with browser UI visible",
            "  --headless                   Force headless mode (default)",
            "  --help, -h                   Show this help text"
        );
    }

    private static List<string> ParseSites(string raw)
    {
        if (string.Equals(raw, "all", StringComparison.OrdinalIgnoreCase))
        {
            return SupportedSites.OrderBy(s => s).ToList();
        }

        var sites = raw.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(s => s.ToLowerInvariant())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (sites.Count == 0)
        {
            throw new ArgumentException("At least one site is required for --sites.");
        }

        var invalid = sites.Where(s => !SupportedSites.Contains(s)).ToList();
        if (invalid.Count > 0)
        {
            throw new ArgumentException($"Unsupported site(s): {string.Join(", ", invalid)}. Supported: {string.Join(", ", SupportedSites)}.");
        }

        return sites;
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
