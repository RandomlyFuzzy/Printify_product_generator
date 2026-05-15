using System.Collections.Concurrent;

namespace PrintifyGenerator.Library.Tests.Modules.Utility;

public class RoundRobinSelectorTests
{
    [Fact]
    public void Constructor_WithNullItems_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => new RoundRobinSelector<string>(null!));
    }

    [Fact]
    public void Constructor_WithEmptyItems_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() => new RoundRobinSelector<string>(Array.Empty<string>()));
    }

    [Fact]
    public void Constructor_WithSingleItem_Succeeds()
    {
        var selector = new RoundRobinSelector<string>(["only"]);
        Assert.Single(selector.Items);
    }

    [Fact]
    public void Items_ReturnsReadOnlyCopy()
    {
        var selector = new RoundRobinSelector<int>([1, 2, 3]);
        Assert.Equal([1, 2, 3], selector.Items);
    }

    [Fact]
    public void Next_WithSingleItem_AlwaysReturnsThatItem()
    {
        var selector = new RoundRobinSelector<string>(["only"]);
        for (var i = 0; i < 10; i++)
            Assert.Equal("only", selector.Next());
    }

    [Fact]
    public void Next_WithMultipleItems_CyclesThroughAll()
    {
        var selector = new RoundRobinSelector<string>(["a", "b", "c"]);
        var expected = new[] { "a", "b", "c", "a", "b", "c" };
        var actual = Enumerable.Range(0, 6).Select(_ => selector.Next()).ToArray();
        Assert.Equal(expected, actual);
    }

    [Fact]
    public void Next_IsThreadSafe_NoException()
    {
        var selector = new RoundRobinSelector<int>([1, 2, 3, 4, 5]);
        var results = new ConcurrentBag<int>();

        Parallel.For(0, 100, _ => results.Add(selector.Next()));

        Assert.Equal(100, results.Count);
    }

    [Fact]
    public void Next_WithTwoItems_Alternates()
    {
        var selector = new RoundRobinSelector<string>(["x", "y"]);
        Assert.Equal("x", selector.Next());
        Assert.Equal("y", selector.Next());
        Assert.Equal("x", selector.Next());
        Assert.Equal("y", selector.Next());
    }
}
