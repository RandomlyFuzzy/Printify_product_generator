using PuppeteerSharp;

public class Humanizer
{
    private readonly Random _rnd = new();

    public async Task TypeLikeHuman(IElementHandle el, string text)
    {
        foreach (var c in text)
        {
            await el.TypeAsync(c.ToString());
            await Task.Delay(_rnd.Next(80, 180));
        }
    }

    public async Task RandomScroll(IPage page)
    {
        var rnd = new Random();

        var totalHeight = await page.EvaluateFunctionAsync<int>("() => document.body.scrollHeight");
        var viewport = await page.EvaluateFunctionAsync<int>("() => window.innerHeight");

        int current = 0;

        while (current < totalHeight)
        {
            // small "mouse wheel" step (like real scrolling)
            int step = rnd.Next(80, 220);

            current += step;

            await page.EvaluateFunctionAsync($"window.scrollBy(0, {step})");

            // variable delay = human scroll rhythm
            await Task.Delay(rnd.Next(100, 300));

            // occasional pause (reading a listing)
            if (rnd.NextDouble() < 0.15)
            {
                await Task.Delay(rnd.Next(800, 2000));
            }

            // occasional micro-adjustment upward (human correction)
            if (rnd.NextDouble() < 0.08)
            {
                await page.EvaluateFunctionAsync($"window.scrollBy(0, -{rnd.Next(20, 80)})");
                await Task.Delay(rnd.Next(200, 500));
            }

            // refresh height (infinite scroll pages grow)
            totalHeight = await page.EvaluateFunctionAsync<int>("() => document.body.scrollHeight");
        }
    }

    public async Task RandomMouse(IPage page)
    {
        await page.Mouse.MoveAsync(
            _rnd.Next(100, 1200),
            _rnd.Next(100, 800)
        );
    }
}