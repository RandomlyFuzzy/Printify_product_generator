using System.Text.Json;

namespace PrintifyGenerator.Library.VectorStorage;

public record CategorySnapshot(
    string Name,
    string Type,
    int ProductCount,
    List<CtrTerm> CtrTerms,
    List<SentimentTerm> SentimentTerms,
    List<ColorMetric> ColorMetrics,
    List<MaterialMetric> MaterialMetrics,
    List<PriceTerm> PriceTerms
);

public record CtrTerm(string Term, double Lift, int TopCount, int BottomCount);
public record SentimentTerm(string Term, double PositivePct, int ProductCount);
public record ColorMetric(string Name, int ProductCount, double TotalSales, double Sentiment, int SampleCount, double Score);
public record MaterialMetric(string Name, int ProductCount, double TotalSales, double Sentiment, int SampleCount, double Score);
public record PriceTerm(string Term, double AvgPrice, double Ratio, bool IsCheaper, int ProductCount);

public class MarketDataEmbedder
{
    private readonly VectorStore _store;
    private readonly string _categoryFeaturesDir;

    public MarketDataEmbedder(VectorStore store, string categoryFeaturesDir)
    {
        _store = store;
        _categoryFeaturesDir = categoryFeaturesDir;
    }

    private HashSet<string> GetExistingMarketConcepts()
    {
        return [.. _store.GetAllRecords()
            .Where(r => r.Source == "market_data")
            .Select(r => r.Concept)];
    }

    public int EmbedCategoryFiles()
    {
        if (!Directory.Exists(_categoryFeaturesDir))
            return 0;

        var existing = GetExistingMarketConcepts();
        int count = 0;
        foreach (var file in Directory.GetFiles(_categoryFeaturesDir, "*.txt"))
        {
            try
            {
                var snapshot = ParseCategoryFile(file);
                if (snapshot == null) continue;

                string concept = $"market:{snapshot.Name}";
                if (existing.Contains(concept)) continue;

                var text = SnapshotToText(snapshot);
                _store.Store(
                    concept,
                    text,
                    [], // embedding will be computed separately via Ollama
                    CalculateScore(snapshot),
                    source: "market_data"
                );
                count++;
            }
            catch
            {
                // skip malformed files
            }
        }

        return count;
    }

    public string GenerateSearchText(string query, string? category = null, string? color = null, string? material = null)
    {
        var parts = new List<string> { query };
        if (category != null) parts.Add($"category:{category}");
        if (color != null) parts.Add($"color:{color}");
        if (material != null) parts.Add($"material:{material}");
        return string.Join(" ", parts);
    }

    public static string SnapshotToText(CategorySnapshot snap)
    {
        var lines = new List<string>
        {
            $"Category: {snap.Name}",
            $"Type: {snap.Type}",
            $"Products: {snap.ProductCount}"
        };

        if (snap.CtrTerms.Count > 0)
        {
            var topCtr = snap.CtrTerms.OrderByDescending(t => t.Lift).Take(5);
            lines.Add("Top CTR keywords: " + string.Join(", ", topCtr.Select(t => $"{t.Term}(+{t.Lift:F1}%)")));
        }

        if (snap.SentimentTerms.Count > 0)
        {
            var topSent = snap.SentimentTerms.OrderByDescending(t => t.PositivePct).Take(5);
            lines.Add("Top sentiment keywords: " + string.Join(", ", topSent.Select(t => $"{t.Term}({t.PositivePct:F1}%)")));
        }

        if (snap.ColorMetrics.Count > 0)
        {
            var topColors = snap.ColorMetrics.OrderByDescending(c => c.Score).Take(5);
            lines.Add("Top colors: " + string.Join(", ", topColors.Select(c => $"{c.Name}({c.ProductCount} products, {c.TotalSales:F0} sales)")));
        }

        if (snap.MaterialMetrics.Count > 0)
        {
            var topMats = snap.MaterialMetrics.OrderByDescending(m => m.Score).Take(5);
            lines.Add("Top materials: " + string.Join(", ", topMats.Select(m => $"{m.Name}({m.ProductCount} products, {m.TotalSales:F0} sales)")));
        }

        if (snap.PriceTerms.Count > 0)
        {
            var topPrices = snap.PriceTerms.OrderBy(t => t.Ratio).Take(5);
            lines.Add("Price guidance: " + string.Join(", ", topPrices.Select(p => $"${p.AvgPrice:F2}{(p.IsCheaper ? " [cheaper]" : "")}")));
        }

        return string.Join("\n", lines);
    }

    public List<string> GetCategories()
    {
        if (!Directory.Exists(_categoryFeaturesDir))
            return [];

        return Directory.GetFiles(_categoryFeaturesDir, "*.txt")
            .Select(Path.GetFileNameWithoutExtension)
            .Where(n => n != null)
            .Select(n => n!)
            .ToList();
    }

    private static CategorySnapshot? ParseCategoryFile(string path)
    {
        var lines = File.ReadAllLines(path);
        if (lines.Length == 0) return null;

        string? name = null, type = null;
        int productCount = 0;
        var ctrTerms = new List<CtrTerm>();
        var sentTerms = new List<SentimentTerm>();
        var colors = new List<ColorMetric>();
        var materials = new List<MaterialMetric>();
        var prices = new List<PriceTerm>();
        string currentSection = "";

        foreach (var line in lines)
        {
            if (line.StartsWith("===") && line.EndsWith("==="))
            {
                var header = line.Trim('=', ' ');
                if (header.Contains('(') && header.Contains("products"))
                {
                    var namePart = header.Split('(')[0].Trim();
                    name = namePart;
                    var countMatch = System.Text.RegularExpressions.Regex.Match(header, @"(\d[\d,]*)\s+products");
                    if (countMatch.Success)
                        productCount = int.Parse(countMatch.Groups[1].Value.Replace(",", ""));
                    var typeMatch = System.Text.RegularExpressions.Regex.Match(header, @"Type:\s*(\w+)");
                    if (typeMatch.Success)
                        type = typeMatch.Groups[1].Value;
                }
                else if (header.Contains("CTR")) currentSection = "ctr";
                else if (header.Contains("SENTIMENT")) currentSection = "sentiment";
                else if (header.Contains("COLOR")) currentSection = "color";
                else if (header.Contains("MATERIAL")) currentSection = "material";
                else if (header.Contains("PRICE")) currentSection = "price";
                continue;
            }

            if (string.IsNullOrWhiteSpace(line) || line.StartsWith("---")) continue;

            switch (currentSection)
            {
                case "ctr":
                    var ctrMatch = System.Text.RegularExpressions.Regex.Match(line,
                        @"\s*(\w[\w\s]*?):\s*([+-]?\d+\.?\d*)%\s*CTR\s*lift\s*\((\d+)\s+in\s+top\s*/\s*(\d+)\s+in\s+bottom\)");
                    if (ctrMatch.Success)
                        ctrTerms.Add(new CtrTerm(
                            ctrMatch.Groups[1].Value.Trim(),
                            double.Parse(ctrMatch.Groups[2].Value),
                            int.Parse(ctrMatch.Groups[3].Value),
                            int.Parse(ctrMatch.Groups[4].Value)
                        ));
                    break;

                case "sentiment":
                    var sentMatch = System.Text.RegularExpressions.Regex.Match(line,
                        @"\s*([\w\s]+?):\s*(\d+\.?\d*)%\s*positive\s*\((\d+)\s+products\)");
                    if (sentMatch.Success)
                        sentTerms.Add(new SentimentTerm(
                            sentMatch.Groups[1].Value.Trim(),
                            double.Parse(sentMatch.Groups[2].Value),
                            int.Parse(sentMatch.Groups[3].Value)
                        ));
                    break;

                case "color":
                    var colorMatch = System.Text.RegularExpressions.Regex.Match(line,
                        @"\s*([\w\s]+):\s*([\d,]+)\s+products,\s*([\d.]+)\s+avg\s+sentiment,\s*([\d,]+)\s+total\s+sales");
                    if (colorMatch.Success)
                        colors.Add(new ColorMetric(
                            colorMatch.Groups[1].Value.Trim(),
                            int.Parse(colorMatch.Groups[2].Value.Replace(",", "")),
                            double.Parse(colorMatch.Groups[4].Value.Replace(",", "")),
                            double.Parse(colorMatch.Groups[3].Value),
                            int.Parse(colorMatch.Groups[2].Value.Replace(",", "")),
                            0
                        ));
                    break;

                case "material":
                    var matMatch = System.Text.RegularExpressions.Regex.Match(line,
                        @"\s*([\w\s]+):\s*([\d,]+)\s+products,\s*([\d.]+)\s+avg\s+sentiment,\s*([\d,]+)\s+total\s+sales");
                    if (matMatch.Success)
                        materials.Add(new MaterialMetric(
                            matMatch.Groups[1].Value.Trim(),
                            int.Parse(matMatch.Groups[2].Value.Replace(",", "")),
                            double.Parse(matMatch.Groups[4].Value.Replace(",", "")),
                            double.Parse(matMatch.Groups[3].Value),
                            int.Parse(matMatch.Groups[2].Value.Replace(",", "")),
                            0
                        ));
                    break;

                case "price":
                    var priceMatch = System.Text.RegularExpressions.Regex.Match(line,
                        @"\s*([\w\s]+):\s*\$?([\d.]+)\s+avg\s*\(ratio:\s*([\d.]+)\)\s*-\s*(CHEAPER|PRICIER)\s*\((\d+)\s+products\)");
                    if (priceMatch.Success)
                        prices.Add(new PriceTerm(
                            priceMatch.Groups[1].Value.Trim(),
                            double.Parse(priceMatch.Groups[2].Value),
                            double.Parse(priceMatch.Groups[3].Value),
                            priceMatch.Groups[4].Value == "CHEAPER",
                            int.Parse(priceMatch.Groups[5].Value)
                        ));
                    break;
            }
        }

        if (name == null) return null;

        return new CategorySnapshot(
            name, type ?? "Unknown", productCount,
            ctrTerms, sentTerms, colors, materials, prices
        );
    }

    private static float CalculateScore(CategorySnapshot snap)
    {
        float score = 5f;

        if (snap.ProductCount > 1000) score += 2;
        else if (snap.ProductCount > 500) score += 1;
        else if (snap.ProductCount < 50) score -= 1;

        if (snap.CtrTerms.Any(t => t.Lift > 10)) score += 1;
        if (snap.SentimentTerms.Any(t => t.PositivePct > 80)) score += 1;

        if (snap.PriceTerms.Any(p => p.IsCheaper)) score += 0.5f;

        return Math.Clamp(score, 0, 10);
    }
}
