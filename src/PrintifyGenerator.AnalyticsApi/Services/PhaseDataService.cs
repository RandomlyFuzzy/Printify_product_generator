using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Options;
using PrintifyGenerator.AnalyticsApi.Models;

namespace PrintifyGenerator.AnalyticsApi.Services;

public sealed class PhaseDataService
{
    private static readonly Regex BundleRegex = new("^[0-9a-fA-F-]{36}$", RegexOptions.Compiled | RegexOptions.CultureInvariant);
    private readonly AnalyticsApiOptions _options;
    private readonly string _contentRoot;

    public PhaseDataService(IOptions<AnalyticsApiOptions> options, IWebHostEnvironment env)
    {
        _options = options.Value;
        _contentRoot = env.ContentRootPath;
    }

    public IReadOnlyList<PhaseBundleSummary> ListBundles(int limit)
    {
        var results = new List<PhaseBundleSummary>();
        var seenBundleDirs = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var root in EnumerateCheckingRoots())
        {
            if (!Directory.Exists(root))
            {
                continue;
            }

            foreach (var bundleDir in Directory.EnumerateDirectories(root, "*", SearchOption.AllDirectories))
            {
                var normalizedBundleDir = Path.GetFullPath(bundleDir);
                if (!seenBundleDirs.Add(normalizedBundleDir))
                {
                    continue;
                }

                var name = Path.GetFileName(bundleDir);
                if (!BundleRegex.IsMatch(name) || !Guid.TryParse(name, out var bundleId))
                {
                    continue;
                }

                var files = Directory.EnumerateFiles(bundleDir, "*", SearchOption.TopDirectoryOnly).ToArray();
                var phases = files
                    .Select(Path.GetFileName)
                    .Where(static file => !string.IsNullOrWhiteSpace(file))
                    .Select(static file => ParsePhase(file!))
                    .Where(static phase => !string.IsNullOrWhiteSpace(phase))
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .OrderBy(static p => p, StringComparer.OrdinalIgnoreCase)
                    .ToArray();

                var updated = files.Length == 0
                    ? Directory.GetLastWriteTimeUtc(bundleDir)
                    : files.Select(File.GetLastWriteTimeUtc).DefaultIfEmpty(Directory.GetLastWriteTimeUtc(bundleDir)).Max();

                results.Add(new PhaseBundleSummary(
                    bundleId,
                    bundleDir,
                    phases,
                    updated));
            }
        }

        return results
            .OrderByDescending(x => x.LastUpdatedUtc)
            .Take(Math.Max(1, limit))
            .ToArray();
    }

    public IReadOnlyList<PhaseOverviewItem> GetPhaseOverview()
    {
        var artifacts = EnumerateAllArtifacts();

        return artifacts
            .GroupBy(a => a.Phase, StringComparer.OrdinalIgnoreCase)
            .OrderBy(g => g.Key, StringComparer.OrdinalIgnoreCase)
            .Select(g => new PhaseOverviewItem(
                g.Key,
                g.Count(),
                g.Max(x => (DateTime?)x.LastUpdatedUtc)))
            .ToArray();
    }

    public PhaseBundleSummary? GetBundle(Guid bundleId)
    {
        var path = FindBundlePath(bundleId);
        if (path is null)
        {
            return null;
        }

        var files = Directory.EnumerateFiles(path, "*", SearchOption.TopDirectoryOnly).ToArray();
        var phases = files
            .Select(Path.GetFileName)
            .Where(static file => !string.IsNullOrWhiteSpace(file))
            .Select(static file => ParsePhase(file!))
            .Where(static phase => !string.IsNullOrWhiteSpace(phase))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(static p => p, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        var updated = files.Length == 0
            ? Directory.GetLastWriteTimeUtc(path)
            : files.Select(File.GetLastWriteTimeUtc).DefaultIfEmpty(Directory.GetLastWriteTimeUtc(path)).Max();

        return new PhaseBundleSummary(bundleId, path, phases, updated);
    }

    public IReadOnlyList<PhaseArtifactReference> GetPhaseArtifacts(Guid bundleId, string phase)
    {
        var path = FindBundlePath(bundleId);
        if (path is null)
        {
            return Array.Empty<PhaseArtifactReference>();
        }

        var normalizedPhase = phase.Trim().ToLowerInvariant();
        if (!normalizedPhase.StartsWith("phase", StringComparison.Ordinal))
        {
            normalizedPhase = $"phase{normalizedPhase}";
        }

        return Directory.EnumerateFiles(path, "*", SearchOption.TopDirectoryOnly)
            .Where(file => Path.GetFileName(file).StartsWith(normalizedPhase, StringComparison.OrdinalIgnoreCase))
            .Select(file => new PhaseArtifactReference(
                bundleId,
                ParsePhase(Path.GetFileName(file) ?? string.Empty),
                file,
                File.GetLastWriteTimeUtc(file)))
            .OrderByDescending(item => item.LastUpdatedUtc)
            .ToArray();
    }

    public IReadOnlyList<PhaseArtifactReference> GetLatestPhaseArtifacts(string phase, int limit)
    {
        var normalizedPhase = NormalizePhase(phase);
        return EnumerateAllArtifacts()
            .Where(a => a.Phase.Equals(normalizedPhase, StringComparison.OrdinalIgnoreCase))
            .OrderByDescending(a => a.LastUpdatedUtc)
            .Take(Math.Max(1, limit))
            .ToArray();
    }

    public PhaseArtifactResult? GetArtifact(Guid bundleId, string fileName)
    {
        var path = FindBundlePath(bundleId);
        if (path is null)
        {
            return null;
        }

        var candidate = Path.Combine(path, fileName);
        if (!File.Exists(candidate))
        {
            return null;
        }

        var contentType = GuessContentType(candidate);
        var phase = ParsePhase(fileName);

        if (contentType == "application/json")
        {
            var doc = JsonDocument.Parse(File.ReadAllText(candidate));
            return new PhaseArtifactResult(bundleId, fileName, phase, contentType, doc.RootElement.Clone(), null, File.GetLastWriteTimeUtc(candidate));
        }

        return new PhaseArtifactResult(bundleId, fileName, phase, contentType, null, File.ReadAllText(candidate), File.GetLastWriteTimeUtc(candidate));
    }

    public IngestResult SaveProductDefinition(IngestProductDefinitionRequest request)
    {
        var bundleId = request.BundleId.GetValueOrDefault(Guid.NewGuid());
        var bundleDir = EnsureBundleDirectory(bundleId);

        var payload = new
        {
            title = request.Title,
            description = request.Description,
            mainCategory = request.MainCategory,
            subCategory = request.SubCategory,
            audience = request.Audience,
            primaryColor = request.PrimaryColor,
            material = request.Material,
            useCase = request.UseCase,
            keywords = request.Keywords ?? Array.Empty<string>()
        };

        var filePath = Path.Combine(bundleDir, "product_definition.json");
        File.WriteAllText(filePath, JsonSerializer.Serialize(payload, new JsonSerializerOptions { WriteIndented = true }));

        return new IngestResult(bundleId, filePath, "Product definition saved.");
    }

    public IngestResult SavePhaseData(IngestPhaseDataRequest request)
    {
        var bundleId = request.BundleId.GetValueOrDefault(Guid.NewGuid());
        var bundleDir = EnsureBundleDirectory(bundleId);

        var phase = request.Phase?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(phase))
        {
            phase = "phase_custom";
        }

        var normalizedPhase = phase.StartsWith("phase", StringComparison.OrdinalIgnoreCase)
            ? phase
            : $"phase{phase}";

        var safeFileName = Path.GetFileName(string.IsNullOrWhiteSpace(request.FileName)
            ? $"{normalizedPhase}.ingest.json"
            : request.FileName);

        if (!safeFileName.EndsWith(".json", StringComparison.OrdinalIgnoreCase) &&
            !safeFileName.EndsWith(".txt", StringComparison.OrdinalIgnoreCase))
        {
            safeFileName += ".json";
        }

        var filePath = Path.Combine(bundleDir, safeFileName);
        using var stream = File.Create(filePath);
        using var writer = new Utf8JsonWriter(stream, new JsonWriterOptions { Indented = true });
        request.Payload.WriteTo(writer);

        return new IngestResult(bundleId, filePath, "Phase data saved.");
    }

    private string EnsureBundleDirectory(Guid bundleId)
    {
        var now = DateTime.UtcNow;
        var root = ResolvePath(_options.IntakeRoot);
        var dir = Path.Combine(root, now.ToString("yyyy-MM"), now.ToString("dd"), bundleId.ToString());
        Directory.CreateDirectory(dir);
        return dir;
    }

    private IEnumerable<string> EnumerateCheckingRoots()
    {
        var resolved = ResolvePath(_options.DataRoot);
        var srcData = ResolvePath("../data");

        var candidates = new[]
        {
            Path.Combine(resolved, "Checking"),
            Path.Combine(resolved, "checking"),
            Path.Combine(srcData, "Checking"),
            Path.Combine(srcData, "checking")
        };

        foreach (var candidate in candidates
            .Select(Path.GetFullPath)
            .Distinct(StringComparer.OrdinalIgnoreCase))
        {
            yield return candidate;
        }
    }

    private IEnumerable<PhaseArtifactReference> EnumerateAllArtifacts()
    {
        var seenFiles = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var root in EnumerateCheckingRoots())
        {
            if (!Directory.Exists(root))
            {
                continue;
            }

            foreach (var bundleDir in Directory.EnumerateDirectories(root, "*", SearchOption.AllDirectories))
            {
                var dirName = Path.GetFileName(bundleDir);
                if (!Guid.TryParse(dirName, out var bundleId))
                {
                    continue;
                }

                foreach (var file in Directory.EnumerateFiles(bundleDir, "*", SearchOption.TopDirectoryOnly))
                {
                    var normalizedFilePath = Path.GetFullPath(file);
                    if (!seenFiles.Add(normalizedFilePath))
                    {
                        continue;
                    }

                    var phase = ParsePhase(Path.GetFileName(file) ?? string.Empty);
                    if (string.IsNullOrWhiteSpace(phase))
                    {
                        continue;
                    }

                    yield return new PhaseArtifactReference(
                        bundleId,
                        phase,
                        file,
                        File.GetLastWriteTimeUtc(file));
                }
            }
        }
    }

    private string? FindBundlePath(Guid bundleId)
    {
        var needle = bundleId.ToString();

        foreach (var root in EnumerateCheckingRoots())
        {
            if (!Directory.Exists(root))
            {
                continue;
            }

            var direct = Directory.EnumerateDirectories(root, needle, SearchOption.AllDirectories).FirstOrDefault();
            if (!string.IsNullOrWhiteSpace(direct))
            {
                return direct;
            }
        }

        return null;
    }

    private string ResolvePath(string path)
    {
        if (Path.IsPathRooted(path))
        {
            return path;
        }

        return Path.GetFullPath(Path.Combine(_contentRoot, path));
    }

    private static string GuessContentType(string path)
    {
        var ext = Path.GetExtension(path);
        return ext.Equals(".json", StringComparison.OrdinalIgnoreCase)
            ? "application/json"
            : "text/plain";
    }

    private static string ParsePhase(string fileName)
    {
        if (string.IsNullOrWhiteSpace(fileName))
        {
            return string.Empty;
        }

        var lowered = fileName.ToLowerInvariant();

        if (lowered.StartsWith("phase_"))
        {
            return lowered.Split('.', 2)[0].Replace("phase_", "phase");
        }

        if (lowered.StartsWith("phase"))
        {
            return lowered.Split('.', 2)[0];
        }

        if (lowered.Contains("definition"))
        {
            return "phase1";
        }

        return string.Empty;
    }

    private static string NormalizePhase(string phase)
    {
        var normalized = phase.Trim().ToLowerInvariant();
        return normalized.StartsWith("phase", StringComparison.Ordinal)
            ? normalized
            : $"phase{normalized}";
    }
}
