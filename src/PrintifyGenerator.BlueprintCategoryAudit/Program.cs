using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

var detailsDir = GetArgValue(args, "--details-dir") ?? Path.Combine("src", "data", "Cached", "blueprint_details");
var referenceDir = GetArgValue(args, "--reference-dir") ?? "category_features";
var outputDir = GetArgValue(args, "--output-dir") ?? Path.Combine("src", "data", "Checking", "blueprint_category_audit");

detailsDir = Path.GetFullPath(detailsDir);
referenceDir = Path.GetFullPath(referenceDir);
outputDir = Path.GetFullPath(outputDir);

if (!Directory.Exists(detailsDir))
{
	Console.Error.WriteLine($"Details directory not found: {detailsDir}");
	Environment.Exit(1);
}

if (!Directory.Exists(referenceDir))
{
	Console.Error.WriteLine($"Reference directory not found: {referenceDir}");
	Environment.Exit(1);
}

Directory.CreateDirectory(outputDir);

var detailFiles = Directory.GetFiles(detailsDir, "*.json", SearchOption.TopDirectoryOnly);
var numericDetailIds = new HashSet<int>();
var blueprintTitles = new List<(int? Id, string Title)>();

foreach (var file in detailFiles)
{
	var fileName = Path.GetFileNameWithoutExtension(file);
	if (int.TryParse(fileName, NumberStyles.Integer, CultureInfo.InvariantCulture, out var fileId))
	{
		numericDetailIds.Add(fileId);
	}

	foreach (var item in ExtractBlueprintTitles(file))
	{
		blueprintTitles.Add(item);
	}
}

var blueprintCatalogPath = Path.Combine(detailsDir, "blueprints.json");
var catalogIds = new HashSet<int>();
var catalogBlueprints = new List<(int Id, string Title)>();
if (File.Exists(blueprintCatalogPath))
{
	foreach (var item in ExtractBlueprintTitles(blueprintCatalogPath))
	{
		if (item.Id.HasValue)
		{
			catalogIds.Add(item.Id.Value);
			catalogBlueprints.Add((item.Id.Value, item.Title));
		}
	}
}

if (catalogBlueprints.Count == 0)
{
	foreach (var item in blueprintTitles.Where(x => x.Id.HasValue).Select(x => (x.Id!.Value, x.Title)).DistinctBy(x => x.Value))
	{
		catalogBlueprints.Add(item);
		catalogIds.Add(item.Value);
	}
}

var inferredMainCategories = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
var rawCategoryPhrases = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
var unknownCategoryTitles = new List<string>();
var missingCoverageBlueprints = new List<(int Id, string Title, string ProductType)>();

foreach (var (id, title) in catalogBlueprints)
{
	if (string.IsNullOrWhiteSpace(title))
	{
		continue;
	}

	var phrase = ExtractRawCategoryPhrase(title);
	Increment(rawCategoryPhrases, phrase);

	var normalized = InferMainCategory(phrase, title);
	if (normalized is null)
	{
		unknownCategoryTitles.Add(title);
		missingCoverageBlueprints.Add((id, title, InferProductType(title)));
		continue;
	}

	Increment(inferredMainCategories, normalized);
}

var existingMainCategoryKeys = Directory
	.GetFiles(referenceDir, "MainCategory_*.txt", SearchOption.TopDirectoryOnly)
	.Select(path => Path.GetFileNameWithoutExtension(path)["MainCategory_".Length..])
	.ToHashSet(StringComparer.OrdinalIgnoreCase);

var inferredCategoryKeys = inferredMainCategories.Keys.ToHashSet(StringComparer.OrdinalIgnoreCase);
var missingCategoryData = inferredCategoryKeys.Where(k => !existingMainCategoryKeys.Contains(k)).OrderBy(k => k).ToList();
var extraCategoryData = existingMainCategoryKeys.Where(k => !inferredCategoryKeys.Contains(k)).OrderBy(k => k).ToList();

var missingDetailIds = catalogIds.Where(id => !numericDetailIds.Contains(id)).OrderBy(id => id).ToList();
var orphanDetailIds = numericDetailIds.Where(id => !catalogIds.Contains(id)).OrderBy(id => id).ToList();

var missingCoverageByType = missingCoverageBlueprints
	.GroupBy(x => x.ProductType)
	.OrderByDescending(g => g.Count())
	.ThenBy(g => g.Key)
	.Select(g => new
	{
		productType = g.Key,
		count = g.Count(),
		ids = g.Select(x => x.Id).Distinct().OrderBy(x => x).ToArray()
	})
	.ToArray();

var summary = new
{
	generatedAtUtc = DateTime.UtcNow,
	detailsDirectory = detailsDir,
	referenceDirectory = referenceDir,
	totals = new
	{
		detailJsonFiles = detailFiles.Length,
		numericDetailFiles = numericDetailIds.Count,
		blueprintTitlesRead = blueprintTitles.Count,
		inferredMainCategoryCount = inferredMainCategories.Count,
		unknownCategoryTitleCount = unknownCategoryTitles.Count
	},
	inferredMainCategories = inferredMainCategories
		.OrderByDescending(kv => kv.Value)
		.ThenBy(kv => kv.Key)
		.Select(kv => new { category = kv.Key, count = kv.Value })
		.ToArray(),
	rawCategoryPhrases = rawCategoryPhrases
		.OrderByDescending(kv => kv.Value)
		.ThenBy(kv => kv.Key)
		.Select(kv => new { phrase = kv.Key, count = kv.Value })
		.ToArray(),
	missingMainCategoryData = missingCategoryData,
	extraMainCategoryData = extraCategoryData,
	missingDetailIds = missingDetailIds,
	orphanDetailIds = orphanDetailIds,
	missingCoverageByProductType = missingCoverageByType,
	unknownCategoryTitles = unknownCategoryTitles
		.Distinct(StringComparer.OrdinalIgnoreCase)
		.OrderBy(s => s)
		.Take(200)
		.ToArray()
};

var jsonPath = Path.Combine(outputDir, "blueprint_category_audit.json");
var csvPath = Path.Combine(outputDir, "blueprint_category_counts.csv");
var missingCoverageCsvPath = Path.Combine(outputDir, "blueprint_missing_coverage.csv");

var jsonOptions = new JsonSerializerOptions { WriteIndented = true };
File.WriteAllText(jsonPath, JsonSerializer.Serialize(summary, jsonOptions));
WriteCategoryCsv(csvPath, inferredMainCategories);
WriteMissingCoverageCsv(missingCoverageCsvPath, missingCoverageBlueprints);

Console.WriteLine("Blueprint category audit complete.");
Console.WriteLine($"JSON report: {jsonPath}");
Console.WriteLine($"CSV report:  {csvPath}");
Console.WriteLine($"Missing coverage CSV: {missingCoverageCsvPath}");
Console.WriteLine($"Inferred categories: {inferredMainCategories.Count}");
Console.WriteLine($"Missing category data files: {missingCategoryData.Count}");
Console.WriteLine($"Missing detail IDs: {missingDetailIds.Count}");
Console.WriteLine($"Missing coverage blueprints: {missingCoverageBlueprints.Count}");

foreach (var item in missingCoverageByType.Take(10))
{
	Console.WriteLine($"  {item.productType}: {item.count}");
}

static string? GetArgValue(string[] args, string key)
{
	for (var i = 0; i < args.Length - 1; i++)
	{
		if (string.Equals(args[i], key, StringComparison.OrdinalIgnoreCase))
		{
			return args[i + 1];
		}
	}

	return null;
}

static IEnumerable<(int? Id, string Title)> ExtractBlueprintTitles(string filePath)
{
	using var stream = File.OpenRead(filePath);
	using var document = JsonDocument.Parse(stream);
	var root = document.RootElement;

	if (root.ValueKind == JsonValueKind.Array)
	{
		foreach (var item in root.EnumerateArray())
		{
			var title = GetString(item, "title");
			if (string.IsNullOrWhiteSpace(title))
			{
				continue;
			}

			yield return (GetInt(item, "id"), title!);
		}

		yield break;
	}

	if (root.ValueKind == JsonValueKind.Object)
	{
		if (root.TryGetProperty("blueprint", out var blueprintObj) && blueprintObj.ValueKind == JsonValueKind.Object)
		{
			var title = GetString(blueprintObj, "title");
			if (!string.IsNullOrWhiteSpace(title))
			{
				yield return (GetInt(blueprintObj, "id"), title!);
			}

			yield break;
		}

		var rootTitle = GetString(root, "title");
		if (!string.IsNullOrWhiteSpace(rootTitle))
		{
			yield return (GetInt(root, "id"), rootTitle!);
		}
	}
}

static string ExtractRawCategoryPhrase(string title)
{
	var normalized = Regex.Replace(title.ToLowerInvariant(), "[^a-z0-9' ]+", " ").Trim();
	normalized = Regex.Replace(normalized, "\\s+", " ");

	var tokens = normalized.Split(' ', StringSplitOptions.RemoveEmptyEntries);
	var cutWords = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
	{
		"tee", "tshirt", "t-shirt", "tank", "hoodie", "sweatshirt", "dress", "jacket", "socks",
		"hat", "cap", "shirt", "bodysuit", "pullover", "bag", "leggings", "pants", "shorts"
	};

	var selected = new List<string>();
	foreach (var token in tokens)
	{
		if (cutWords.Contains(token))
		{
			break;
		}

		selected.Add(token);
		if (selected.Count == 4)
		{
			break;
		}
	}

	if (selected.Count == 0)
	{
		return tokens.Length > 0 ? tokens[0] : "unknown";
	}

	return string.Join(' ', selected);
}

static string? InferMainCategory(string phrase, string fullTitle)
{
	var text = (phrase + " " + fullTitle).ToLowerInvariant();

	if (text.Contains("baby girl", StringComparison.Ordinal)) return "baby_girl";
	if (text.Contains("baby boy", StringComparison.Ordinal)) return "baby_boy";

	if (Regex.IsMatch(text, "\\bwomen('?s)?\\b|\\bwoman\\b|\\bladies\\b")) return "woman";
	if (Regex.IsMatch(text, "\\bmen('?s)?\\b|\\bman\\b")) return "man";
	if (Regex.IsMatch(text, "\\bboys\\b|\\bboy\\b")) return "boy";
	if (Regex.IsMatch(text, "\\bgirls\\b|\\bgirl\\b")) return "girl";

	if (Regex.IsMatch(text, "\\binfant\\b|\\btoddler\\b|\\bbaby\\b")) return "unisex_baby";
	if (Regex.IsMatch(text, "\\bkids\\b|\\bchild\\b|\\byouth\\b")) return "unisex_child";

	if (Regex.IsMatch(text, "\\bunisex\\b"))
	{
		if (Regex.IsMatch(text, "\\badult\\b")) return "unisex_adult";
		return "unisex";
	}

	return null;
}

static string? GetString(JsonElement element, string propertyName)
{
	if (element.TryGetProperty(propertyName, out var prop) && prop.ValueKind == JsonValueKind.String)
	{
		return prop.GetString();
	}

	return null;
}

static int? GetInt(JsonElement element, string propertyName)
{
	if (!element.TryGetProperty(propertyName, out var prop))
	{
		return null;
	}

	if (prop.ValueKind == JsonValueKind.Number && prop.TryGetInt32(out var intVal))
	{
		return intVal;
	}

	if (prop.ValueKind == JsonValueKind.String && int.TryParse(prop.GetString(), NumberStyles.Integer, CultureInfo.InvariantCulture, out intVal))
	{
		return intVal;
	}

	return null;
}

static void Increment(Dictionary<string, int> map, string key)
{
	map[key] = map.GetValueOrDefault(key, 0) + 1;
}

static void WriteCategoryCsv(string filePath, Dictionary<string, int> categories)
{
	var sb = new StringBuilder();
	sb.AppendLine("Category,Count");

	foreach (var kv in categories.OrderByDescending(x => x.Value).ThenBy(x => x.Key))
	{
		sb.AppendLine($"\"{kv.Key}\",{kv.Value}");
	}

	File.WriteAllText(filePath, sb.ToString());
}

static void WriteMissingCoverageCsv(string filePath, List<(int Id, string Title, string ProductType)> rows)
{
	var sb = new StringBuilder();
	sb.AppendLine("BlueprintId,ProductType,Title");

	foreach (var row in rows.OrderBy(r => r.ProductType).ThenBy(r => r.Id))
	{
		sb.AppendLine($"{row.Id},\"{EscapeCsv(row.ProductType)}\",\"{EscapeCsv(row.Title)}\"");
	}

	File.WriteAllText(filePath, sb.ToString());
}

static string EscapeCsv(string value)
{
	return value.Replace("\"", "\"\"");
}

static string InferProductType(string title)
{
	var text = title.ToLowerInvariant();

	var rules = new (string ProductType, string[] Keywords)[]
	{
		("phone_case", new[] { "phone case", "iphone case", "samsung case", "galaxy case", "pixel case" }),
		("calendar", new[] { "calendar", "desk calendar", "wall calendar" }),
		("mug", new[] { "mug", "coffee mug", "ceramic mug", "travel mug" }),
		("poster", new[] { "poster", "wall art", "print" }),
		("canvas", new[] { "canvas", "canvas print" }),
		("sticker", new[] { "sticker", "decal", "wall decal" }),
		("notebook", new[] { "notebook", "journal" }),
		("tote_bag", new[] { "tote bag", "tote" }),
		("backpack", new[] { "backpack" }),
		("duffel_bag", new[] { "duffel", "duffle" }),
		("pillow", new[] { "pillow", "pillowcase" }),
		("blanket", new[] { "blanket", "throw" }),
		("bottle", new[] { "water bottle", "bottle", "tumbler" }),
		("mouse_pad", new[] { "mouse pad" }),
		("hat", new[] { "hat", "cap", "beanie" }),
		("socks", new[] { "socks", "sock" }),
		("hoodie", new[] { "hoodie" }),
		("sweatshirt", new[] { "sweatshirt" }),
		("t_shirt", new[] { "t-shirt", "tee", "shirt" }),
		("dress", new[] { "dress" }),
		("jacket", new[] { "jacket" }),
		("bag_other", new[] { "bag", "pouch", "wallet" }),
		("shoes", new[] { "shoe", "boots", "slipper", "sandal" })
	};

	foreach (var rule in rules)
	{
		foreach (var keyword in rule.Keywords)
		{
			if (text.Contains(keyword, StringComparison.Ordinal))
			{
				return rule.ProductType;
			}
		}
	}

	return "uncategorized_title";
}
