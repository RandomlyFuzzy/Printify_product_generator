using PuppeteerSharp;

namespace PrintifyGenerator.Scraper.Scraping.Abstractions;

public interface IHumanBehaviorSimulator
{
    Task RandomDelayAsync(int minMs, int maxMs, CancellationToken cancellationToken = default);
    Task TypeLikeHumanAsync(IPage page, string selector, string text, CancellationToken cancellationToken = default);
    Task RandomMouseMovementAsync(IPage page, int steps = 8, CancellationToken cancellationToken = default);
    Task ScrollLikeHumanAsync(IPage page, int minScrolls = 3, int maxScrolls = 7, CancellationToken cancellationToken = default);
}
