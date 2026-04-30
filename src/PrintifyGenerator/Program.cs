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

OllamaClient? ollama = new OllamaClient("http://localhost:11434");
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

// ── MockupGenerator ────────────────────────────────────────────────────────────
var generator = new MockupGenerator(
    printify:     printify,
    ollama:       ollama,
    shopId:       shopId);

// ── Process images from phase_3 results ───────────────────────────────────────
const string checkingPath = "./src/data/Checking";

if (!Directory.Exists(checkingPath))
{
    Console.Error.WriteLine("[WARN] No phase-3 data directories were found.");
    return;
}

int success = 0, failed = 0, processed = 0;

// Stream through files one at a time to avoid high memory usage
foreach (var jsonFile in EnumerateSuitabilityFiles(checkingPath).Distinct(StringComparer.OrdinalIgnoreCase))
{
    processed++;
    Console.WriteLine($"[{processed}] Processing: {Path.GetFileName(Path.GetDirectoryName(jsonFile))}");

    try
    {
        var suitability = JsonSerializer.Deserialize<ImageSuitability>(
            await File.ReadAllTextAsync(jsonFile),
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        if (suitability is null)
            continue;

        string? path = ResolveImagePath(suitability.imageURL, jsonFile);
        if (path is null)
        {
            Console.WriteLine($"  Skipping: Could not resolve image path");
            continue;
        }

        var eligibility = PublishingEligibilityEvaluator.Evaluate(
            path,
            suitability,
            orchestrationSettings.MinimumPublishScore,
            publishingOverrides);

        if (!eligibility.IsEligibleForPublishing)
        {
            Console.WriteLine($"  Skipping: {eligibility.Reason}");
            continue;
        }

        if (eligibility.HasManualOverride)
            Console.WriteLine($"  Including (override): {eligibility.Reason}");

        // Process the image
        var results = await generator.ProcessImageAsync(path);

        foreach (var result in results)
        {
            if (result.Success)
            {
                Console.WriteLine($"  ✓ Draft created – product ID: {result.Draft!.ProductId}");
                Console.WriteLine($"    Blueprint: {result.Draft.BlueprintTitle}");
                success++;
            }
            else
            {
                Console.Error.WriteLine($"  ✗ Failed: {result.Error}");
                failed++;
            }
        }
    }
    catch (Exception ex)
    {
        Console.Error.WriteLine($"  Error: {ex.Message}");
        failed++;
    }

    Console.WriteLine();
}

Console.WriteLine($"\nDone. Processed: {processed}, Drafts created: {success}, Failed: {failed}");
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