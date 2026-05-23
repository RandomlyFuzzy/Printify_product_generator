using System.Runtime.CompilerServices;
using System.Text.Json;

namespace PrintifyGenerator.Library.VectorStorage;

public class HnswIndex<T>
{
    private readonly int _m;
    private readonly int _mMax;
    private readonly int _mMax0;
    private int _efConstruction;
    private int _efSearch;
    private readonly float _mL;

    private readonly List<float[]> _vectors = [];
    private readonly List<T> _metadata = [];
    private readonly List<List<int>[]> _graph = [];
    private int _entryPoint = -1;
    private int _maxLevel;
    private readonly Random _rng = new();

    private readonly ReaderWriterLockSlim _lock = new();

    public int Count { get; private set; }

    public List<T> GetAllMetadata()
    {
        _lock.EnterReadLock();
        try
        {
            return [.. _metadata];
        }
        finally
        {
            _lock.ExitReadLock();
        }
    }

    public HnswIndex(int m = 16, int efConstruction = 200, int efSearch = 50)
    {
        _m = m;
        _mMax = m;
        _mMax0 = 2 * m;
        _efConstruction = efConstruction;
        _efSearch = efSearch;
        _mL = 1f / MathF.Log(m);
    }

    public void ConfigureSearch(int ef) => _efSearch = ef;
    public void ConfigureConstruction(int ef) => _efConstruction = ef;

    public int Add(float[] vector, T metadata)
    {
        _lock.EnterWriteLock();
        try
        {
            return AddUnsafe(vector, metadata);
        }
        finally
        {
            _lock.ExitWriteLock();
        }
    }

    public int AddUnsafe(float[] vector, T metadata)
    {
        int id = Count;
        int level = GenerateLevel();

        _vectors.Add(vector);
        _metadata.Add(metadata);

        var connections = new List<int>[level + 1];
        for (int l = 0; l <= level; l++)
            connections[l] = [];
        _graph.Add(connections);
        Count++;

        if (_entryPoint == -1)
        {
            _entryPoint = id;
            _maxLevel = level;
            return id;
        }

        int currEntry = _entryPoint;
        float[] queryVec = vector;

        for (int l = _maxLevel; l > level; l--)
        {
            currEntry = GreedySearch(queryVec, currEntry, l);
        }

        for (int l = Math.Min(level, _maxLevel); l >= 0; l--)
        {
            var candidates = SearchLayer(queryVec, currEntry, l, _efConstruction);
            var selected = SelectNeighbors(queryVec, candidates, l == 0 ? _mMax0 : _mMax);

            foreach (var (neighborId, _) in selected)
            {
                AddConnection(id, neighborId, l);
                AddConnection(neighborId, id, l);

                ShrinkConnections(neighborId, l);
            }

            currEntry = candidates[0].nodeId;
        }

        if (level > _maxLevel)
        {
            _maxLevel = level;
            _entryPoint = id;
        }

        return id;
    }

    public List<(T metadata, float score)> Search(float[] query, int k)
    {
        _lock.EnterReadLock();
        try
        {
            if (Count == 0) return [];

            int currEntry = _entryPoint;

            for (int l = _maxLevel; l > 0; l--)
            {
                currEntry = GreedySearch(query, currEntry, l);
            }

            var candidates = SearchLayer(query, currEntry, 0, _efSearch);

            var results = new List<(int nodeId, float distance)>();
            int take = Math.Min(k, candidates.Count);
            for (int i = 0; i < take; i++)
                results.Add(candidates[i]);

            results.Sort((a, b) => a.distance.CompareTo(b.distance));

            return results.ConvertAll(r => (_metadata[r.nodeId], CosineSimilarity.Compute(query, _vectors[r.nodeId])));
        }
        finally
        {
            _lock.ExitReadLock();
        }
    }

    public void Save(string path)
    {
        _lock.EnterReadLock();
        try
        {
            var data = new
            {
                m = _m,
                mMax = _mMax,
                mMax0 = _mMax0,
                efConstruction = _efConstruction,
                efSearch = _efSearch,
                mL = _mL,
                entryPoint = _entryPoint,
                maxLevel = _maxLevel,
                count = Count,
                graph = _graph.Select(nodeLevels =>
                    nodeLevels.Select(conns => conns.ToArray()).ToArray()
                ).ToArray()
            };

            var json = JsonSerializer.Serialize(data, new JsonSerializerOptions { WriteIndented = false });
            File.WriteAllText(path, json);
        }
        finally
        {
            _lock.ExitReadLock();
        }
    }

    public static HnswIndex<T> Load(string path)
    {
        var json = File.ReadAllText(path);
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        var index = new HnswIndex<T>(
            root.GetProperty("m").GetInt32(),
            root.GetProperty("efConstruction").GetInt32(),
            root.GetProperty("efSearch").GetInt32()
        );

        index._entryPoint = root.GetProperty("entryPoint").GetInt32();
        index._maxLevel = root.GetProperty("maxLevel").GetInt32();
        index.Count = root.GetProperty("count").GetInt32();

        var graph = root.GetProperty("graph").EnumerateArray().Select(nodeLevels =>
            nodeLevels.EnumerateArray().Select(conns =>
                conns.EnumerateArray().Select(c => c.GetInt32()).ToList()
            ).ToArray() as List<int>[]
        ).ToArray();
        index._graph.AddRange(graph);

        return index;
    }

    public void PopulateVectors(IEnumerable<(float[] vector, T metadata)> items)
    {
        foreach (var (vec, meta) in items)
        {
            _vectors.Add(vec);
            _metadata.Add(meta);
        }
    }

    private int GenerateLevel()
    {
        float r = _rng.NextSingle();
        return (int)MathF.Floor(-MathF.Log(r) * _mL);
    }

    private int GreedySearch(float[] query, int entry, int level)
    {
        float bestDist = CosineSimilarity.Distance(query, _vectors[entry]);
        int best = entry;
        bool improved = true;

        while (improved)
        {
            improved = false;
            foreach (var neighbor in GetConnections(best, level))
            {
                float d = CosineSimilarity.Distance(query, _vectors[neighbor]);
                if (d < bestDist)
                {
                    bestDist = d;
                    best = neighbor;
                    improved = true;
                }
            }
        }

        return best;
    }

    private List<(int nodeId, float distance)> SearchLayer(float[] query, int entry, int level, int ef)
    {
        var visited = new HashSet<int>();
        var candidates = new SortedSet<(float dist, int nodeId)>(Comparer<(float, int)>.Create((a, b) =>
        {
            int cmp = a.Item1.CompareTo(b.Item1);
            if (cmp != 0) return cmp;
            return a.Item2.CompareTo(b.Item2);
        }));
        var results = new SortedSet<(float dist, int nodeId)>(Comparer<(float, int)>.Create((a, b) =>
        {
            int cmp = b.Item1.CompareTo(a.Item1);
            if (cmp != 0) return cmp;
            return a.Item2.CompareTo(b.Item2);
        }));

        float entryDist = CosineSimilarity.Distance(query, _vectors[entry]);
        candidates.Add((entryDist, entry));
        results.Add((entryDist, entry));
        visited.Add(entry);

        while (candidates.Count > 0)
        {
            var closest = candidates.Min;
            candidates.Remove(closest);

            var farthest = results.Min;

            if (closest.dist > farthest.dist)
                break;

            foreach (var neighbor in GetConnections(closest.nodeId, level))
            {
                if (visited.Contains(neighbor))
                    continue;
                visited.Add(neighbor);

                float d = CosineSimilarity.Distance(query, _vectors[neighbor]);
                farthest = results.Min;

                if (results.Count < ef || d < farthest.dist)
                {
                    candidates.Add((d, neighbor));
                    results.Add((d, neighbor));

                    if (results.Count > ef)
                        results.Remove(results.Min);
                }
            }
        }

        return results.OrderBy(r => r.dist).Select(r => (r.nodeId, r.dist)).ToList();
    }

    private List<(int nodeId, float distance)> SelectNeighbors(float[] query, List<(int nodeId, float distance)> candidates, int maxConnections)
    {
        candidates.Sort((a, b) => a.distance.CompareTo(b.distance));
        return candidates.Take(maxConnections).ToList();
    }

    private void AddConnection(int fromId, int toId, int level)
    {
        var conns = GetConnections(fromId, level);
        if (!conns.Contains(toId))
            conns.Add(toId);
    }

    private void ShrinkConnections(int nodeId, int level)
    {
        var conns = GetConnections(nodeId, level);
        int max = level == 0 ? _mMax0 : _mMax;
        if (conns.Count <= max) return;

        var nodeVec = _vectors[nodeId];
        conns.Sort((a, b) =>
            CosineSimilarity.Distance(nodeVec, _vectors[a])
                .CompareTo(CosineSimilarity.Distance(nodeVec, _vectors[b])));

        while (conns.Count > max)
            conns.RemoveAt(conns.Count - 1);
    }

    private List<int> GetConnections(int nodeId, int level)
    {
        return _graph[nodeId][level];
    }
}
