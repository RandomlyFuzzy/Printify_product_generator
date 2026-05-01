using System.Globalization;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Options;
using PrintifyGenerator.AnalyticsApi.Models;

namespace PrintifyGenerator.AnalyticsApi.Services;

public sealed class MarketFeatureService
{
    private static readonly Regex HeaderRegex = new(
        @"^===\s*(?<name>.+?)\s*\((?<count>[0-9,]+)\s+products,\s+Type:\s*(?<type>[^\)]+)\)\s*===",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    private static readonly Regex SentimentRegex = new(
        @"^\s{2}(?<term>[^:]{2,}):\s*(?<pct>[0-9]+(?:\.[0-9]+)?)%\s+positive\s+\((?<count>[0-9,]+)\s+products\)",
        RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.IgnoreCase);

    private static readonly Regex CtrRegex = new(
        @"^\s{2}(?<term>[^:]{2,}):\s*\+?(?<lift>[0-9]+(?:\.[0-9]+)?)%\s+CTR\s+lift\s+\((?<top>[0-9,]+)\s+in\s+top\s*/\s*(?<bottom>[0-9,]+)\s+in\s+bottom\)",
        RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.IgnoreCase);

    private static readonly Regex ColorOrMaterialRegex = new(
        @"^\s{2}(?<name>[^:]{2,}):\s*(?<count>[0-9,]+)\s+products,\s*(?<sent>[0-9]+(?:\.[0-9]+)?)\s+avg\s+sentiment,\s*(?<sales>[0-9,]+)\s+(?:total\s+)?sales",
        RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.IgnoreCase);

    private static readonly Regex PriceTermRegex = new(
        @"^\s{2}(?<term>[^:]{2,}):\s*\$(?<price>[0-9]+(?:\.[0-9]+)?)\s+avg\s+\(ratio:\s*(?<ratio>[0-9]+(?:\.[0-9]+)?)\)\s*-\s*(?<tag>CHEAPER|PRICIER)\s+\((?<count>[0-9,]+)\s+products\)",
        RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.IgnoreCase);

    private static readonly Regex WordRegex = new(
        @"[a-z0-9][a-z0-9\-']+",
        RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.IgnoreCase);

    private readonly AnalyticsApiOptions _options;
    private readonly IWebHostEnvironment _env;
    private volatile List<CategorySnapshot>? _snapshotCache;
    private DateTime _cacheTimestamp = DateTime.MinValue;
    private readonly object _cacheLock = new();

    public MarketFeatureService(IOptions<AnalyticsApiOptions> options, IWebHostEnvironment env)
    {
        _options = options.Value;
        _env = env;
    }

    // ─── Public API ──────────────────────────────────────────────────────────

    public MarketSummaryResponse BuildSummary(int top)
    {
        var snapshots = GetSnapshots(out var fileCount);
        var keywords = BuildKeywordIndex(snapshots).OrderByDescending(x => x.Score).Take(Math.Max(1, top)).ToArray();
        var colors    = BuildColorMetricIndex(snapshots.SelectMany(x => x.Colors),    top);
        var materials = BuildColorMetricIndex(snapshots.SelectMany(x => x.Materials), top);
        return new MarketSummaryResponse(fileCount, snapshots.Count, keywords, colors, materials, DateTime.UtcNow);
    }

    public IReadOnlyList<CategoryListItem> GetCategories()
        => GetSnapshots(out _)
            .OrderByDescending(s => s.ProductCount)
            .Select(s => new CategoryListItem(s.Name, s.Type, s.ProductCount))
            .ToArray();

    public CategoryDetailResponse? GetCategory(string name)
    {
        var snapshot = GetSnapshots(out _)
            .FirstOrDefault(s => s.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
        return snapshot is null ? null : BuildCategoryDetail(snapshot);
    }

    public IReadOnlyList<CategoryDetailResponse> SearchCategories(string? type, int limit)
    {
        var snapshots = GetSnapshots(out _);
        var filtered = string.IsNullOrWhiteSpace(type)
            ? snapshots
            : snapshots.Where(s => s.Type.Equals(NormalizeToken(type), StringComparison.OrdinalIgnoreCase)).ToList();
        return filtered
            .OrderByDescending(s => s.ProductCount)
            .Take(Math.Max(1, limit))
            .Select(BuildCategoryDetail)
            .ToArray();
    }

    public CrossCategoryOverlapResponse GetCrossOverlap(int minCategories, int top)
    {
        var snapshots = GetSnapshots(out _);
        minCategories = Math.Max(2, minCategories);
        top = Math.Max(1, top);
        return new CrossCategoryOverlapResponse(
            minCategories,
            BuildOverlapForKeywords(snapshots, minCategories, top),
            BuildOverlapForMetric(snapshots, s => s.Colors,    "color",    minCategories, top),
            BuildOverlapForMetric(snapshots, s => s.Materials, "material", minCategories, top),
            DateTime.UtcNow);
    }

    public ProductScoreResponse ScoreProduct(ProductScoreRequest request)
    {
        var snapshots = GetSnapshots(out _);
        var signals = new List<SignalDetail>();
        var notes   = new List<string>();
        double score = 40.0;

        var candidateWords = ExtractWords(request.Title)
            .Concat(ExtractWords(request.Description))
            .Concat((request.Keywords ?? Array.Empty<string>()).SelectMany(ExtractWords))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        // Category match
        var matched = string.IsNullOrWhiteSpace(request.Category)
            ? snapshots.OrderByDescending(s => s.ProductCount).Take(15).ToList()
            : snapshots.Where(s =>
                s.Name.Contains(request.Category, StringComparison.OrdinalIgnoreCase) ||
                request.Category.Contains(s.Name, StringComparison.OrdinalIgnoreCase))
              .OrderByDescending(s => s.ProductCount).Take(15).ToList();

        if (matched.Count > 0)
        {
            var boost = Math.Min(20.0, matched.Average(s => Math.Clamp(Math.Log10(s.ProductCount + 10) / 5.0, 0.0, 1.0)) * 20.0);
            score += boost;
            signals.Add(new SignalDetail("CategoryMatch", boost,
                $"Matched {matched.Count} group(s): {string.Join(", ", matched.Take(3).Select(s => s.Name))}"));
        }
        else
        {
            notes.Add("No direct category match; scoring with global priors.");
        }

        // Keyword / CTR signal
        var kwIndex = BuildKeywordIndex(matched.Count > 0 ? matched : snapshots);
        var hitKw = kwIndex
            .Where(k => candidateWords.Contains(k.Term, StringComparer.OrdinalIgnoreCase))
            .OrderByDescending(k => k.Score).ToArray();

        if (hitKw.Length > 0)
        {
            var kwBoost = Math.Min(25.0, hitKw.Take(8).Average(k => k.Score) * 25.0);
            score += kwBoost;
            signals.Add(new SignalDetail("KeywordCTR", kwBoost,
                $"Matched: {string.Join(", ", hitKw.Take(5).Select(k => k.Term))}"));
        }
        else
        {
            notes.Add("No high-intent keyword overlap found; add category-specific phrases.");
        }

        // Title length signal
        var titleWords = request.Title?.Split(' ', StringSplitOptions.RemoveEmptyEntries).Length ?? 0;
        if (titleWords is >= 4 and <= 12)
        {
            score += 5.0;
            signals.Add(new SignalDetail("TitleLength", 5.0, $"{titleWords} words — ideal range 4-12."));
        }
        else if (titleWords > 0)
        {
            signals.Add(new SignalDetail("TitleLength", 0.0,
                titleWords < 4 ? "Title too short; aim for 4-12 words." : "Title may be too long."));
        }

        // Description quality
        var descWords = request.Description?.Split(' ', StringSplitOptions.RemoveEmptyEntries).Length ?? 0;
        if (descWords >= 20) { score += 5.0; signals.Add(new SignalDetail("DescriptionLength", 5.0, $"{descWords} words — sufficient depth.")); }
        else if (descWords > 0) { signals.Add(new SignalDetail("DescriptionLength", 0.0, "Description thin; aim for 20+ words.")); }

        // Color signal
        if (!string.IsNullOrWhiteSpace(request.Color))
        {
            var colorAgg = BuildColorMetricIndex((matched.Count > 0 ? matched : snapshots).SelectMany(s => s.Colors), 300);
            var hit = colorAgg.FirstOrDefault(c => c.Name.Equals(NormalizeToken(request.Color), StringComparison.OrdinalIgnoreCase));
            if (hit is not null) { var b = Math.Min(10.0, hit.Score * 10.0); score += b; signals.Add(new SignalDetail("Color", b, $"'{hit.Name}' score {hit.Score:F3}, sentiment {hit.Sentiment:F2}")); }
            else { signals.Add(new SignalDetail("Color", 0.0, $"No color data for '{request.Color}'.")); }
        }

        // Material signal
        if (!string.IsNullOrWhiteSpace(request.Material))
        {
            var matAgg = BuildColorMetricIndex((matched.Count > 0 ? matched : snapshots).SelectMany(s => s.Materials), 300);
            var hit = matAgg.FirstOrDefault(m => m.Name.Equals(NormalizeToken(request.Material), StringComparison.OrdinalIgnoreCase));
            if (hit is not null) { var b = Math.Min(10.0, hit.Score * 10.0); score += b; signals.Add(new SignalDetail("Material", b, $"'{hit.Name}' score {hit.Score:F3}, sentiment {hit.Sentiment:F2}")); }
            else { signals.Add(new SignalDetail("Material", 0.0, $"No material data for '{request.Material}'.")); }
        }

        // Price competitiveness
        var priceTerms = (matched.Count > 0 ? matched : snapshots)
            .SelectMany(s => s.PriceTerms).Where(t => t.IsCheaper && t.AvgPrice > 0 && t.ProductCount >= 5).ToList();
        var medianPrice = 0.0;
        if (priceTerms.Count > 0)
        {
            var sorted = priceTerms.Select(t => t.AvgPrice).OrderBy(p => p).ToList();
            medianPrice = sorted.Count % 2 == 0 ? (sorted[sorted.Count / 2 - 1] + sorted[sorted.Count / 2]) / 2.0 : sorted[sorted.Count / 2];
            var pb = Math.Min(5.0, Math.Log10(priceTerms.Count + 1) * 3.0);
            score += pb;
            signals.Add(new SignalDetail("PriceCompetitiveness", pb, $"Median competitive price ~${medianPrice:F2} ({priceTerms.Count} data points)"));
        }

        score = Math.Clamp(score, 0.0, 100.0);

        var recKeywords = hitKw.Length > 0
            ? hitKw.Take(8).Select(k => k.Term).ToArray()
            : BuildKeywordIndex(snapshots).OrderByDescending(k => k.Score).Take(8).Select(k => k.Term).ToArray();

        var src = matched.Count > 0 ? matched : snapshots;
        var recColors    = BuildColorMetricIndex(src.SelectMany(s => s.Colors),    5).Select(c => c.Name).ToArray();
        var recMaterials = BuildColorMetricIndex(src.SelectMany(s => s.Materials), 5).Select(m => m.Name).ToArray();

        if (notes.Count == 0) notes.Add("Listing looks competitive. Validate pricing against the median before publishing.");

        return new ProductScoreResponse(
            score,
            score >= 75 ? "High" : score >= 55 ? "Medium" : "Low",
            signals.ToArray(), recKeywords, recColors, recMaterials, medianPrice, notes.ToArray());
    }

    public FileIngestResult SaveCategoryFeatureFile(string fileName, string content)
    {
        var categoryDir = ResolveCategoryFeaturesPath();
        Directory.CreateDirectory(categoryDir);
        var safeName = Path.GetFileName(fileName.Trim());
        if (!safeName.EndsWith(".txt", StringComparison.OrdinalIgnoreCase)) safeName += ".txt";
        var fullPath = Path.Combine(categoryDir, safeName);
        File.WriteAllText(fullPath, content);
        lock (_cacheLock) { _snapshotCache = null; }
        return new FileIngestResult(fullPath, "Category feature file saved.");
    }

    // ─── Snapshot cache ───────────────────────────────────────────────────────

    private List<CategorySnapshot> GetSnapshots(out int fileCount)
    {
        var categoryDir = ResolveCategoryFeaturesPath();
        var files = Directory.Exists(categoryDir)
            ? Directory.EnumerateFiles(categoryDir, "*.txt", SearchOption.TopDirectoryOnly).ToArray()
            : Array.Empty<string>();
        fileCount = files.Length;
        if (files.Length == 0) return new List<CategorySnapshot>();

        var newestFile = files.Select(File.GetLastWriteTimeUtc).Max();
        lock (_cacheLock)
        {
            if (_snapshotCache is not null && _cacheTimestamp >= newestFile) return _snapshotCache;
            var fresh = new List<CategorySnapshot>(files.Length);
            foreach (var file in files) { var s = ParseFile(file); if (s is not null) fresh.Add(s); }
            _snapshotCache = fresh;
            _cacheTimestamp = newestFile;
            return fresh;
        }
    }

    private static CategoryDetailResponse BuildCategoryDetail(CategorySnapshot snapshot)
    {
        var keywords = BuildKeywordIndex(new[] { snapshot })
            .OrderByDescending(k => k.Score)
            .Select(k => new CategoryKeywordEntry(k.Term, k.Score, k.AvgCtrLift, k.AvgPositivePct, k.TopHits, k.BottomHits))
            .ToArray();

        var colors = snapshot.Colors
            .Select(c => new CategoryColorEntry(c.Name, MetricScore(c.ProductCount, c.TotalSales, c.Sentiment), c.ProductCount, c.TotalSales, c.Sentiment))
            .OrderByDescending(c => c.Score).ToArray();

        var materials = snapshot.Materials
            .Select(m => new CategoryColorEntry(m.Name, MetricScore(m.ProductCount, m.TotalSales, m.Sentiment), m.ProductCount, m.TotalSales, m.Sentiment))
            .OrderByDescending(m => m.Score).ToArray();

        var prices = snapshot.PriceTerms
            .OrderBy(t => t.Ratio)
            .Select(t => new CategoryPriceEntry(t.Term, t.AvgPrice, t.Ratio, t.IsCheaper, t.ProductCount))
            .ToArray();

        var cheaperPrices = snapshot.PriceTerms.Where(t => t.IsCheaper && t.AvgPrice > 0).Select(t => t.AvgPrice).OrderBy(p => p).ToList();
        var median = cheaperPrices.Count > 0
            ? (cheaperPrices.Count % 2 == 0 ? (cheaperPrices[cheaperPrices.Count / 2 - 1] + cheaperPrices[cheaperPrices.Count / 2]) / 2.0 : cheaperPrices[cheaperPrices.Count / 2])
            : 0.0;

        return new CategoryDetailResponse(snapshot.Name, snapshot.Type, snapshot.ProductCount,
            keywords, colors, materials, prices, median,
            cheaperPrices.Count > 0 ? cheaperPrices.First() : 0.0,
            cheaperPrices.Count > 0 ? cheaperPrices.Last()  : 0.0);
    }

    private static OverlappingFeature[] BuildOverlapForKeywords(
        IReadOnlyList<CategorySnapshot> snapshots, int minCategories, int top)
    {
        var index = new Dictionary<string, List<(string Cat, double Lift, double Pct, int Top, int Bot)>>(StringComparer.OrdinalIgnoreCase);

        foreach (var snap in snapshots)
        {
            var ctrMap  = snap.CtrTerms.ToDictionary(c => c.Term, StringComparer.OrdinalIgnoreCase);
            var sentMap = snap.SentimentTerms.ToDictionary(s => s.Term, StringComparer.OrdinalIgnoreCase);

            foreach (var term in ctrMap.Keys.Concat(sentMap.Keys).Distinct(StringComparer.OrdinalIgnoreCase))
            {
                if (!index.TryGetValue(term, out var list)) { list = new(); index[term] = list; }
                ctrMap.TryGetValue(term, out var ctr);
                sentMap.TryGetValue(term, out var sent);
                list.Add((snap.Name, ctr?.Lift ?? 0, sent?.PositivePct ?? 0, ctr?.Top ?? 0, ctr?.Bottom ?? 0));
            }
        }

        return index
            .Where(kv => kv.Value.Count >= minCategories)
            .Select(kv =>
            {
                var scores = kv.Value.Select(e =>
                    Math.Clamp(e.Lift / 250.0, 0.0, 1.0) * 0.55 +
                    Math.Clamp(e.Pct  / 100.0, 0.0, 1.0) * 0.35 +
                    Math.Clamp(Math.Log10(e.Top + e.Bot + 10) / 3.0, 0.0, 1.0) * 0.10).ToArray();
                return new OverlappingFeature(
                    kv.Key, "keyword", kv.Value.Count,
                    kv.Value.Select(e => e.Cat).Distinct().OrderBy(c => c).ToArray(),
                    scores.Average(), scores.Max(), 0, 0L);
            })
            .OrderByDescending(f => f.CategoryCount).ThenByDescending(f => f.AverageScore)
            .Take(top).ToArray();
    }

    private static OverlappingFeature[] BuildOverlapForMetric(
        IReadOnlyList<CategorySnapshot> snapshots,
        Func<CategorySnapshot, IEnumerable<MetricTerm>> selector,
        string featureType, int minCategories, int top)
    {
        var index = new Dictionary<string, List<(string Cat, int Count, long Sales, double Sent)>>(StringComparer.OrdinalIgnoreCase);

        foreach (var snap in snapshots)
        {
            foreach (var term in selector(snap))
            {
                if (!index.TryGetValue(term.Name, out var list)) { list = new(); index[term.Name] = list; }
                list.Add((snap.Name, term.ProductCount, term.TotalSales, term.Sentiment));
            }
        }

        return index
            .Where(kv => kv.Value.Count >= minCategories)
            .Select(kv =>
            {
                var scores = kv.Value.Select(e => MetricScore(e.Count, e.Sales, e.Sent)).ToArray();
                return new OverlappingFeature(
                    kv.Key, featureType, kv.Value.Count,
                    kv.Value.Select(e => e.Cat).Distinct().OrderBy(c => c).ToArray(),
                    scores.Average(), scores.Max(),
                    kv.Value.Sum(e => e.Count), kv.Value.Sum(e => e.Sales));
            })
            .OrderByDescending(f => f.CategoryCount).ThenByDescending(f => f.AverageScore)
            .Take(top).ToArray();
    }

    // ─── Parsers ──────────────────────────────────────────────────────────────

    private string ResolvePath(string path)
        => Path.IsPathRooted(path) ? path : Path.GetFullPath(Path.Combine(_env.ContentRootPath, path));

    private string ResolveCategoryFeaturesPath()
    {
        var configured = ResolvePath(_options.CategoryFeaturesPath);
        if (Directory.Exists(configured)) return configured;
        var fallback = Path.Combine(Path.GetFullPath(Path.Combine(_env.ContentRootPath, "../..")), "category_features");
        return Directory.Exists(fallback) ? fallback : configured;
    }

    private static CategorySnapshot? ParseFile(string path)
    {
        var lines = File.ReadAllLines(path);
        if (lines.Length == 0) return null;
        var header = HeaderRegex.Match(lines[0]);
        if (!header.Success) return null;

        var snapshot = new CategorySnapshot(
            NormalizeToken(header.Groups["name"].Value),
            NormalizeToken(header.Groups["type"].Value),
            ParseInt(header.Groups["count"].Value));

        var section = string.Empty;
        foreach (var line in lines.Skip(1))
        {
            if (line.StartsWith("--- ", StringComparison.Ordinal) && line.EndsWith(" ---", StringComparison.Ordinal))
            { section = line; continue; }
            if (string.IsNullOrWhiteSpace(section)) continue;

            if (section.Contains("CTR INDICATORS", StringComparison.OrdinalIgnoreCase))
            { var m = CtrRegex.Match(line); if (m.Success) snapshot.CtrTerms.Add(new(NormalizeToken(m.Groups["term"].Value), ParseDouble(m.Groups["lift"].Value), ParseInt(m.Groups["top"].Value), ParseInt(m.Groups["bottom"].Value))); continue; }

            if (section.Contains("SENTIMENT", StringComparison.OrdinalIgnoreCase))
            { var m = SentimentRegex.Match(line); if (m.Success) snapshot.SentimentTerms.Add(new(NormalizeToken(m.Groups["term"].Value), ParseDouble(m.Groups["pct"].Value))); continue; }

            if (section.Contains("COLOR ANALYSIS", StringComparison.OrdinalIgnoreCase))
            { var m = ColorOrMaterialRegex.Match(line); if (m.Success) snapshot.Colors.Add(new(NormalizeToken(m.Groups["name"].Value), ParseInt(m.Groups["count"].Value), ParseLong(m.Groups["sales"].Value), ParseDouble(m.Groups["sent"].Value))); continue; }

            if (section.Contains("MATERIAL ANALYSIS", StringComparison.OrdinalIgnoreCase))
            { var m = ColorOrMaterialRegex.Match(line); if (m.Success) snapshot.Materials.Add(new(NormalizeToken(m.Groups["name"].Value), ParseInt(m.Groups["count"].Value), ParseLong(m.Groups["sales"].Value), ParseDouble(m.Groups["sent"].Value))); continue; }

            if (section.Contains("PRICE COMPETITIVENESS", StringComparison.OrdinalIgnoreCase))
            { var m = PriceTermRegex.Match(line); if (m.Success) snapshot.PriceTerms.Add(new(NormalizeToken(m.Groups["term"].Value), ParseDouble(m.Groups["price"].Value), ParseDouble(m.Groups["ratio"].Value), m.Groups["tag"].Value.Equals("CHEAPER", StringComparison.OrdinalIgnoreCase), ParseInt(m.Groups["count"].Value))); }
        }

        return snapshot;
    }

    private static IReadOnlyList<MarketKeywordMetric> BuildKeywordIndex(IEnumerable<CategorySnapshot> snapshots)
    {
        var index = new Dictionary<string, KeywordAgg>(StringComparer.OrdinalIgnoreCase);
        foreach (var snap in snapshots)
        {
            foreach (var ctr in snap.CtrTerms)
            { if (!index.TryGetValue(ctr.Term, out var a)) { a = new(ctr.Term); index[ctr.Term] = a; } a.Lift += ctr.Lift; a.TopHits += ctr.Top; a.BottomHits += ctr.Bottom; a.CtrSamples++; }
            foreach (var sent in snap.SentimentTerms)
            { if (!index.TryGetValue(sent.Term, out var a)) { a = new(sent.Term); index[sent.Term] = a; } a.PositivePct += sent.PositivePct; a.SentimentSamples++; }
        }
        return index.Values.Select(a =>
        {
            var lift = a.CtrSamples > 0 ? a.Lift / a.CtrSamples : 0;
            var pct  = a.SentimentSamples > 0 ? a.PositivePct / a.SentimentSamples : 0;
            var conf = Math.Clamp(Math.Log10(a.TopHits + a.BottomHits + 10) / 3.0, 0.0, 1.0);
            var score = Math.Clamp(lift / 250.0, 0.0, 1.0) * 0.55 + Math.Clamp(pct / 100.0, 0.0, 1.0) * 0.35 + conf * 0.10;
            return new MarketKeywordMetric(a.Term, score, lift, pct, a.TopHits, a.BottomHits);
        }).ToArray();
    }

    private static MarketColorMetric[] BuildColorMetricIndex(IEnumerable<MetricTerm> terms, int top)
    {
        var index = new Dictionary<string, MetricAgg>(StringComparer.OrdinalIgnoreCase);
        foreach (var t in terms)
        { if (!index.TryGetValue(t.Name, out var a)) { a = new(t.Name); index[t.Name] = a; } a.ProductCount += t.ProductCount; a.TotalSales += t.TotalSales; a.Sentiment += t.Sentiment; a.Samples++; }
        return index.Values.Select(a => { var s = a.Samples > 0 ? a.Sentiment / a.Samples : a.Sentiment; return new MarketColorMetric(a.Name, MetricScore(a.ProductCount, a.TotalSales, s), a.ProductCount, a.TotalSales, s); })
            .OrderByDescending(x => x.Score).Take(Math.Max(1, top)).ToArray();
    }

    private static double MetricScore(int productCount, long totalSales, double sentiment)
    {
        var c = Math.Clamp(Math.Log10(productCount + 10) / 4.0, 0.0, 1.0);
        var s = Math.Clamp(Math.Log10(totalSales + 10) / 7.0, 0.0, 1.0);
        var e = Math.Clamp(sentiment / 5.0, 0.0, 1.0);
        return c * 0.25 + s * 0.45 + e * 0.30;
    }

    private static IEnumerable<string> ExtractWords(string? text)
    {
        if (string.IsNullOrWhiteSpace(text)) yield break;
        foreach (Match m in WordRegex.Matches(text.ToLowerInvariant()))
            if (m.Success && m.Value.Length >= 3) yield return m.Value;
    }

    private static int    ParseInt   (string v) => int.TryParse(v.Replace(",","",StringComparison.Ordinal), NumberStyles.Integer, CultureInfo.InvariantCulture, out var r) ? r : 0;
    private static long   ParseLong  (string v) => long.TryParse(v.Replace(",","",StringComparison.Ordinal), NumberStyles.Integer, CultureInfo.InvariantCulture, out var r) ? r : 0;
    private static double ParseDouble(string v) => double.TryParse(v.Replace(",","",StringComparison.Ordinal), NumberStyles.Float, CultureInfo.InvariantCulture, out var r) ? r : 0;

    private static string NormalizeToken(string value)
    {
        if (string.IsNullOrWhiteSpace(value)) return string.Empty;
        var t = value.Trim().ToLowerInvariant().Replace("_", " ", StringComparison.Ordinal);
        return Regex.Replace(t, "\\s+", " ").Trim();
    }

    // ─── Inner records ────────────────────────────────────────────────────────
    private sealed record CategorySnapshot(string Name, string Type, int ProductCount)
    {
        public List<CtrTerm>       CtrTerms      { get; } = new();
        public List<SentimentTerm> SentimentTerms{ get; } = new();
        public List<MetricTerm>    Colors        { get; } = new();
        public List<MetricTerm>    Materials     { get; } = new();
        public List<PriceTerm>     PriceTerms    { get; } = new();
    }
    private sealed record CtrTerm      (string Term, double Lift, int Top, int Bottom);
    private sealed record SentimentTerm(string Term, double PositivePct);
    private sealed record MetricTerm   (string Name, int ProductCount, long TotalSales, double Sentiment);
    private sealed record PriceTerm    (string Term, double AvgPrice, double Ratio, bool IsCheaper, int ProductCount);

    private sealed class KeywordAgg
    {
        public KeywordAgg(string term) => Term = term;
        public string Term { get; }
        public double Lift { get; set; }
        public int TopHits { get; set; }
        public int BottomHits { get; set; }
        public int CtrSamples { get; set; }
        public double PositivePct { get; set; }
        public int SentimentSamples { get; set; }
    }
    private sealed class MetricAgg
    {
        public MetricAgg(string name) => Name = name;
        public string Name { get; }
        public int  ProductCount { get; set; }
        public long TotalSales  { get; set; }
        public double Sentiment { get; set; }
        public int  Samples     { get; set; }
    }
}
