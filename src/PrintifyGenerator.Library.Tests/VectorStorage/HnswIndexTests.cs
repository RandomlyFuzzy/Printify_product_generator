using PrintifyGenerator.Library.VectorStorage;

namespace PrintifyGenerator.Library.Tests.VectorStorage;

public class HnswIndexTests
{
    [Fact]
    public void Count_EmptyIndex_ReturnsZero()
    {
        var index = new HnswIndex<string>(4, 10, 10);
        Assert.Equal(0, index.Count);
    }

    [Fact]
    public void Add_SingleVector_CountIsOne()
    {
        var index = new HnswIndex<string>(4, 10, 10);
        index.Add([1, 0, 0], "test");
        Assert.Equal(1, index.Count);
    }

    [Fact]
    public void Search_EmptyIndex_ReturnsEmpty()
    {
        var index = new HnswIndex<string>(4, 10, 10);
        var results = index.Search([1, 0, 0], 5);
        Assert.Empty(results);
    }

    [Fact]
    public void Search_ExactMatch_ReturnsCorrect()
    {
        var index = new HnswIndex<string>(4, 10, 10);
        index.Add([1, 0, 0], "first");
        index.Add([0, 1, 0], "second");
        index.Add([0, 0, 1], "third");

        var results = index.Search([0, 1, 0], 1);
        Assert.Single(results);
        Assert.Equal("second", results[0].metadata);
    }

    [Fact]
    public void Search_TopK_ReturnsCorrectOrder()
    {
        var index = new HnswIndex<string>(4, 10, 10);
        index.Add([0.1f, 0.9f], "close_a");
        index.Add([0.0f, 1.0f], "closest");
        index.Add([0.9f, 0.1f], "far");

        var results = index.Search([0, 1], 3);
        Assert.Equal(3, results.Count);
        Assert.Equal("closest", results[0].metadata);
        Assert.Equal("close_a", results[1].metadata);
        Assert.Equal("far", results[2].metadata);
    }

    [Fact]
    public void AddAndSearch_ManyVectors_ReturnsReasonableResults()
    {
        var index = new HnswIndex<string>(8, 50, 20);
        var rng = new Random(42);

        for (int i = 0; i < 100; i++)
        {
            var v = new float[] { (float)rng.NextDouble(), (float)rng.NextDouble() };
            index.Add(v, $"vec_{i}");
        }

        var query = new float[] { 0.5f, 0.5f };
        var results = index.Search(query, 5);

        Assert.Equal(5, results.Count);
        foreach (var (_, score) in results)
            Assert.True(score >= 0 && score <= 1);
    }

    [Fact]
    public void GetAllMetadata_Empty_ReturnsEmpty()
    {
        var index = new HnswIndex<string>(4, 10, 10);
        Assert.Empty(index.GetAllMetadata());
    }

    [Fact]
    public void GetAllMetadata_AfterAdd_ContainsAdded()
    {
        var index = new HnswIndex<string>(4, 10, 10);
        index.Add([1, 0, 0], "alpha");
        index.Add([0, 1, 0], "beta");

        var all = index.GetAllMetadata();
        Assert.Equal(2, all.Count);
        Assert.Contains("alpha", all);
        Assert.Contains("beta", all);
    }

    [Fact]
    public void SaveAndLoad_RoundTrip_PreservesData()
    {
        var path = Path.GetTempFileName();
        try
        {
            var index = new HnswIndex<string>(4, 10, 10);
            index.Add([1, 0, 0], "saved");
            index.Add([0, 1, 0], "data");
            index.Save(path);

            var loaded = HnswIndex<string>.Load(path);
            Assert.Equal(2, loaded.Count);

            var results = loaded.Search([1, 0, 0], 1);
            Assert.Single(results);
            Assert.Equal("saved", results[0].metadata);
        }
        finally
        {
            if (File.Exists(path)) File.Delete(path);
        }
    }

    [Fact]
    public void ConfigureSearch_AffectsResults()
    {
        var index = new HnswIndex<string>(4, 10, 50);
        for (int i = 0; i < 20; i++)
            index.Add([i / 20f, 1 - i / 20f], $"v{i}");

        index.ConfigureSearch(5);
        var results = index.Search([0.5f, 0.5f], 3);
        Assert.Equal(3, results.Count);
    }

    [Fact]
    public void ThreadSafety_ConcurrentAdds_NoException()
    {
        var index = new HnswIndex<string>(4, 10, 10);
        var exceptions = new List<Exception>();

        Parallel.For(0, 50, i =>
        {
            try
            {
                index.Add([i / 50f, 1 - i / 50f], $"t{i}");
            }
            catch (Exception ex)
            {
                lock (exceptions) { exceptions.Add(ex); }
            }
        });

        Assert.Empty(exceptions);
        Assert.Equal(50, index.Count);
    }
}
