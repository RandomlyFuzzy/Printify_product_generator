using System.Text;
using System.Text.Json;

namespace PrintifyGenerator.Library.VectorStorage;

public class VectorStore
{
    private HnswIndex<VectorRecord> _index;
    private readonly string _persistencePath;
    private readonly string _baseName;
    private readonly List<VectorRecord> _records = [];
    private readonly int _m;
    private readonly int _efConstruction;
    private readonly int _efSearch;

    public int Count => _index.Count;

    public long MaxFileSizeBytes { get; set; } = 30 * 1024 * 1024;
    public int MaxLoadRecords { get; set; } = 50000;

    private string ShardPath(int index) =>
        Path.Combine(Path.GetDirectoryName(_persistencePath) ?? ".",
            $"{_baseName}_{index}.json");

    public VectorStore(string persistencePath = "vector_store.json", int m = 16, int efConstruction = 200, int efSearch = 50)
    {
        _persistencePath = persistencePath;
        _baseName = Path.GetFileNameWithoutExtension(persistencePath);
        _m = m;
        _efConstruction = efConstruction;
        _efSearch = efSearch;
        _index = new HnswIndex<VectorRecord>(m, efConstruction, efSearch);
    }

    public void RebuildIndex()
    {
        _index = new HnswIndex<VectorRecord>(_m, _efConstruction, _efSearch);
        int total = _records.Count(r => r.Embedding.Length > 0);
        if (total == 0) return;
        int done = 0;
        foreach (var r in _records)
        {
            if (r.Embedding.Length == 0) continue;
            _index.AddUnsafe(r.Embedding, r);
            done++;
            if (done % 500 == 0 || done == total)
            {
                Console.Error.WriteLine($"  [RebuildIndex] {done}/{total}");
                Console.Error.Flush();
            }
        }
    }

    public void Store(string concept, string prompt, float[] embedding, float score, string source = "generation")
    {
        var record = new VectorRecord
        {
            Embedding = embedding,
            Concept = concept,
            Prompt = prompt,
            Score = score,
            Source = source,
            Timestamp = DateTime.UtcNow
        };

        _index.Add(embedding, record);
        _records.Add(record);
    }

    public List<(VectorRecord record, float similarity)> SearchSimilar(float[] queryEmbedding, int topK = 10)
    {
        var results = _index.Search(queryEmbedding, topK);
        return results.ConvertAll(r => (r.metadata, r.score));
    }

    public List<VectorRecord> SearchByText(string concept, int topK = 10)
    {
        return _records
            .Where(r => r.Concept.Contains(concept, StringComparison.OrdinalIgnoreCase)
                     || r.Prompt.Contains(concept, StringComparison.OrdinalIgnoreCase))
            .OrderByDescending(r => r.Score)
            .Take(topK)
            .ToList();
    }

    public List<VectorRecord> GetAllRecords() => [.. _records];

    public List<VectorRecord> GetTopByScore(int count = 10)
    {
        return [.. _records.OrderByDescending(r => r.Score).Take(count)];
    }

    public List<VectorRecord> GetRecent(int count = 10)
    {
        return [.. _records.OrderByDescending(r => r.Timestamp).Take(count)];
    }

    public List<VectorRecord> GetByConcept(string concept)
    {
        return _records.Where(r =>
            r.Concept.Equals(concept, StringComparison.OrdinalIgnoreCase)
        ).ToList();
    }

    public void Save()
    {
        var opts = new JsonSerializerOptions { WriteIndented = false };
        int shardIndex = 0;
        int pos = 0;

        // Quick path: everything fits in one shard
        var allJson = JsonSerializer.Serialize(new { records = _records }, opts);
        if (Encoding.UTF8.GetByteCount(allJson) <= MaxFileSizeBytes)
        {
            File.WriteAllText(ShardPath(0), allJson);
            shardIndex = 1;
        }
        else
        {
            while (pos < _records.Count)
            {
                int lo = pos, hi = _records.Count;
                while (lo < hi)
                {
                    int mid = lo + (hi - lo + 1) / 2;
                    var batch = _records.Skip(pos).Take(mid - pos).ToList();
                    var testJson = JsonSerializer.Serialize(new { records = batch }, opts);
                    if (Encoding.UTF8.GetByteCount(testJson) <= MaxFileSizeBytes)
                        lo = mid;
                    else
                        hi = mid - 1;
                }

                var finalBatch = _records.Skip(pos).Take(lo - pos).ToList();
                File.WriteAllText(ShardPath(shardIndex),
                    JsonSerializer.Serialize(new { records = finalBatch }, opts));
                shardIndex++;
                pos = lo;
            }
        }

        // Remove stale shards (any with index >= shardIndex)
        while (File.Exists(ShardPath(shardIndex)))
        {
            File.Delete(ShardPath(shardIndex));
            shardIndex++;
        }

        // Remove old single-file if present
        if (File.Exists(_persistencePath))
            File.Delete(_persistencePath);

        _index.Save(Path.ChangeExtension(_persistencePath, ".hnsw"));
    }

    public void Load()
    {
        _records.Clear();
        _index = new HnswIndex<VectorRecord>(_m, _efConstruction, _efSearch);

        // Count total shards first so we can pre-allocate
        int shardCount = 0;
        while (File.Exists(ShardPath(shardCount))) shardCount++;

        bool hasShards = shardCount > 0;
        int totalEstimate = 0;

        if (hasShards)
        {
            // Sample first shard to estimate per-shard record count
            var firstJson = File.ReadAllText(ShardPath(0));
            var firstData = JsonSerializer.Deserialize<RecordsContainer>(firstJson);
            int perShard = firstData?.records?.Count ?? 0;
            totalEstimate = perShard * shardCount;
            if (totalEstimate > MaxLoadRecords)
            {
                Console.Error.WriteLine($"Warning: {totalEstimate} records exceeds MaxLoadRecords ({MaxLoadRecords}). Loading top {MaxLoadRecords} by score.");
            }
            int capacity = Math.Min(totalEstimate, MaxLoadRecords);
            if (capacity > 0) _records.Capacity = capacity;

            // Collect all records, score-sorted, capped
            var all = new List<VectorRecord>(capacity);
            all.AddRange(firstData?.records ?? []);

            for (int i = 1; i < shardCount; i++)
            {
                var json = File.ReadAllText(ShardPath(i));
                var data = JsonSerializer.Deserialize<RecordsContainer>(json);
                if (data?.records != null)
                    all.AddRange(data.records);
                if (all.Count >= MaxLoadRecords) break;
            }

            if (all.Count > MaxLoadRecords)
            {
                all = [.. all.OrderByDescending(r => r.Score).ThenByDescending(r => r.Timestamp).Take(MaxLoadRecords)];
            }

            _records.AddRange(all);
        }

        // Fallback to legacy single-file
        if (!hasShards && File.Exists(_persistencePath))
        {
            var json = File.ReadAllText(_persistencePath);
            var data = JsonSerializer.Deserialize<RecordsContainer>(json);
            if (data?.records != null)
            {
                if (data.records.Count > MaxLoadRecords)
                {
                    Console.Error.WriteLine($"Warning: {data.records.Count} records exceeds MaxLoadRecords ({MaxLoadRecords}). Loading top {MaxLoadRecords} by score.");
                    _records.AddRange([.. data.records.OrderByDescending(r => r.Score).ThenByDescending(r => r.Timestamp).Take(MaxLoadRecords)]);
                }
                else
                {
                    _records.Capacity = data.records.Count;
                    _records.AddRange(data.records);
                }
            }
        }

        // Load HNSW graph from file (fast, ~1MB), populate vectors from records
        var hnswPath = Path.ChangeExtension(_persistencePath, ".hnsw");
        if (File.Exists(hnswPath))
        {
            try
            {
                _index = HnswIndex<VectorRecord>.Load(hnswPath);
                _index.PopulateVectors(
                    _records.Where(r => r.Embedding.Length > 0)
                             .Select(r => (r.Embedding, r)));
            }
            catch
            {
                RebuildIndex();
            }
        }
        else
        {
            RebuildIndex();
        }
    }

    public int BatchEmbedExistingData(params string[] dataDirs)
    {
        int count = 0;
        var visited = new HashSet<string>(_records.Select(r => r.Prompt.GetHashCode().ToString()));

        foreach (var dataDir in dataDirs)
        {
            if (!Directory.Exists(dataDir)) continue;

            if (dataDir.Contains("output") || Directory.GetFiles(dataDir, "job_manifest.json").Any())
            {
                count += ScanOutputDirectory(dataDir, visited);
            }

            if (dataDir.Contains("Checking") || Directory.GetFiles(dataDir, "phase_1.json").Any())
            {
                count += ScanCheckingDirectory(dataDir, visited);
            }
        }

        return count;
    }

    public List<string> ScanAllDataSources()
    {
        var sources = new List<string>();
        var root = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", ".."));

        var outputDir = Path.Combine(root, "output");
        if (Directory.Exists(outputDir)) sources.Add(outputDir);

        var checkingDir = Path.Combine(root, "src", "data", "Checking");
        if (Directory.Exists(checkingDir)) sources.Add(checkingDir);

        return sources;
    }

    private int ScanOutputDirectory(string dir, HashSet<string> visited)
    {
        int count = 0;
        var dirs = Directory.GetDirectories(dir);

        foreach (var subdir in dirs)
        {
            var manifestPath = Path.Combine(subdir, "job_manifest.json");
            var promptsPath = Path.Combine(subdir, "prompts.json");

            if (!File.Exists(manifestPath) || !File.Exists(promptsPath))
                continue;

            try
            {
                var manifest = JsonSerializer.Deserialize<JobManifest>(File.ReadAllText(manifestPath));
                var prompts = JsonSerializer.Deserialize<List<PromptEntry>>(File.ReadAllText(promptsPath));
                if (manifest == null || prompts == null) continue;

                string title = manifest.Title ?? Path.GetFileName(subdir);
                string theme = manifest.Theme ?? "";

                var imagePrompts = prompts
                    .Where(p => p.Key != null && p.Key.Contains("prompt", StringComparison.OrdinalIgnoreCase))
                    .Select(p => p.Prompt ?? "")
                    .Where(p => !string.IsNullOrWhiteSpace(p))
                    .Distinct()
                    .ToList();

                foreach (var prompt in imagePrompts)
                {
                    if (!visited.Add(prompt.GetHashCode().ToString())) continue;

                    _records.Add(new VectorRecord
                    {
                        Embedding = [],
                        Concept = theme,
                        Prompt = prompt,
                        Score = 0,
                        Source = title,
                        Timestamp = DateTime.UtcNow
                    });
                    count++;
                }
            }
            catch { }
        }

        return count;
    }

    private int ScanCheckingDirectory(string dir, HashSet<string> visited)
    {
        int count = 0;

        try
        {
            foreach (var monthDir in Directory.GetDirectories(dir))
            {
                foreach (var dayDir in Directory.GetDirectories(monthDir))
                {
                    foreach (var bundleDir in Directory.GetDirectories(dayDir))
                    {
                        var phase1Path = Path.Combine(bundleDir, "phase_1.json");
                        if (!File.Exists(phase1Path)) continue;

                        try
                        {
                            var promptData = JsonSerializer.Deserialize<Phase1Prompt>(File.ReadAllText(phase1Path));
                            if (promptData == null || string.IsNullOrWhiteSpace(promptData.positive))
                                continue;

                            var promptKey = promptData.positive.GetHashCode().ToString();
                            if (!visited.Add(promptKey)) continue;

                            _records.Add(new VectorRecord
                            {
                                Embedding = [],
                                Concept = Path.GetFileName(bundleDir),
                                Prompt = promptData.positive,
                                Score = 0,
                                Source = $"Checking/{Path.GetFileName(monthDir)}/{Path.GetFileName(dayDir)}",
                                Timestamp = DateTime.UtcNow
                            });
                            count++;
                        }
                        catch { }
                    }
                }
            }
        }
        catch { }

        return count;
    }

    public async Task<int> BatchEmbedWithOllamaAsync(string[] dataDirs, OllamaClient ollama, string model)
    {
        int count = 0;
        var visited = new HashSet<string>(_records.Select(r => r.Prompt.GetHashCode().ToString()));

        foreach (var dataDir in dataDirs)
        {
            if (!Directory.Exists(dataDir)) continue;

            if (Directory.GetFiles(dataDir, "job_manifest.json", SearchOption.AllDirectories).Any())
            {
                count += await ScanOutputWithEmbeddingsAsync(dataDir, ollama, model, visited);
            }

            if (dataDir.Contains("Checking") || Directory.GetFiles(dataDir, "phase_1.json", SearchOption.AllDirectories).Any())
            {
                count += await ScanCheckingWithEmbeddingsAsync(dataDir, ollama, model, visited);
            }
        }

        return count;
    }

    private async Task<int> ScanOutputWithEmbeddingsAsync(string rootDir, OllamaClient ollama, string model, HashSet<string> visited)
    {
        int count = 0;

        foreach (var dir in Directory.GetDirectories(rootDir))
        {
            var manifestPath = Path.Combine(dir, "job_manifest.json");
            var promptsPath = Path.Combine(dir, "prompts.json");

            if (!File.Exists(manifestPath) || !File.Exists(promptsPath)) continue;

            try
            {
                var manifest = JsonSerializer.Deserialize<JobManifest>(File.ReadAllText(manifestPath));
                var prompts = JsonSerializer.Deserialize<List<PromptEntry>>(File.ReadAllText(promptsPath));
                if (manifest == null || prompts == null) continue;

                string title = manifest.Title ?? Path.GetFileName(dir);
                string theme = manifest.Theme ?? "";

                var imagePrompts = prompts
                    .Where(p => p.Key != null && p.Key.Contains("prompt", StringComparison.OrdinalIgnoreCase))
                    .Select(p => p.Prompt ?? "")
                    .Where(p => !string.IsNullOrWhiteSpace(p))
                    .Distinct()
                    .ToList();

                foreach (var prompt in imagePrompts)
                {
                    if (!visited.Add(prompt.GetHashCode().ToString())) continue;

                    try
                    {
                        var embedding = await ollama.GetEmbeddingVectorAsync(model, prompt);
                        var record = new VectorRecord
                        {
                            Embedding = embedding,
                            Concept = theme,
                            Prompt = prompt,
                            Score = 0,
                            Source = title,
                            Timestamp = DateTime.UtcNow
                        };
                        _index.Add(embedding, record);
                        _records.Add(record);
                        count++;
                    }
                    catch { }
                }
            }
            catch { }
        }

        return count;
    }

    private async Task<int> ScanCheckingWithEmbeddingsAsync(string rootDir, OllamaClient ollama, string model, HashSet<string> visited)
    {
        int count = 0;

        try
        {
            foreach (var monthDir in Directory.GetDirectories(rootDir))
            {
                foreach (var dayDir in Directory.GetDirectories(monthDir))
                {
                    foreach (var bundleDir in Directory.GetDirectories(dayDir))
                    {
                        var phase1Path = Path.Combine(bundleDir, "phase_1.json");
                        if (!File.Exists(phase1Path)) continue;

                        try
                        {
                            var promptData = JsonSerializer.Deserialize<Phase1Prompt>(File.ReadAllText(phase1Path));
                            if (promptData == null || string.IsNullOrWhiteSpace(promptData.positive)) continue;

                            var promptKey = promptData.positive.GetHashCode().ToString();
                            if (!visited.Add(promptKey)) continue;

                            var embedding = await ollama.GetEmbeddingVectorAsync(model, promptData.positive);
                            var record = new VectorRecord
                            {
                                Embedding = embedding,
                                Concept = Path.GetFileName(bundleDir),
                                Prompt = promptData.positive,
                                Score = 0,
                                Source = $"Checking/{Path.GetFileName(monthDir)}/{Path.GetFileName(dayDir)}",
                                Timestamp = DateTime.UtcNow
                            };
                            _index.Add(embedding, record);
                            _records.Add(record);
                            count++;
                        }
                        catch { }
                    }
                }
            }
        }
        catch { }

        return count;
    }

    private record RecordsContainer(List<VectorRecord>? records);
    private record JobManifest(string? Title, string? Theme);
    private record PromptEntry(string? Source, string? Key, string? Prompt);
    private record Phase1Prompt(string positive, string negative, int width, int height, int steps, float cfg);
}


