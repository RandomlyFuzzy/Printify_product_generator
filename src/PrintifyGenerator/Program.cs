using System.IO;
using System.Linq;
using System.Text.Json;

// ── Load API token ─────────────────────────────────────────────────────────────
string token = "";
if (File.Exists("./main.env"))
{
    foreach (var line in File.ReadAllLines("./main.env"))
    {
        if (line.StartsWith("TOKEN="))
        {
            token = line["TOKEN=".Length..].Trim();
            break;
        }
    }
}

if (string.IsNullOrWhiteSpace(token))
{
    Console.Error.WriteLine("[ERROR] TOKEN not found in ./main.env");
    return;
}

// ── Clients ────────────────────────────────────────────────────────────────────
PrintifyClient printify = new PrintifyClient(token);
OllamaClient   ollama   = new OllamaClient();

// ── Auto-resolve shop ID ───────────────────────────────────────────────────────
Console.WriteLine("Fetching shop list from Printify...");
var shops = await printify.GetShopsAsync();
if (shops.Count == 0)
{
    Console.Error.WriteLine("[ERROR] No shops found on this Printify account.");
    return;
}
int shopId = shops[0].Id;
Console.WriteLine($"Using shop: \"{shops[0].Title}\" (ID: {shopId})");

// ── MockupGenerator (moondream:latest, drafts only – no publishing) ────────────
var generator = new MockupGenerator(
    printify:     printify,
    ollama:       ollama,
    shopId:       shopId,
    dataBasePath: "./src/data",
    visionModel:  "moondream:latest");

// ── Collect candidate images from phase_3 results ─────────────────────────────
const string phase3Path = "./src/data/phase_3";
if (!Directory.Exists(phase3Path))
{
    Console.Error.WriteLine($"[WARN] Phase-3 data directory not found: {phase3Path}");
    Console.Error.WriteLine("  → Checking old_data fallback...");
}

var jsonFiles = (Directory.Exists(phase3Path)
        ? Directory.EnumerateFiles(phase3Path, "*.json", SearchOption.AllDirectories)
        : Enumerable.Empty<string>())
    .Concat(Directory.Exists("./src/old_data/phase_3")
        ? Directory.EnumerateFiles("./src/old_data/phase_3", "*.json", SearchOption.AllDirectories)
        : Enumerable.Empty<string>())
    .ToList();

Console.WriteLine($"Found {jsonFiles.Count} phase-3 suitability records.");

var imagePaths = new List<string>();
foreach (var jsonFile in jsonFiles)
{
    try
    {
        var suitability = JsonSerializer.Deserialize<ImageSuitability>(
            await File.ReadAllTextAsync(jsonFile),
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        if (suitability is null)            continue;
        if (!suitability.IsSuitableForPrint()) continue;
        if (suitability.OverallScore() < 6.0f) continue;

        // imageURL is stored as "file://..." – strip the scheme
        string path = suitability.imageURL.StartsWith("file://")
            ? suitability.imageURL[7..]
            : suitability.imageURL;

        if (File.Exists(path))
            imagePaths.Add(path);
    }
    catch { /* skip malformed records */ }
}

if (imagePaths.Count == 0)
{
    Console.WriteLine("No suitable images found to process.");
    return;
}

Console.WriteLine($"\n{imagePaths.Count} suitable image(s) queued for mockup generation.");
Console.WriteLine("Drafts will be created on Printify but NOT published.\n");

// ── Process each image ─────────────────────────────────────────────────────────
int success = 0, failed = 0;
foreach (var (imagePath, idx) in imagePaths.Select((p, i) => (p, i)))
{
    Console.WriteLine($"[{idx + 1}/{imagePaths.Count}] {imagePath}");
    var result = await generator.ProcessImageAsync(imagePath);

    if (result.Success)
    {
        Console.WriteLine($"  ✓ Draft created – product ID: {result.Draft!.ProductId}");
        Console.WriteLine($"  Blueprint: {result.Draft.BlueprintTitle}");
        Console.WriteLine($"  Reason:    {result.Draft.LlmReason}");
        success++;
    }
    else
    {
        Console.Error.WriteLine($"  ✗ Failed: {result.Error}");
        failed++;
    }

    Console.WriteLine();
}

Console.WriteLine($"Done. {success} draft(s) created, {failed} failed.");
Console.WriteLine("Inspect drafts in ./src/data/staging/drafts/ before publishing.");