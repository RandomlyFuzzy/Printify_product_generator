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
Console.WriteLine("Starting Printify Mockup Generator...\n");
Console.WriteLine($"Loading API token from ./main.env... {token}");
if (string.IsNullOrWhiteSpace(token))
{
    Console.Error.WriteLine("[ERROR] TOKEN not found in ./main.env");
    return;
}

// ── Clients ────────────────────────────────────────────────────────────────────
const string dataBasePath = "./src/data";
var orchestrationSettingsPath = OrchestrationSettingsStore.GetSettingsPath(dataBasePath);
var orchestrationSettings = OrchestrationSettingsStore.Load(dataBasePath);
var publishingOverrides = PublishingOverrideStore.Load(dataBasePath);

PrintifyClient printify = new PrintifyClient(token);

OllamaClient? ollama = new OllamaClient();//"http://192.168.0.131:11434");
// OrchestrationNode? selectedOllamaNode = null;
// foreach (var candidate in orchestrationSettings.Ollama.Where(node => node.Enabled && !string.IsNullOrWhiteSpace(node.BaseUrl)))
// {
//     try
//     {
//         var candidateClient = new OllamaClient(candidate.BaseUrl);
//         await candidateClient.CheckStatusAsync();
//         var installedModels = await candidateClient.GetInstalledModelNamesAsync();
//         if (!installedModels.Contains(orchestrationSettings.MockupVisionModel))
//         {
//             candidateClient.Dispose();
//             continue;
//         }

//         ollama = candidateClient;
//         selectedOllamaNode = candidate;
//         break;
//     }
//     catch
//     {
//         // try the next configured node
//     }
// }

// if (ollama is null || selectedOllamaNode is null)
// {
//     Console.Error.WriteLine($"[ERROR] No reachable Ollama nodes were found in {orchestrationSettingsPath}");
//     return;
// }

// Console.WriteLine($"Using Ollama node: {selectedOllamaNode.Name} ({selectedOllamaNode.BaseUrl})");

// ── Auto-resolve shop ID ───────────────────────────────────────────────────────
Console.WriteLine("Fetching shop list from Printify...");
var shops = await printify.GetShopsAsync();
if (shops.Count == 0)
{
    Console.Error.WriteLine("[ERROR] No shops found on this Printify account.");
    return;
}
foreach (var shop in shops)
    Console.WriteLine($"  - {shop.Title} (ID: {shop.Id})");


int shopId = shops.Where(a=>a.Title=="Staging").FirstOrDefault()?.Id ?? 0;
Console.WriteLine($"Using shop: \"{shops.FirstOrDefault(a=>a.Id==shopId)?.Title}\" (ID: {shopId})");

// ── MockupGenerator (moondream:latest, drafts only – no publishing) ────────────
var generator = new MockupGenerator(
    printify:     printify,
    ollama:       ollama,
    shopId:       shopId,
    dataBasePath: dataBasePath,
    visionModel:  orchestrationSettings.MockupVisionModel);

// ── Collect candidate images from phase_3 results ─────────────────────────────
const string checkingPath = "./src/data/Checking";

if (!Directory.Exists(checkingPath) )
{
    Console.Error.WriteLine("[WARN] No phase-3 data directories were found.");
    Console.Error.WriteLine($"  → Expected one of: {checkingPath}");
}

var jsonFiles = EnumerateSuitabilityFiles(checkingPath)
    .Distinct(StringComparer.OrdinalIgnoreCase)
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

        if (suitability is null)
            continue;

        string? path = ResolveImagePath(suitability.imageURL, jsonFile);
        if (path is null)
            continue;

        var eligibility = PublishingEligibilityEvaluator.Evaluate(
            path,
            suitability,
            orchestrationSettings.MinimumPublishScore,
            publishingOverrides);

        if (!eligibility.IsEligibleForPublishing)
        {
            Console.WriteLine($"Skipping {path}: {eligibility.Reason}");
            continue;
        }

        if (eligibility.HasManualOverride)
            Console.WriteLine($"Including {path}: {eligibility.Reason}");

        imagePaths.Add(path);
    }
    catch { /* skip malformed records */ }
}

imagePaths = imagePaths
    .Distinct(StringComparer.OrdinalIgnoreCase)
    .ToList();

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
    
    await foreach (var result in generator.ProcessImageAsync(imagePath))
    {
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
    }

    Console.WriteLine();
}

Console.WriteLine($"Done. {success} draft(s) created, {failed} failed.");
Console.WriteLine("Inspect drafts in ./src/data/staging/drafts/ before publishing.");

static IEnumerable<string> EnumerateSuitabilityFiles(string checkingRoot)
{
    if (Directory.Exists(checkingRoot))
    {
        foreach (var file in Directory.EnumerateFiles(checkingRoot, "phase_3.json", SearchOption.AllDirectories))
            yield return file;
    }

}

static string? ResolveImagePath(string? imageUrl, string jsonFile)
{
    var resolvedPath = ResolveFromImageUrl(imageUrl);
    if (resolvedPath is not null)
        return resolvedPath;

    var folderPath = Path.GetDirectoryName(jsonFile);
    if (string.IsNullOrWhiteSpace(folderPath))
        return null;

    var folderName = Path.GetFileName(folderPath);
    if (string.IsNullOrWhiteSpace(folderName))
        return null;

    foreach (var extension in new[] { ".png", ".jpg", ".jpeg", ".webp" })
    {
        var siblingImage = Path.Combine(folderPath, folderName + extension);
        if (File.Exists(siblingImage))
            return Path.GetFullPath(siblingImage);
    }

    return null;
}

static string? ResolveFromImageUrl(string? imageUrl)
{
    if (string.IsNullOrWhiteSpace(imageUrl))
        return null;

    var trimmed = imageUrl.Trim();

    if (Uri.TryCreate(trimmed, UriKind.Absolute, out var fileUri) && fileUri.IsFile && File.Exists(fileUri.LocalPath))
        return fileUri.LocalPath;

    if (trimmed.StartsWith("file://", StringComparison.OrdinalIgnoreCase))
    {
        var withoutScheme = trimmed["file://".Length..];
        if (!withoutScheme.StartsWith('/'))
            withoutScheme = "/" + withoutScheme;

        var legacyPath = Path.GetFullPath(withoutScheme);
        if (File.Exists(legacyPath))
            return legacyPath;
    }

    var localPath = Path.IsPathRooted(trimmed)
        ? trimmed
        : Path.GetFullPath(trimmed);

    return File.Exists(localPath) ? Path.GetFullPath(localPath) : null;
}