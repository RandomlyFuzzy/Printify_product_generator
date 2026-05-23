using PrintifyGenerator.ColoringBookGenerator.Utilities;

namespace PrintifyGenerator.ColoringBookGenerator.Tests.Utilities;

public class ImageRateLimiterTests
{
    [Fact]
    public void Constructor_StartsWithFullTokens()
    {
        var limiter = new ImageRateLimiter();
        var status = limiter.GetStatus();
        Assert.Equal(100.0, status.tokens, 1);
        Assert.Equal(100.0, status.capacity);
    }

    [Fact]
    public async Task AcquireAsync_DecrementsTokens()
    {
        var limiter = new ImageRateLimiter();
        await limiter.AcquireAsync();
        var status = limiter.GetStatus();
        Assert.Equal(99.0, status.tokens, 1);
    }

    [Fact]
    public async Task AcquireAsync_MultipleCalls_DecrementsEachTime()
    {
        var limiter = new ImageRateLimiter();
        for (int i = 0; i < 5; i++)
            await limiter.AcquireAsync();
        var status = limiter.GetStatus();
        Assert.Equal(95.0, status.tokens, 1);
    }

    [Fact]
    public async Task AcquireAsync_RespectsCancellation()
    {
        var limiter = new ImageRateLimiter();
        using var cts = new CancellationTokenSource();
        cts.Cancel();
        await Assert.ThrowsAsync<OperationCanceledException>(() =>
            limiter.AcquireAsync(cts.Token));
    }

    [Fact]
    public void GetStatus_ReturnsCapacity()
    {
        var limiter = new ImageRateLimiter();
        var status = limiter.GetStatus();
        Assert.Equal(100.0, status.capacity);
    }

    [Fact]
    public void GetStatus_ReturnsRefillRate()
    {
        var limiter = new ImageRateLimiter();
        var status = limiter.GetStatus();
        Assert.Equal(100.0 / 3600.0, status.refillPerSecond, 6);
    }

    [Fact]
    public async Task AcquireAllTokens_ExhaustsToZero()
    {
        var limiter = new ImageRateLimiter();
        for (int i = 0; i < 100; i++)
            await limiter.AcquireAsync();
        var status = limiter.GetStatus();
        Assert.Equal(0.0, status.tokens, 1);
    }

    [Fact]
    public async Task Singleton_Instance_IsAccessible()
    {
        var instance = ImageRateLimiter.Instance;
        Assert.NotNull(instance);
        var status = instance.GetStatus();
        Assert.Equal(100.0, status.capacity);
    }
}
