using PuppeteerSharp;

public class BrowserEngine
{
    private  IBrowser _browser {get; init;}
    private readonly Random _rnd = new();

    public BrowserEngine()
    {
        new BrowserFetcher().DownloadAsync().GetAwaiter().GetResult();

        _browser = Puppeteer.LaunchAsync(new LaunchOptions
        {
            Headless = false,
            Args = new[]
            {
                "--no-sandbox",
                "--disable-setuid-sandbox",
                "--disable-blink-features=AutomationControlled",
                "--start-maximized"
            }
        }).GetAwaiter().GetResult();

    }


    public async Task<IPage> NewPageAsync()
    {
        var page = await _browser.NewPageAsync();

        await page.SetUserAgentAsync(
            "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 Chrome/120 Safari/537.36"
        );

        await page.SetViewportAsync(new ViewPortOptions
        {
            Width = 1366,
            Height = 768
        });

        return page;
    }

    public async Task HumanPause(int min = 800, int max = 3000)
    {
        await Task.Delay(_rnd.Next(min, max));
    }

    public async Task CloseAsync() => await _browser.CloseAsync();
}