using PuppeteerSharp;
using PrintifyGenerator.Scraper.Scraping.Abstractions;

namespace PrintifyGenerator.Scraper.Scraping.Core;

public sealed class DynamicScrollLoader
{
    private readonly IHumanBehaviorSimulator _human;

    public DynamicScrollLoader(IHumanBehaviorSimulator human)
    {
        _human = human;
    }

    public async Task WarmUpPageAsync(
        IPage page,
        string listingSelector,
        int maxRounds,
        int stagnationLimit,
        CancellationToken cancellationToken = default)
    {
        var noGrowthRounds = 0;
        var previousCount = -1;

        for (var round = 0; round < maxRounds; round++)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var count = await page.EvaluateFunctionAsync<int>("selector => document.querySelectorAll(selector).length", listingSelector);
            if (count <= previousCount)
            {
                noGrowthRounds++;
            }
            else
            {
                noGrowthRounds = 0;
                previousCount = count;
            }

            if (noGrowthRounds >= stagnationLimit)
            {
                break;
            }

            await _human.ScrollLikeHumanAsync(page, minScrolls: 1, maxScrolls: 2, cancellationToken);
            await _human.RandomDelayAsync(300, 900, cancellationToken);
        }
    }
}
