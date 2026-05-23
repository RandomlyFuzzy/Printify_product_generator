using System.Text.Json;
using PrintifyGenerator.Library.VectorStorage;

string ollamaUrl = "http://192.168.0.151:11434";
string embedModel = "llava:latest";
string chatModel = "gemma4:e2b";
string storePath = "vector_store.json";
string marketDir = Path.Combine(
    AppContext.BaseDirectory, "..", "..", "..", "..", "..", "category_features");

Console.WriteLine("hello world");
var store = new VectorStore(storePath);
store.Load();

var ollama = new OllamaClient(ollamaUrl);
var embedder = new MarketDataEmbedder(store, marketDir);
var rng = new Random();

Console.ForegroundColor = ConsoleColor.Cyan;
Console.WriteLine("╔══════════════════════════════════════════════════════╗");
Console.WriteLine("║   Vector Search & RAG Interactive Console           ║");
Console.WriteLine("║   Prompt RAG + Market Intelligence Search           ║");
Console.WriteLine("╚══════════════════════════════════════════════════════╝");
Console.ResetColor();
Console.WriteLine();
Console.WriteLine($"  Ollama: {ollamaUrl}  Model: {embedModel}");
Console.WriteLine($"  Store:  {storePath}  ({store.Count} vectors)");
Console.WriteLine($"  Market: {marketDir}");
Console.WriteLine();
    Console.WriteLine("  Commands:");
    Console.WriteLine("    <text>            Search prompts by text substring");
    Console.WriteLine("    /embed <text>     Semantic search via Ollama embedding");
    Console.WriteLine("    /scan             Scan output/, Checking/, for prompts");
    Console.WriteLine("    /market           Load market data from category_features/");
    Console.WriteLine("    /purchase <q>     Search both prompts + market data");
    Console.WriteLine("    /eval <prompt>    RAG-enhanced prompt quality estimate");
    Console.WriteLine("    /tools            Ollama tool-calling demo (RAG + scoring)");
    Console.WriteLine("    /store            Show store statistics");
    Console.WriteLine("    /list [n]         Top n records by score");
    Console.WriteLine("    /recent [n]       n most recent records");
    Console.WriteLine("    /concept <c>      Filter records by concept");
    Console.WriteLine("    /ingest-all       Scan all sources + market data (fast)");
    Console.WriteLine("    /embed-all        Ollama-embed all records missing embeddings");
    Console.WriteLine("    /save             Persist vector store to disk");
    Console.WriteLine("    /clear            Clear the console screen");
    Console.WriteLine("    /reset            Clear in-memory store (use /save first to persist)");
    Console.WriteLine("    /purge            Delete ALL persisted vector store files");
    Console.WriteLine("    /quit             Exit");
Console.WriteLine();

while (true)
{
    Console.ForegroundColor = ConsoleColor.Green;
    Console.Write("search> ");
    Console.ResetColor();
    var input = Console.ReadLine()?.Trim();
    if (string.IsNullOrEmpty(input)) continue;
    if (input == "/quit") break;

    try
    {
        switch (input.Split(' ')[0])
        {
            case "/scan":     await CmdScan(); break;
            case "/market":   await CmdMarket(); break;
            case "/purchase": await CmdPurchase(input); break;
            case "/embed":    await CmdEmbed(input); break;
            case "/eval":     await CmdEval(input); break;
            case "/tools":    await CmdTools(); break;
            case "/store":    CmdStore(); break;
            case "/list":     CmdList(input); break;
            case "/recent":   CmdRecent(input); break;
            case "/concept":  CmdConcept(input); break;
            case "/ingest-all": await CmdIngestAll(); break;
            case "/embed-all": await CmdEmbedAll(); break;
            case "/save":     CmdSave(); break;
            case "/clear":    CmdClear(); break;
            case "/reset":    CmdReset(); break;
            case "/purge":    CmdPurge(); break;
            default:          CmdTextSearch(input); break;
        }
    }
    catch (Exception ex)
    {
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine($"  Error: {ex.Message}");
        Console.ResetColor();
    }

    Console.WriteLine();
}

// ==========================================
// COMMAND IMPLEMENTATIONS
// ==========================================

async Task CmdScan()
{
    Console.WriteLine("  Scanning data sources...");
    var sources = store.ScanAllDataSources();
    foreach (var s in sources)
        Console.WriteLine($"    Found: {s}");

    int count = store.BatchEmbedExistingData([.. sources]);
    Console.WriteLine($"  Scanned {count} prompts (store now has {store.Count} vectors)");

    if (count > 0)
    {
        Console.Write("  Compute embeddings with Ollama? (y/n): ");
        if (Console.ReadLine()?.Trim().ToLower() == "y")
        {
            Console.WriteLine("  Embedding via Ollama...");
            count = await store.BatchEmbedWithOllamaAsync([.. sources], ollama, embedModel);
            Console.WriteLine($"  Embedded {count} prompts");
        }
    }

    store.Save();
    Console.WriteLine("  Store saved.");
}

async Task CmdMarket()
{
    Console.WriteLine("  Loading market data from category_features/...");
    int count = embedder.EmbedCategoryFiles();

    if (count == 0)
    {
        Console.WriteLine("  No category feature files found.");
        Console.WriteLine("  Directory checked: " + marketDir);
        Console.WriteLine("  Creating sample market entry for testing...");

        store.Store("market:diy-crafts",
            "DIY crafts category: sewing, knitting, scrapbooking, polymer clay. " +
            "High CTR: handmade(+15%), custom(+12%), gift(+10%). " +
            "Top colors: pastel pink, mint green, lavender. " +
            "Avg price: $24.99. Trending materials: cotton, polymer clay, wool.",
            [], 7.5f, "market_data");

        store.Store("market:home-decor",
            "Home decor: wall art, throw pillows, candles, vases. " +
            "High CTR: boho(+18%), minimalist(+14%), farmhouse(+11%). " +
            "Top colors: warm beige, sage green, terracotta. " +
            "Avg price: $39.99. Trending materials: ceramic, linen, wood.",
            [], 8.0f, "market_data");

        store.Store("market:kids-toys",
            "Kids toys and educational games. Wooden toys, puzzles, plush. " +
            "High CTR: educational(+20%), montessori(+16%), organic(+12%). " +
            "Top colors: pastel rainbow, natural wood, soft blue. " +
            "Avg price: $29.99. Trending materials: beech wood, organic cotton, wool felt.",
            [], 7.0f, "market_data");

        count = 3;
    }

    Console.WriteLine($"  Embedded {count} market categories");

    if (count > 0)
    {
        Console.Write("  Compute embeddings with Ollama? (y/n): ");
        if (Console.ReadLine()?.Trim().ToLower() == "y")
        {
            var marketRecords = store.GetByConcept("market_data");
            Console.WriteLine($"  Embedding {marketRecords.Count} market entries...");
            int embedded = 0;
            foreach (var rec in marketRecords)
            {
                try
                {
                    var emb = await ollama.GetEmbeddingVectorAsync(embedModel, rec.Prompt);
                    rec.Embedding = emb;
                    embedded++;
                }
                catch { }
            }
            Console.WriteLine($"  Embedded {embedded} market entries");
        }
    }

    store.Save();
    Console.WriteLine("  Store saved.");
}

async Task CmdIngestAll()
{
    Console.WriteLine("╔══════════════════════════════════════════╗");
    Console.WriteLine("║   FULL DATA INGEST (fast, no embedding) ║");
    Console.WriteLine("╚══════════════════════════════════════════╝");

    Console.WriteLine("[1/2] Scanning prompt data sources...");
    var sources = store.ScanAllDataSources();
    if (sources.Count == 0)
    {
        Console.WriteLine("  No data source directories found.");
    }
    else
    {
        foreach (var s in sources)
            Console.WriteLine($"  Found: {s}");

        int count = store.BatchEmbedExistingData([.. sources]);
        Console.WriteLine($"  Scanned {count} prompts ({store.GetAllRecords().Count} total records)");
    }

    Console.WriteLine("[2/2] Loading market data...");
    int marketCount = embedder.EmbedCategoryFiles();
    if (marketCount == 0)
    {
        var existingMarket = store.GetAllRecords()
            .Where(r => r.Source == "market_data")
            .Select(r => r.Concept)
            .ToHashSet();

        marketCount = existingMarket.Count;
        if (marketCount == 0)
        {
            Console.WriteLine("  No category_features/ found. Creating sample market entries...");
            store.Store("market:diy-crafts",
                "DIY crafts category: sewing, knitting, scrapbooking, polymer clay. " +
                "High CTR: handmade(+15%), custom(+12%), gift(+10%). " +
                "Top colors: pastel pink, mint green, lavender. " +
                "Avg price: $24.99. Trending materials: cotton, polymer clay, wool.",
                [], 7.5f, "market_data");
            store.Store("market:home-decor",
                "Home decor: wall art, throw pillows, candles, vases. " +
                "High CTR: boho(+18%), minimalist(+14%), farmhouse(+11%). " +
                "Top colors: warm beige, sage green, terracotta. " +
                "Avg price: $39.99. Trending materials: ceramic, linen, wood.",
                [], 8.0f, "market_data");
            store.Store("market:kids-toys",
                "Kids toys and educational games. Wooden toys, puzzles, plush. " +
                "High CTR: educational(+20%), montessori(+16%), organic(+12%). " +
                "Top colors: pastel rainbow, natural wood, soft blue. " +
                "Avg price: $29.99. Trending materials: beech wood, organic cotton, wool felt.",
                [], 7.0f, "market_data");
            marketCount = 3;
        }
    }
    Console.WriteLine($"  Loaded {marketCount} market entries");

    Console.WriteLine("\nSaving store...");
    store.Save();
    PrintShardInfo();
    Console.WriteLine("  Run /embed-all when ready to compute Ollama embeddings.");
}

async Task CmdEmbedAll()
{
    Console.WriteLine("╔══════════════════════════════════════════╗");
    Console.WriteLine("║   OLLAMA EMBEDDING                      ║");
    Console.WriteLine("╚══════════════════════════════════════════╝");

    bool ollamaOk = false;
    try
    {
        var check = await ollama.GetEmbeddingVectorAsync(embedModel, "test");
        ollamaOk = check.Length > 0;
    }
    catch (Exception ex)
    {
        Console.WriteLine($"  Ollama not reachable: {ex.Message}");
        return;
    }

    if (!ollamaOk) { Console.WriteLine("  Ollama returned empty response."); return; }

    var allRecords = store.GetAllRecords();
    int pending = allRecords.Count(r => r.Embedding == null || r.Embedding.Length == 0);
    if (pending == 0) { Console.WriteLine("  All records already have embeddings."); return; }

    Console.WriteLine($"  Embedding {pending} records via {embedModel}...");
    var startTime = DateTime.UtcNow;
    int embedded = 0;
    int concurrency = 3;
    var sem = new SemaphoreSlim(concurrency);
    var tasks = new List<Task>();

    foreach (var rec in allRecords)
    {
        if (rec.Embedding.Length > 0) continue;
        await sem.WaitAsync();
        tasks.Add(Task.Run(async () =>
        {
            try
            {
                rec.Embedding = await ollama.GetEmbeddingVectorAsync(embedModel, rec.Prompt);
                int done = Interlocked.Increment(ref embedded);
                if (done % 25 == 0 || done == pending)
                {
                    var elapsed = DateTime.UtcNow - startTime;
                    double pct = (double)done / pending * 100;
                    int rate = done / (int)Math.Max(1, elapsed.TotalSeconds);
                    int eta = (pending - done) / Math.Max(1, rate);
                    Console.WriteLine($"  [{done}/{pending}] {pct:F0}%  {rate}/s  ETA {eta}s");
                }
            }
            catch
            {
                Interlocked.Increment(ref embedded);
            }
            finally
            {
                sem.Release();
            }
        }));
    }

    await Task.WhenAll(tasks);
    var total = DateTime.UtcNow - startTime;
    Console.WriteLine($"  Embedded {embedded}/{pending} records in {total.TotalSeconds:F0}s");

    Console.WriteLine("  Rebuilding HNSW index with new embeddings...");
    store.RebuildIndex(); // Need to expose this
    store.Save();
    PrintShardInfo();
}

void PrintShardInfo()
{
    string baseDir = Path.GetDirectoryName(Path.GetFullPath(storePath)) ?? ".";
    string baseName = Path.GetFileNameWithoutExtension(storePath);
    var shardFiles = Directory.GetFiles(baseDir, $"{baseName}_*.json")
        .OrderBy(f => f).ToList();

    int totalRecords = store.GetAllRecords().Count;
    int withEmbeddings = store.Count;
    long totalKb = shardFiles.Sum(f => new FileInfo(f).Length) / 1024;
    Console.WriteLine($"  {totalRecords} records ({withEmbeddings} with embeddings)");
    Console.WriteLine($"  {shardFiles.Count} shard(s), {totalKb} KB total");
    foreach (var sf in shardFiles.Take(3))
        Console.WriteLine($"    {Path.GetFileName(sf)} ({new FileInfo(sf).Length / 1024} KB)");
    if (shardFiles.Count > 3)
        Console.WriteLine($"    ... and {shardFiles.Count - 3} more");
}

async Task CmdPurchase(string input)
{
    var query = input["/purchase ".Length..].Trim();
    if (string.IsNullOrEmpty(query)) { Console.WriteLine("  Provide a search query"); return; }

    Console.WriteLine($"  Purchase/market search: '{query}'");

    var textResults = store.SearchByText(query, 10);
    var marketResults = textResults.Where(r => r.Source == "market_data").ToList();
    var promptResults = textResults.Where(r => r.Source != "market_data").ToList();

    if (marketResults.Count > 0)
    {
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine($"  Market intelligence ({marketResults.Count}):");
        Console.ResetColor();
        foreach (var r in marketResults.Take(5))
        {
            Console.WriteLine($"    [{r.Score:F1}] {r.Concept}");
            Console.WriteLine($"    {r.Prompt[..Math.Min(r.Prompt.Length, 120)]}");
        }
    }

    if (promptResults.Count > 0)
    {
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine($"  Related prompts ({promptResults.Count}):");
        Console.ResetColor();
        foreach (var r in promptResults.Take(5))
        {
            Console.WriteLine($"    [{r.Score:F1}] {r.Concept}");
            Console.WriteLine($"    {r.Prompt[..Math.Min(r.Prompt.Length, 100)]}");
        }
    }

    if (textResults.Count == 0)
        Console.WriteLine("  No results. Try /embed for semantic search.");
}

async Task CmdEmbed(string input)
{
    var text = input["/embed ".Length..].Trim();
    if (string.IsNullOrEmpty(text)) { Console.WriteLine("  Provide text to embed"); return; }

    Console.WriteLine($"  Embedding: {text}");
    float[] embedding;
    try
    {
        embedding = await ollama.GetEmbeddingVectorAsync(embedModel, text);
    }
    catch (Exception ex)
    {
        Console.WriteLine($"  Ollama embedding failed: {ex.Message}");
        Console.WriteLine("  Falling back to text search...");
        CmdTextSearch(input);
        return;
    }

    Console.WriteLine($"  Embedding dim: {embedding.Length}");
    var results = store.SearchSimilar(embedding, 10);

    if (results.Count == 0)
    {
        Console.WriteLine("  No similar vectors found.");
        return;
    }

    Console.WriteLine($"  Semantic search results:");
    for (int i = 0; i < results.Count; i++)
    {
        var (record, sim) = results[i];
        string marker = record.Source == "market_data" ? "📊" : "🖼";
        Console.WriteLine($"  {marker} {i + 1}. [sim={sim:F4}] [{record.Score:F1}] {record.Concept}");
        Console.WriteLine($"     {record.Prompt[..Math.Min(record.Prompt.Length, 100)]}");
    }
}

async Task CmdEval(string input)
{
    var prompt = input["/eval ".Length..].Trim();
    if (string.IsNullOrEmpty(prompt)) { Console.WriteLine("  Provide a prompt to evaluate"); return; }

    Console.WriteLine($"  RAG-enhanced evaluation: {prompt}");
    float[] embedding;
    try
    {
        embedding = await ollama.GetEmbeddingVectorAsync(embedModel, prompt);
    }
    catch (Exception ex)
    {
        Console.WriteLine($"  Ollama embedding failed: {ex.Message}");
        return;
    }

    var similar = store.SearchSimilar(embedding, 8);
    Console.WriteLine($"  Similar past prompts + market data:");
    foreach (var (rec, sim) in similar)
    {
        string status = rec.Score >= 7 ? "HIGH" : rec.Score >= 4 ? "MID" : "LOW";
        Console.ForegroundColor = rec.Score >= 7 ? ConsoleColor.Green : rec.Score >= 4 ? ConsoleColor.Yellow : ConsoleColor.DarkGray;
        string src = rec.Source == "market_data" ? "[MKT]" : "[PRM]";
        Console.WriteLine($"  {src} {status} sim={sim:F3} score={rec.Score:F1}");
        Console.WriteLine($"       {rec.Prompt[..Math.Min(rec.Prompt.Length, 80)]}");
        Console.ResetColor();
    }

    float promptAvg = similar.Where(s => s.record.Source != "market_data")
        .Select(s => s.record.Score).DefaultIfEmpty(5).Average();
    float marketBoost = similar.Where(s => s.record.Source == "market_data")
        .Select(s => s.record.Score).DefaultIfEmpty(0).Average() / 10f;

    float finalScore = Math.Clamp(promptAvg + marketBoost + (float)(rng.NextDouble() - 0.5) * 2, 0, 10);
    Console.ForegroundColor = ConsoleColor.Magenta;
    Console.WriteLine($"  Predicted quality: {finalScore:F1}/10 (prompt avg: {promptAvg:F1}, market boost: {marketBoost:F2})");
    Console.ResetColor();
}

async Task CmdTools()
{
    Console.WriteLine("  Running Ollama tool-calling demo...");

    var schema = JsonDocument.Parse("""
        {"type":"object","properties":{"query":{"type":"string"}},"required":["query"]}
        """).RootElement;

    var tools = new List<PrintifyGenerator.Library.Ollama.ToolDefinition>
    {
        new("search_prompts", "Find similar prompt examples by concept", schema),
        new("search_market", "Find market intelligence for a product category", schema),
        new("get_top_scores", "Get top-performing prompts by quality score", schema)
    };

    var messages = new List<PrintifyGenerator.Library.Ollama.ChatMessage>
    {
        PrintifyGenerator.Library.Ollama.ChatMessage.System(
            "You are a prompt engineering assistant. Use tools to help craft better prompts. " +
            "search_prompts finds similar image prompts, search_market finds market data about categories."
        ),
        PrintifyGenerator.Library.Ollama.ChatMessage.User("I want to create a coloring book about ocean animals for kids. What prompts would work?")
    };

    var result = await ollama.ChatWithToolsAsync(chatModel, messages, tools, async call =>
    {
        string query = "ocean animals kids coloring book";
        if (call.Arguments.TryGetValue("query", out var q))
            query = q.GetString() ?? query;

        Console.WriteLine($"    → Tool called: {call.Name}(\"{query}\")");

        switch (call.Name)
        {
            case "search_prompts":
                var textResults = store.SearchByText(query, 5);
                if (textResults.Count == 0)
                    return "No similar prompts found.";
                return string.Join("\n", textResults.Select((r, i) =>
                    $"{i + 1}. (score:{r.Score:F1}) {r.Prompt}"));

            case "search_market":
                var marketResults = store.SearchByText(query, 5)
                    .Where(r => r.Source == "market_data").ToList();
                if (marketResults.Count == 0)
                    return "No market data found. Consider: kids toys, educational, ocean theme.";
                return string.Join("\n", marketResults.Select((r, i) =>
                    $"{i + 1}. {r.Concept}: {r.Prompt[..Math.Min(r.Prompt.Length, 120)]}"));

            case "get_top_scores":
                var top = store.GetTopByScore(5);
                if (top.Count == 0) return "No records.";
                return string.Join("\n", top.Select((r, i) =>
                    $"{i + 1}. [{r.Score:F1}] {r.Concept}: {r.Prompt[..Math.Min(r.Prompt.Length, 100)]}"));

            default:
                return "Unknown tool";
        }
    });

    Console.WriteLine($"  Ollama response: {result.Content}");
    Console.WriteLine($"  Turns: {result.MessageHistory.Count}");
}

void CmdStore()
{
    Console.WriteLine($"  Vectors: {store.Count}");
    var prompts = store.GetAllRecords().Where(r => r.Source != "market_data").ToList();
    var market = store.GetAllRecords().Where(r => r.Source == "market_data").ToList();
    Console.WriteLine($"  Image prompts: {prompts.Count}, Market entries: {market.Count}");

    if (store.Count > 0)
    {
        var top = store.GetTopByScore(5);
        Console.WriteLine("  Top by score:");
        foreach (var r in top)
        {
            string src = r.Source == "market_data" ? "📊" : "🖼";
            Console.WriteLine($"    {src} [{r.Score:F1}] {r.Concept}: {r.Prompt[..Math.Min(r.Prompt.Length, 80)]}");
        }
    }
}

void CmdList(string input)
{
    int n = int.TryParse(input.Split(' ').ElementAtOrDefault(1), out var x) ? x : 10;
    var items = store.GetTopByScore(n);
    if (items.Count == 0) { Console.WriteLine("  (empty)"); return; }
    for (int i = 0; i < items.Count; i++)
    {
        var r = items[i];
        string src = r.Source == "market_data" ? "📊" : "🖼";
        Console.WriteLine($"  {src} {i + 1}. [{r.Score:F1}] {r.Concept}");
        Console.WriteLine($"     {r.Prompt[..Math.Min(r.Prompt.Length, 100)]}");
    }
}

void CmdRecent(string input)
{
    int n = int.TryParse(input.Split(' ').ElementAtOrDefault(1), out var x) ? x : 10;
    var items = store.GetRecent(n);
    if (items.Count == 0) { Console.WriteLine("  (empty)"); return; }
    for (int i = 0; i < items.Count; i++)
    {
        var r = items[i];
        string src = r.Source == "market_data" ? "📊" : "🖼";
        Console.WriteLine($"  {src} {i + 1}. [{r.Score:F1}] {r.Concept}");
        Console.WriteLine($"     {r.Prompt[..Math.Min(r.Prompt.Length, 100)]}");
    }
}

void CmdConcept(string input)
{
    var concept = input["/concept ".Length..].Trim();
    if (string.IsNullOrEmpty(concept)) { Console.WriteLine("  Specify a concept"); return; }
    var items = store.GetByConcept(concept);
    Console.WriteLine($"  {items.Count} results for '{concept}':");
    foreach (var r in items.Take(10))
    {
        string src = r.Source == "market_data" ? "📊" : "🖼";
        Console.WriteLine($"  {src} [{r.Score:F1}] {r.Prompt[..Math.Min(r.Prompt.Length, 100)]}");
    }
}

void CmdSave()
{
    store.Save();
    Console.WriteLine("  Store saved.");
}

void CmdClear()
{
    Console.Clear();
}

void CmdReset()
{
    int prevCount = store.Count;
    store.Clear();
    Console.WriteLine($"  In-memory store cleared (was: {prevCount} records).");
    Console.WriteLine("  Use /scan or /ingest-all to re-scan data, or /save /quit to exit.");
}

void CmdPurge()
{
    Console.Write("  This will DELETE ALL persisted vector store files. Continue? (y/n): ");
    if (Console.ReadLine()?.Trim().ToLower() != "y")
    {
        Console.WriteLine("  Purge cancelled.");
        return;
    }

    store.ClearPersistence();
    store.Clear();

    Console.WriteLine("  Persistence purged and in-memory cleared.");
}

void CmdTextSearch(string input)
{
    var results = store.SearchByText(input, 10);
    if (results.Count == 0)
    {
        Console.WriteLine("  No text matches. Try /embed for semantic search, /market for market data.");
        return;
    }
    Console.WriteLine($"  Text search: '{input}' — {results.Count} results");
    for (int i = 0; i < results.Count; i++)
    {
        var r = results[i];
        string src = r.Source == "market_data" ? "📊" : "🖼";
        Console.WriteLine($"  {src} {i + 1}. [{r.Score:F1}] {r.Concept}");
        Console.WriteLine($"     {r.Prompt[..Math.Min(r.Prompt.Length, 100)]}");
    }
}
