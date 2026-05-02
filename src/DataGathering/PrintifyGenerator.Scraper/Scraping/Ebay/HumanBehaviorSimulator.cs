using PuppeteerSharp;
using PuppeteerSharp.Input;
using PrintifyGenerator.Scraper.Scraping.Abstractions;

namespace PrintifyGenerator.Scraper.Scraping.Ebay;

public sealed class HumanBehaviorSimulator : IHumanBehaviorSimulator
{
    private readonly Random _random;

    public HumanBehaviorSimulator(Random random)
    {
        _random = random;
    }

    public Task RandomDelayAsync(int minMs, int maxMs, CancellationToken cancellationToken = default)
    {
        var delay = _random.Next(minMs, maxMs + 1);
        return Task.Delay(delay, cancellationToken);
    }

    public async Task TypeLikeHumanAsync(IPage page, string selector, string text, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        await page.FocusAsync(selector);
        await RandomDelayAsync(150, 500, cancellationToken);

        foreach (var ch in text)
        {
            cancellationToken.ThrowIfCancellationRequested();
            await page.Keyboard.TypeAsync(ch.ToString(), new TypeOptions
            {
                Delay = _random.Next(60, 170)
            });

            if (_random.NextDouble() < 0.07)
            {
                await RandomDelayAsync(120, 300, cancellationToken);
            }
        }
    }

    public async Task RandomMouseMovementAsync(IPage page, int steps = 8, CancellationToken cancellationToken = default)
    {
        var viewport = page.Viewport;
        var width = viewport?.Width ?? 1280;
        var height = viewport?.Height ?? 900;

        for (var i = 0; i < steps; i++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var x = _random.Next(20, Math.Max(21, width - 20));
            var y = _random.Next(20, Math.Max(21, height - 20));
            await page.Mouse.MoveAsync(x, y, new MoveOptions
            {
                Steps = _random.Next(3, 12)
            });
            await RandomDelayAsync(30, 180, cancellationToken);
        }
    }

    public async Task ScrollLikeHumanAsync(IPage page, int minScrolls = 3, int maxScrolls = 7, CancellationToken cancellationToken = default)
    {
        var totalScrolls = _random.Next(minScrolls, maxScrolls + 1);

        for (var i = 0; i < totalScrolls; i++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var delta = _random.Next(350, 900);
            await page.EvaluateFunctionAsync("value => window.scrollBy(0, value)", delta);
            await RandomDelayAsync(350, 1200, cancellationToken);
        }

        if (_random.NextDouble() < 0.20)
        {
            var reverse = _random.Next(100, 350);
            await page.EvaluateFunctionAsync("value => window.scrollBy(0, -value)", reverse);
            await RandomDelayAsync(200, 500, cancellationToken);
        }
    }
}
