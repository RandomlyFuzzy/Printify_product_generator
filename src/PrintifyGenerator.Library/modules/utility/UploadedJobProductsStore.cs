using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;

public static class UploadedJobProductsStore
{
    private static readonly JsonSerializerOptions JsonOptions = new JsonSerializerOptions
    {
        PropertyNameCaseInsensitive = true,
        WriteIndented = true
    };

    public static string GetUploadsPath(string dataBasePath)
    {
        var normalizedDataRoot = Path.GetFullPath(dataBasePath);
        return Path.Combine(normalizedDataRoot, "staging", "uploaded-job-products.json");
    }

    public static Dictionary<string, List<string>> Load(string dataBasePath)
    {
        var uploadsPath = GetUploadsPath(dataBasePath);
        if (!File.Exists(uploadsPath))
            return CreateEmpty();

        try
        {
            var json = File.ReadAllText(uploadsPath);
            var loaded = JsonSerializer.Deserialize<List<Dictionary<string, List<string>>>>(json, JsonOptions);
            return Sanitize(loaded);
        }
        catch
        {
            return CreateEmpty();
        }
    }

    public static void Save(string dataBasePath, IReadOnlyDictionary<string, List<string>> uploads)
    {
        var sanitized = Sanitize(uploads);
        var uploadsPath = GetUploadsPath(dataBasePath);
        var directory = Path.GetDirectoryName(uploadsPath);
        if (!string.IsNullOrWhiteSpace(directory))
            Directory.CreateDirectory(directory);

        var serialized = sanitized
            .OrderBy(entry => entry.Key, StringComparer.OrdinalIgnoreCase)
            .Select(entry => new Dictionary<string, List<string>>
            {
                [entry.Key] = entry.Value
            })
            .ToList();

        File.WriteAllText(uploadsPath, JsonSerializer.Serialize(serialized, JsonOptions));
    }

    public static void TrackUpload(string dataBasePath, string? jobId, string? productId)
    {
        if (string.IsNullOrWhiteSpace(jobId) || string.IsNullOrWhiteSpace(productId))
            return;

        var uploads = Load(dataBasePath);
        MergeEntry(uploads, jobId, new[] { productId });
        Save(dataBasePath, uploads);
    }

    private static Dictionary<string, List<string>> Sanitize(List<Dictionary<string, List<string>>>? uploads)
    {
        var sanitized = CreateEmpty();

        foreach (var entry in uploads ?? new List<Dictionary<string, List<string>>>())
        {
            foreach (var pair in entry)
                MergeEntry(sanitized, pair.Key, pair.Value);
        }

        return sanitized;
    }

    private static Dictionary<string, List<string>> Sanitize(IReadOnlyDictionary<string, List<string>> uploads)
    {
        var sanitized = CreateEmpty();

        foreach (var pair in uploads)
            MergeEntry(sanitized, pair.Key, pair.Value);

        return sanitized;
    }

    private static void MergeEntry(
        Dictionary<string, List<string>> uploads,
        string? jobId,
        IEnumerable<string>? productIds)
    {
        if (string.IsNullOrWhiteSpace(jobId))
            return;

        var normalizedJobId = jobId.Trim();
        if (!uploads.TryGetValue(normalizedJobId, out var knownProductIds))
        {
            knownProductIds = new List<string>();
            uploads[normalizedJobId] = knownProductIds;
        }

        foreach (var productId in productIds ?? Enumerable.Empty<string>())
        {
            if (string.IsNullOrWhiteSpace(productId))
                continue;

            var normalizedProductId = productId.Trim();
            if (!knownProductIds.Contains(normalizedProductId, StringComparer.OrdinalIgnoreCase))
                knownProductIds.Add(normalizedProductId);
        }

        knownProductIds.Sort(StringComparer.OrdinalIgnoreCase);
    }

    private static Dictionary<string, List<string>> CreateEmpty()
    {
        return new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);
    }
}