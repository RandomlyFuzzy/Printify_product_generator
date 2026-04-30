using PuppeteerSharp;

public class HumanScroll
{
    private readonly Random _rnd = new();

    public async Task ScrollPageLikeHuman(IPage page)
    {
        var totalHeight = await page.EvaluateFunctionAsync<int>("() => document.body.scrollHeight");
        var viewportHeight = await page.EvaluateFunctionAsync<int>("() => window.innerHeight");

        int current = 0;

        while (current < totalHeight)
        {
            // small scroll step (like reading a section)
            var step = _rnd.Next(viewportHeight / 3, viewportHeight - 50);

            current += step;

            await page.EvaluateFunctionAsync($"window.scrollBy(0, {step})");

            // pause as if reading
            await Task.Delay(_rnd.Next(800, 2500));

            // occasional micro hesitation scroll (human adjustment)
            if (_rnd.NextDouble() < 0.2)
            {
                await page.EvaluateFunctionAsync("window.scrollBy(0, -Math.random() * 80)");
                await Task.Delay(_rnd.Next(300, 900));
            }
        }
    }
}