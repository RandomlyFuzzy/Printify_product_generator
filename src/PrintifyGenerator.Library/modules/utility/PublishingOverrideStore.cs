using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;

public static class PublishingOverrideStore
{
    private static readonly JsonSerializerOptions JsonOptions = new JsonSerializerOptions
    {
        PropertyNameCaseInsensitive = true,
        WriteIndented = true
    };

    public static string GetOverridesPath(string dataBasePath)
    {
        var normalizedDataRoot = Path.GetFullPath(dataBasePath);
        return Path.Combine(normalizedDataRoot, "staging", "publishing-overrides.json");
    }

    public static PublishingOverrideCollection Load(string dataBasePath)
    {
        var overridesPath = GetOverridesPath(dataBasePath);
        if (!File.Exists(overridesPath))
            return new PublishingOverrideCollection();

        try
        {
            var json = File.ReadAllText(overridesPath);
            var loaded = JsonSerializer.Deserialize<PublishingOverrideCollection>(json, JsonOptions);
            return Sanitize(loaded ?? new PublishingOverrideCollection());
        }
        catch
        {
            return new PublishingOverrideCollection();
        }
    }

    public static void Save(string dataBasePath, PublishingOverrideCollection overrides)
    {
        var sanitized = Sanitize(overrides);
        var overridesPath = GetOverridesPath(dataBasePath);
        var directory = Path.GetDirectoryName(overridesPath);
        if (!string.IsNullOrWhiteSpace(directory))
            Directory.CreateDirectory(directory);

        File.WriteAllText(overridesPath, JsonSerializer.Serialize(sanitized, JsonOptions));
    }

    public static ImagePublishingOverride? Find(string? imagePath, PublishingOverrideCollection? overrides)
    {
        if (string.IsNullOrWhiteSpace(imagePath) || overrides is null)
            return null;

        var normalizedImagePath = Path.GetFullPath(imagePath);
        return overrides.Overrides.FirstOrDefault(entry =>
            string.Equals(entry.ImagePath, normalizedImagePath, StringComparison.OrdinalIgnoreCase));
    }

    public static void SetOverride(string dataBasePath, string imagePath, string mode)
    {
        var overrides = Load(dataBasePath);
        var normalizedImagePath = Path.GetFullPath(imagePath);
        var normalizedMode = PublishingOverrideModes.Normalize(mode);

        overrides.Overrides.RemoveAll(entry =>
            string.Equals(entry.ImagePath, normalizedImagePath, StringComparison.OrdinalIgnoreCase));

        if (normalizedMode != PublishingOverrideModes.Automatic)
        {
            overrides.Overrides.Add(new ImagePublishingOverride
            {
                ImagePath = normalizedImagePath,
                Mode = normalizedMode,
                UpdatedAtUtc = DateTime.UtcNow
            });
        }

        Save(dataBasePath, overrides);
    }

    private static PublishingOverrideCollection Sanitize(PublishingOverrideCollection overrides)
    {
        var sanitized = new PublishingOverrideCollection();
        foreach (var entry in overrides.Overrides ?? new List<ImagePublishingOverride>())
        {
            if (string.IsNullOrWhiteSpace(entry.ImagePath))
                continue;

            sanitized.Overrides.RemoveAll(existing =>
                string.Equals(existing.ImagePath, Path.GetFullPath(entry.ImagePath), StringComparison.OrdinalIgnoreCase));

            sanitized.Overrides.Add(new ImagePublishingOverride
            {
                ImagePath = Path.GetFullPath(entry.ImagePath),
                Mode = PublishingOverrideModes.Normalize(entry.Mode),
                UpdatedAtUtc = entry.UpdatedAtUtc == default ? DateTime.UtcNow : entry.UpdatedAtUtc
            });
        }

        return sanitized;
    }
}