using PrintifyGenerator.Library.VectorStorage;

namespace PrintifyGenerator.Library.Tests.VectorStorage;

public class VectorStoreTests
{
    [Fact]
    public void Store_And_GetRecent_ReturnsInOrder()
    {
        var store = new VectorStore(Path.GetTempFileName());
        store.Store("cats", "a fluffy cat", [1, 0, 0], 8.5f);
        Thread.Sleep(10);
        store.Store("dogs", "a happy dog", [0, 1, 0], 7.0f);

        var recent = store.GetRecent(5);
        Assert.Equal(2, recent.Count);
        Assert.Equal("dogs", recent[0].Concept);
    }

    [Fact]
    public void Store_And_GetTopByScore_ReturnsSorted()
    {
        var store = new VectorStore(Path.GetTempFileName());
        store.Store("low", "low score", [1, 0, 0], 3.0f);
        store.Store("high", "high score", [0, 1, 0], 9.5f);
        store.Store("mid", "mid score", [0, 0, 1], 6.0f);

        var top = store.GetTopByScore(2);
        Assert.Equal(2, top.Count);
        Assert.Equal("high", top[0].Concept);
        Assert.Equal("mid", top[1].Concept);
    }

    [Fact]
    public void Store_And_GetByConcept_Match()
    {
        var store = new VectorStore(Path.GetTempFileName());
        store.Store("fantasy", "dragon", [1, 0], 8f);
        store.Store("fantasy", "elf", [0, 1], 7f);
        store.Store("sci-fi", "robot", [0, 0], 6f);

        var fantasy = store.GetByConcept("fantasy");
        Assert.Equal(2, fantasy.Count);
    }

    [Fact]
    public void Store_And_GetByConcept_CaseInsensitive()
    {
        var store = new VectorStore(Path.GetTempFileName());
        store.Store("Fantasy", "dragon", [1, 0], 8f);

        var results = store.GetByConcept("fantasy");
        Assert.Single(results);
    }

    [Fact]
    public void SearchSimilar_WithEmbeddings_ReturnsRanked()
    {
        var store = new VectorStore(Path.GetTempFileName());
        store.Store("concept_a", "prompt a", [1, 0, 0, 0], 8f);
        store.Store("concept_b", "prompt b", [0, 1, 0, 0], 5f);

        var results = store.SearchSimilar([0.9f, 0.1f, 0, 0], 2);
        Assert.Equal(2, results.Count);
        Assert.Equal("prompt a", results[0].record.Prompt);
    }

    [Fact]
    public void GetAllRecords_Empty_ReturnsEmpty()
    {
        var store = new VectorStore(Path.GetTempFileName());
        Assert.Empty(store.GetAllRecords());
    }

    [Fact]
    public void SaveAndLoad_RoundTrip()
    {
        var path = Path.GetTempFileName();
        try
        {
            var store = new VectorStore(path);
            store.Store("test", "save me", [0.5f, 0.5f], 9f);
            store.Save();

            var loaded = new VectorStore(path);
            loaded.Load();

            var all = loaded.GetAllRecords();
            Assert.Single(all);
            Assert.Equal("save me", all[0].Prompt);
        }
        finally
        {
            if (File.Exists(path)) File.Delete(path);
            if (File.Exists(path + ".hnsw")) File.Delete(path + ".hnsw");
        }
    }
}
