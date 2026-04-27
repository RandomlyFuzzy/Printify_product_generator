public sealed class PhaseBundle
{
    public Guid Id { get; }
    public string DirectoryPath { get; }

    public PhaseBundle(Guid id, string directoryPath)
    {
        Id = id;
        DirectoryPath = directoryPath;
    }

    public static bool TryCreate(string directoryPath, out PhaseBundle? bundle)
    {
        var folderName = Path.GetFileName(directoryPath.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));
        if (!Guid.TryParse(folderName, out var id))
        {
            bundle = null;
            return false;
        }

        bundle = new PhaseBundle(id, directoryPath);
        return true;
    }

    public int GetHighestCompletedPhase(IReadOnlyList<IPhaseGenerator> pipeline)
    {
        for (var i = pipeline.Count - 1; i >= 0; i--)
        {
            if (pipeline[i].IsComplete(this))
            {
                return pipeline[i].PhaseNumber;
            }
        }

        return 0;
    }

    public string GetPath(params string[] parts)
    {
        return Path.Combine(new[] { DirectoryPath }.Concat(parts).ToArray());
    }

    public string? FindImagePath()
    {
        var extensions = new[] { ".png", ".jpg", ".jpeg", ".webp" };
        foreach (var extension in extensions)
        {
            var candidate = GetPath($"{Id}{extension}");
            if (File.Exists(candidate))
            {
                return candidate;
            }
        }

        return null;
    }

    public string ResolvePhaseFile(int phaseNumber, string extension, bool underscoreLegacy = false)
    {
        var ext = extension.TrimStart('.');
        var canonical = GetPath($"phase{phaseNumber}.{ext}");
        var legacy = GetPath($"phase_{phaseNumber}.{ext}");

        if (File.Exists(canonical))
        {
            return canonical;
        }

        if (File.Exists(legacy))
        {
            return legacy;
        }

        return underscoreLegacy ? legacy : canonical;
    }

    public IReadOnlyList<string> ReadPhase4ProductIds()
    {
        var file = ResolvePhaseFile(4, "txt");
        if (!File.Exists(file))
        {
            return Array.Empty<string>();
        }

        return File.ReadAllLines(file)
            .Select(line => line.Trim())
            .Where(line => !string.IsNullOrWhiteSpace(line))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    public static PhaseBundle CreateSynthetic(Guid id)
    {
        //./src/data/Checking/yyyy-mm/dd/{id}/phaseN.json
        var tempDir = Path.Combine("src", "data", "Checking", DateTime.Now.ToString("yyyy-MM"), DateTime.Now.ToString("dd"), id.ToString());

        Directory.CreateDirectory(tempDir);

        return new PhaseBundle(id, tempDir);
    }
}