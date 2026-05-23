using System.Text.RegularExpressions;
using System.Text.Json;
using SixLabors.Fonts;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Drawing.Processing;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using PrintifyGenerator.ColoringBookGenerator.Models;
using PrintifyGenerator.ColoringBookGenerator.Services.PromptProviders;
using PrintifyGenerator.ColoringBookGenerator.Utilities;

namespace PrintifyGenerator.ColoringBookGenerator.Services
{
    public enum BookTypeCategory
    {
        PictureBook,
        StoryBook,
        PaintByNumbers
    }

    public class FinishingOptions
    {
        public bool ConvertToBlackAndWhite { get; set; } = true;
        public bool ApplyAntialiasing { get; set; } = true;
        public bool AddPageNumbers { get; set; } = true;
        public bool AddTitleOverlay { get; set; } = true;
        public bool AddStoryText { get; set; } = false;
        public bool SaveIntermediateStages { get; set; } = true;
        public int SupersampleFactor { get; set; } = 8;
        public float GaussianBlurSigma { get; set; } = 1f;
        public string OutputSuffix { get; set; } = "";
        public string? TitleText { get; set; } = null;
        public string? StoryText { get; set; } = null;
    }

    public record FontInfo
    {
        public string Name { get; init; } = "";
        public string FilePath { get; init; } = "";
        public string Family { get; init; } = "";
    }

    public class BookFinisher
    {
        private readonly IPromptProvider _promptProvider;
        private readonly BookTypeCategory _bookType;
        private readonly List<FontInfo> _availableFonts = new();
        private readonly FontCollection _fontCollection = new();
        private int _fontIndex = -1;

        private static readonly string[] SystemFontDirs = new[]
        {
            "/usr/share/fonts",
            "/usr/local/share/fonts",
            Environment.GetFolderPath(Environment.SpecialFolder.Fonts),
            Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "Microsoft", "Windows", "Fonts"),
        };

        public BookTypeCategory BookType => _bookType;
        public IReadOnlyList<FontInfo> AvailableFonts => _availableFonts;
        public bool FontsAvailable => _availableFonts.Count > 0;

        public bool ShouldConvertToBlackAndWhite =>
            _bookType == BookTypeCategory.PictureBook || _bookType == BookTypeCategory.PaintByNumbers;

        public BookFinisher(IPromptProvider promptProvider)
        {
            _promptProvider = promptProvider;
            _bookType = promptProvider switch
            {
                PictureBookPromptProvider => BookTypeCategory.PictureBook,
                StoryBookPromptProvider => BookTypeCategory.StoryBook,
                PaintByNumbersPromptProvider => BookTypeCategory.PaintByNumbers,
                _ => BookTypeCategory.PictureBook
            };

            LoadFonts();
        }

        private void LoadFonts()
        {
            var scanned = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            // Project-level fonts directory
            var projectFontDir = Path.Combine(
                AppContext.BaseDirectory, "..", "..", "..", "..", "..", "fonts");
            if (Directory.Exists(projectFontDir))
            {
                ScanFontDirectory(projectFontDir, scanned);
            }
            // Also check from current directory
            var cwdFontDir = Path.Combine(Directory.GetCurrentDirectory(), "fonts");
            if (Directory.Exists(cwdFontDir) && cwdFontDir != projectFontDir)
            {
                ScanFontDirectory(cwdFontDir, scanned);
            }

            // System font directories
            foreach (var dir in SystemFontDirs)
            {
                if (Directory.Exists(dir))
                    ScanFontDirectory(dir, scanned);
            }

            PromptRecorder.Record("BookFinisher", "fonts_loaded",
                $"Found {_availableFonts.Count} fonts: {string.Join(", ", _availableFonts.Select(f => $"{f.Name} ({f.Family})"))}");
        }

        private void ScanFontDirectory(string dir, HashSet<string> scanned)
        {
            try
            {
                foreach (var file in Directory.EnumerateFiles(dir, "*", SearchOption.AllDirectories))
                {
                    var ext = Path.GetExtension(file).ToLowerInvariant();
                    if (ext != ".ttf" && ext != ".otf") continue;
                    if (!scanned.Add(file)) continue;

                    try
                    {
                        var family = _fontCollection.Add(file);
                        _availableFonts.Add(new FontInfo
                        {
                            Name = Path.GetFileNameWithoutExtension(file),
                            FilePath = file,
                            Family = family.Name
                        });
                    }
                    catch { }
                }
            }
            catch { }
        }

        // ── Font selection (round-robin) ───────────────────────────────

        public FontInfo PickNextFont()
        {
            if (_availableFonts.Count == 0)
                throw new InvalidOperationException("No fonts available");

            _fontIndex = (_fontIndex + 1) % _availableFonts.Count;
            var fontInfo = _availableFonts[_fontIndex];
            PromptRecorder.Record("BookFinisher", "font_selected",
                $"Font: {fontInfo.Name} (family: {fontInfo.Family}, file: {fontInfo.FilePath})");
            return fontInfo;
        }

        public FontInfo? PickFontForPage(int pageNumber)
        {
            if (_availableFonts.Count == 0) return null;
            var index = (pageNumber - 1) % _availableFonts.Count;
            return _availableFonts[index];
        }

        private Font? CreateFontForUsage(FontInfo fontInfo, float size, FontStyle style = FontStyle.Regular)
        {
            try
            {
                if (_fontCollection.TryGet(fontInfo.Family, out var family))
                    return family.CreateFont(size, style);
                return null;
            }
            catch
            {
                return null;
            }
        }

        // ── Main finishing pipeline ─────────────────────────────────────

        public async Task<string> FinishImageAsync(
            string inputPath,
            string outputDirectory,
            int? pageNumber = null,
            FinishingOptions? options = null)
        {
            options ??= new FinishingOptions();
            Directory.CreateDirectory(outputDirectory);

            var baseName = Path.GetFileNameWithoutExtension(inputPath);
            var ext = Path.GetExtension(inputPath);
            if (string.IsNullOrEmpty(ext)) ext = ".jpg";

            var finalName = baseName + options.OutputSuffix + ext;
            var finalPath = Path.Combine(outputDirectory, finalName);

            using var image = await Image.LoadAsync<Rgba32>(inputPath);

            var stages = new List<(string stage, string path)>();

            // Stage 1: Save original
            if (options.SaveIntermediateStages)
            {
                var origPath = Path.Combine(outputDirectory, baseName + "_01_original" + ext);
                await image.SaveAsync(origPath);
                stages.Add(("original", origPath));
            }

            // Stage 2: Convert to black and white if applicable
            if (options.ConvertToBlackAndWhite && ShouldConvertToBlackAndWhite)
            {
                image.Mutate(ctx => ctx.Grayscale());
                if (options.SaveIntermediateStages)
                {
                    var grayPath = Path.Combine(outputDirectory, baseName + "_02_grayscale" + ext);
                    await image.SaveAsync(grayPath);
                    stages.Add(("grayscale", grayPath));
                }
            }

            // Stage 3: Antialiasing
            if (options.ApplyAntialiasing)
            {
                var origW = image.Width;
                var origH = image.Height;
                if (origW > 1 && origH > 1 && options.SupersampleFactor > 1)
                {
                    image.Mutate(ctx =>
                    {
                        ctx.Resize(origW * options.SupersampleFactor, origH * options.SupersampleFactor, KnownResamplers.Lanczos3);
                        ctx.GaussianBlur(options.GaussianBlurSigma);
                        ctx.Resize(origW, origH, KnownResamplers.Lanczos3);
                    });
                }
                if (options.SaveIntermediateStages)
                {
                    var aaPath = Path.Combine(outputDirectory, baseName + "_03_antialiased" + ext);
                    await image.SaveAsync(aaPath);
                    stages.Add(("antialiased", aaPath));
                }
            }

            // Stage 3.5: Detect and remove barcode/text artifacts that some generators render
            try
            {
                var removed = DetectAndRemoveBarcodeAndText(image);
                if (removed && options.SaveIntermediateStages)
                {
                    var cleanPath = Path.Combine(outputDirectory, baseName + "_03b_nobarcode" + ext);
                    await image.SaveAsync(cleanPath);
                    stages.Add(("nobarcode", cleanPath));
                }
            }
            catch { }

            // Stage 4: Add text overlay
            bool didTextOverlay = false;
            if (options.AddPageNumbers && pageNumber.HasValue && _availableFonts.Count > 0)
            {
                var fontInfo = PickFontForPage(pageNumber.Value);
                if (fontInfo != null)
                {
                    var font = CreateFontForUsage(fontInfo, 28);
                    if (font != null)
                    {
                        AddPageNumberOverlay(image, pageNumber.Value, font, fontInfo);
                        didTextOverlay = true;
                    }
                }
            }
            if (options.AddStoryText && !string.IsNullOrWhiteSpace(options.StoryText))
            {
                var fontInfo = pageNumber.HasValue ? PickFontForPage(pageNumber.Value) : _availableFonts.FirstOrDefault();
                if (fontInfo != null)
                {
                    var font = CreateFontForUsage(fontInfo, 20);
                    if (font != null)
                    {
                        AddStoryTextOverlay(image, options.StoryText, font);
                        didTextOverlay = true;
                    }
                }
            }
            if (didTextOverlay && options.SaveIntermediateStages)
            {
                var textPath = Path.Combine(outputDirectory, baseName + "_04_text" + ext);
                await image.SaveAsync(textPath);
                stages.Add(("text_overlay", textPath));
            }

            // Stage 5: Save final
            await image.SaveAsync(finalPath);
            stages.Add(("final", finalPath));

            // Save stages manifest
            if (options.SaveIntermediateStages)
            {
                var manifest = stages.Select(s => new { stage = s.stage, file = Path.GetFileName(s.path) }).ToList();
                var manifestPath = Path.Combine(outputDirectory, baseName + "_finishing_manifest.json");
                await File.WriteAllTextAsync(manifestPath,
                    System.Text.Json.JsonSerializer.Serialize(manifest, new System.Text.Json.JsonSerializerOptions { WriteIndented = true }));
            }

            return finalPath;
        }

        // ── Batch finishing ────────────────────────────────────────────

        public async Task<FinishingResult> FinishDirectoryAsync(
            string inputDirectory,
            string? outputDirectory = null,
            FinishingOptions? options = null)
        {
            options ??= new FinishingOptions();
            outputDirectory ??= Path.Combine(inputDirectory, "finished");
            Directory.CreateDirectory(outputDirectory);

            var imageFiles = Directory.GetFiles(inputDirectory, "*.jpg")
                .Concat(Directory.GetFiles(inputDirectory, "*.png"))
                .OrderBy(f => f)
                .ToArray();

            var results = new List<string>();
            int coverCount = 0;
            int pageCount = 0;

            foreach (var file in imageFiles)
            {
                var fileName = Path.GetFileNameWithoutExtension(file);

                if (fileName.Contains("_01_") || fileName.Contains("_02_") ||
                    fileName.Contains("_03_") || fileName.Contains("_04_"))
                    continue;

                int? pageNumber = null;
                var match = Regex.Match(fileName, @"page_(\d+)", RegexOptions.IgnoreCase);
                if (match.Success && int.TryParse(match.Groups[1].Value, out var pn))
                    pageNumber = pn;

                var result = await FinishImageAsync(file, outputDirectory, pageNumber, options);
                results.Add(result);

                if (fileName.Contains("cover", StringComparison.OrdinalIgnoreCase))
                    coverCount++;
                else
                    pageCount++;
            }

            var fontList = string.Join(", ", _availableFonts.Select(f => $"{f.Name} ({f.Family})"));
            PromptRecorder.Record("BookFinisher", "finish_directory_complete",
                $"Processed {results.Count} files ({coverCount} covers, {pageCount} pages). Fonts: {fontList}");

            return new FinishingResult
            {
                Files = results,
                CoverCount = coverCount,
                PageCount = pageCount,
                OutputDirectory = outputDirectory,
                FontsUsed = _availableFonts.Select(f => f.Name).ToList()
            };
        }

        // ── Text overlay ───────────────────────────────────────────────

        private void AddPageNumberOverlay(Image<Rgba32> image, int pageNumber, Font font, FontInfo fontInfo)
        {
            var text = $"{pageNumber}";
            var brush = ShouldConvertToBlackAndWhite
                ? Brushes.Solid(Color.Black)
                : Brushes.Solid(Color.Gray);

            var x = image.Width - 120f;
            var y = image.Height - 70f;

            var bgRect = new RectangleF(x - 10, y, 110, 50);
            image.Mutate(ctx =>
            {
                ctx.Fill(Brushes.Solid(Color.White), bgRect);
                ctx.DrawText(text, font, brush, new PointF(x, y));
            });

            PromptRecorder.Record("BookFinisher", $"page_{pageNumber:D2}_font",
                $"Font: {fontInfo.Name} (family: {fontInfo.Family}, file: {fontInfo.FilePath})");
        }

        public void ApplyStoryTextToFile(string imagePath, string storyText)
        {
            var fi = _availableFonts.FirstOrDefault();
            if (fi == null) return;
            var font = CreateFontForUsage(fi, 20);
            if (font == null) return;
            using var img = Image.Load<Rgba32>(imagePath);
            AddStoryTextOverlay(img, storyText, font);
            img.Save(imagePath);
        }

        private void AddStoryTextOverlay(Image<Rgba32> image, string text, Font font)
        {
            var brush = ShouldConvertToBlackAndWhite
                ? Brushes.Solid(Color.Black)
                : Brushes.Solid(Color.FromRgb(60, 60, 60));

            int margin = (int)(image.Width * 0.04);
            int bottomArea = (int)(image.Height * 0.18);
            int textAreaY = image.Height - bottomArea;
            int maxWidth = image.Width - margin * 2;

            // Wrap text
            var wrapped = WrapText(text, font, maxWidth);
            if (wrapped.Count == 0) return;

            float lineHeight = font.Size * 1.4f;

            // Background bar
            float bgHeight = wrapped.Count * lineHeight + 16;
            var bgRect = new RectangleF(margin - 4, textAreaY + 4, maxWidth + 8, bgHeight);
            image.Mutate(ctx =>
            {
                ctx.Fill(Brushes.Solid(Color.FromRgba(255, 255, 255, 220)), bgRect);
                float curY = textAreaY + 12;
                foreach (var line in wrapped)
                {
                    ctx.DrawText(line, font, brush, new PointF(margin, curY));
                    curY += lineHeight;
                }
            });
        }

        private static List<string> WrapText(string text, Font font, int maxWidthPx)
        {
            var result = new List<string>();
            var words = text.Split(' ');
            var currentLine = "";
            foreach (var word in words)
            {
                var testLine = string.IsNullOrEmpty(currentLine) ? word : currentLine + " " + word;
                var size = TextMeasurer.MeasureSize(testLine, new TextOptions(font));
                if (size.Width > maxWidthPx && !string.IsNullOrEmpty(currentLine))
                {
                    result.Add(currentLine);
                    currentLine = word;
                }
                else
                {
                    currentLine = testLine;
                }
            }
            if (!string.IsNullOrEmpty(currentLine))
                result.Add(currentLine);
            return result;
        }

        private bool DetectAndRemoveBarcodeAndText(Image<Rgba32> image)
        {
            int w = image.Width;
            int h = image.Height;
            if (w < 64 || h < 64) return false;

            int regionH = Math.Max((int)(h * 0.18), 24);
            int yStart = Math.Max(0, h - regionH);

            // Column-wise darkness counts (vertical strokes -> barcode)
            var colCounts = new int[w];
            for (int x = 0; x < w; x++)
            {
                int cnt = 0;
                for (int y = yStart; y < h; y++)
                {
                    var p = image[x, y];
                    float lum = 0.2126f * p.R + 0.7152f * p.G + 0.0722f * p.B;
                    if (lum < 90) cnt++;
                }
                colCounts[x] = cnt;
            }

            int highThresh = (int)Math.Round(regionH * 0.6);
            int runStart = -1;
            var runs = new List<(int s, int e)>();
            for (int x = 0; x < w; x++)
            {
                if (colCounts[x] >= highThresh)
                {
                    if (runStart == -1) runStart = x;
                }
                else
                {
                    if (runStart != -1) { runs.Add((runStart, x - 1)); runStart = -1; }
                }
            }
            if (runStart != -1) runs.Add((runStart, w - 1));

            int totalRunWidth = 0;
            int longestRunWidth = 0;
            int longestRunIndex = -1;
            for (int i = 0; i < runs.Count; i++)
            {
                int width = runs[i].e - runs[i].s + 1;
                totalRunWidth += width;
                if (width > longestRunWidth) { longestRunWidth = width; longestRunIndex = i; }
            }

            if (longestRunIndex != -1 && longestRunWidth >= Math.Max(6, w / 100))
            {
                double totalRunRatio = (double)totalRunWidth / w;
                if (totalRunRatio > 0.02 && totalRunRatio < 0.6)
                {
                    var lr = runs[longestRunIndex];
                    int left = Math.Max(0, lr.s - 6);
                    int right = Math.Min(w - 1, lr.e + 6);
                    int top = Math.Max(0, yStart - (int)(regionH * 0.05));
                    var rect = new Rectangle(left, top, right - left + 1, h - top);
                    image.Mutate(ctx => ctx.Fill(Brushes.Solid(Color.White), rect));
                    PromptRecorder.Record("BookFinisher", "barcode_removed", $"Removed barcode-like area {rect} from image {w}x{h}");
                    return true;
                }
            }

            // Row-wise detection for dense horizontal text bars
            var rowCounts = new int[regionH];
            for (int y = yStart; y < h; y++)
            {
                int cnt = 0;
                for (int x = 0; x < w; x++)
                {
                    var p = image[x, y];
                    float lum = 0.2126f * p.R + 0.7152f * p.G + 0.0722f * p.B;
                    if (lum < 85) cnt++;
                }
                rowCounts[y - yStart] = cnt;
            }

            int rowHigh = (int)Math.Round(w * 0.25);
            int rStart = -1;
            var rRuns = new List<(int s, int e)>();
            for (int i = 0; i < regionH; i++)
            {
                if (rowCounts[i] >= rowHigh)
                {
                    if (rStart == -1) rStart = i;
                }
                else
                {
                    if (rStart != -1) { rRuns.Add((rStart, i - 1)); rStart = -1; }
                }
            }
            if (rStart != -1) rRuns.Add((rStart, regionH - 1));

            foreach (var rr in rRuns)
            {
                int height = rr.e - rr.s + 1;
                if (height <= Math.Max((int)Math.Ceiling(regionH * 0.6), 40))
                {
                    int top = yStart + Math.Max(0, rr.s - 2);
                    int bottom = Math.Min(h - 1, yStart + rr.e + 2);
                    var rect = new Rectangle(0, top, w, bottom - top + 1);
                    image.Mutate(ctx => ctx.Fill(Brushes.Solid(Color.White), rect));
                    PromptRecorder.Record("BookFinisher", "text_removed", $"Removed text-like rows {rr.s}-{rr.e} -> rect {rect}");
                    return true;
                }
            }

            return false;
        }

        public List<string> ValidateImageFile(string imagePath, int? pageNumber = null)
        {
            var issues = new List<string>();
            try
            {
                using var img = Image.Load<Rgba32>(imagePath);

                if (DetectTextOrBarcode(img, out var detail))
                {
                    issues.Add(detail);
                }

                switch (_bookType)
                {
                    case BookTypeCategory.StoryBook:
                        if (IsMostlyGrayscale(img))
                            issues.Add("image is mostly grayscale or low color — expected full-color illustrations");
                        break;
                    case BookTypeCategory.PictureBook:
                        if (!IsLineArt(img))
                            issues.Add("image contains shading or midtones — expected pure line-art suitable for coloring");
                        break;
                    case BookTypeCategory.PaintByNumbers:
                        if (!IsLineArt(img))
                            issues.Add("image contains shading or midtones — expected numbered line art for paint-by-numbers");
                        else
                        {
                            if (!ContainsSmallDarkBlobs(img, minCount: 4))
                                issues.Add("no small numbered-region markers detected — expected numbers inside regions for paint-by-numbers");
                        }
                        break;
                }
            }
            catch (Exception ex)
            {
                issues.Add("validation failed: " + ex.Message);
            }

            return issues;
        }

        private bool DetectTextOrBarcode(Image<Rgba32> image, out string detail)
        {
            detail = string.Empty;
            int w = image.Width;
            int h = image.Height;
            if (w < 64 || h < 64) return false;

            int regionH = Math.Max((int)(h * 0.18), 24);
            int yStart = Math.Max(0, h - regionH);

            // Column-wise darkness counts (vertical barcode-like strokes)
            var colCounts = new int[w];
            for (int x = 0; x < w; x++)
            {
                int cnt = 0;
                for (int y = yStart; y < h; y++)
                {
                    var p = image[x, y];
                    float lum = 0.2126f * p.R + 0.7152f * p.G + 0.0722f * p.B;
                    if (lum < 90) cnt++;
                }
                colCounts[x] = cnt;
            }

            int highThresh = (int)Math.Round(regionH * 0.6);
            int runStart = -1;
            var runs = new List<(int s, int e)>();
            for (int x = 0; x < w; x++)
            {
                if (colCounts[x] >= highThresh)
                {
                    if (runStart == -1) runStart = x;
                }
                else
                {
                    if (runStart != -1) { runs.Add((runStart, x - 1)); runStart = -1; }
                }
            }
            if (runStart != -1) runs.Add((runStart, w - 1));

            int totalRunWidth = 0;
            int longestRunWidth = 0;
            int longestRunIndex = -1;
            for (int i = 0; i < runs.Count; i++)
            {
                int width = runs[i].e - runs[i].s + 1;
                totalRunWidth += width;
                if (width > longestRunWidth) { longestRunWidth = width; longestRunIndex = i; }
            }

            if (longestRunIndex != -1 && longestRunWidth >= Math.Max(6, w / 100))
            {
                double totalRunRatio = (double)totalRunWidth / w;
                if (totalRunRatio > 0.02 && totalRunRatio < 0.6)
                {
                    detail = "barcode-like vertical bars detected near the bottom of the image";
                    return true;
                }
            }

            // Row-wise detection for dense horizontal text bars
            var rowCounts = new int[regionH];
            for (int y = yStart; y < h; y++)
            {
                int cnt = 0;
                for (int x = 0; x < w; x++)
                {
                    var p = image[x, y];
                    float lum = 0.2126f * p.R + 0.7152f * p.G + 0.0722f * p.B;
                    if (lum < 85) cnt++;
                }
                rowCounts[y - yStart] = cnt;
            }

            int rowHigh = (int)Math.Round(w * 0.25);
            int rStart = -1;
            var rRuns = new List<(int s, int e)>();
            for (int i = 0; i < regionH; i++)
            {
                if (rowCounts[i] >= rowHigh)
                {
                    if (rStart == -1) rStart = i;
                }
                else
                {
                    if (rStart != -1) { rRuns.Add((rStart, i - 1)); rStart = -1; }
                }
            }
            if (rStart != -1) rRuns.Add((rStart, regionH - 1));

            foreach (var rr in rRuns)
            {
                int height = rr.e - rr.s + 1;
                if (height <= Math.Max((int)Math.Ceiling(regionH * 0.6), 40))
                {
                    detail = "text-like dense dark rows detected near bottom of image";
                    return true;
                }
            }

            return false;
        }

        private bool IsMostlyGrayscale(Image<Rgba32> image)
        {
            int w = image.Width;
            int h = image.Height;
            long count = 0;
            long total = 0;
            int stepX = Math.Max(1, w / 200);
            int stepY = Math.Max(1, h / 200);
            for (int y = 0; y < h; y += stepY)
            {
                for (int x = 0; x < w; x += stepX)
                {
                    var p = image[x, y];
                    float r = p.R / 255f, g = p.G / 255f, b = p.B / 255f;
                    float mx = Math.Max(r, Math.Max(g, b));
                    float mn = Math.Min(r, Math.Min(g, b));
                    float sat = mx == 0 ? 0 : (mx - mn) / mx;
                    if (sat < 0.08f) count++;
                    total++;
                }
            }
            if (total == 0) return true;
            return ((double)count / total) > 0.9; // >90% low saturation -> grayscale
        }

        private bool IsLineArt(Image<Rgba32> image)
        {
            int w = image.Width;
            int h = image.Height;
            long midCount = 0;
            long total = 0;
            int stepX = Math.Max(1, w / 250);
            int stepY = Math.Max(1, h / 250);
            for (int y = 0; y < h; y += stepY)
            {
                for (int x = 0; x < w; x += stepX)
                {
                    var p = image[x, y];
                    float lum = 0.2126f * p.R + 0.7152f * p.G + 0.0722f * p.B;
                    if (lum > 30 && lum < 225) midCount++;
                    total++;
                }
            }
            if (total == 0) return false;
            var midRatio = (double)midCount / total;
            return midRatio < 0.08; // less than 8% midtones -> likely line art
        }

        private bool ContainsSmallDarkBlobs(Image<Rgba32> image, int minCount)
        {
            int w = image.Width;
            int h = image.Height;
            var visited = new bool[w, h];
            int found = 0;
            int threshold = Math.Max(10, (w * h) / 2000); // adaptive

            for (int y = 0; y < h; y += 3)
            {
                for (int x = 0; x < w; x += 3)
                {
                    if (visited[x, y]) continue;
                    var p = image[x, y];
                    float lum = 0.2126f * p.R + 0.7152f * p.G + 0.0722f * p.B;
                    if (lum >= 100) { visited[x, y] = true; continue; }

                    // BFS flood fill up to area limit
                    var q = new Queue<(int x, int y)>();
                    q.Enqueue((x, y));
                    visited[x, y] = true;
                    int area = 0;
                    int minX = x, minY = y, maxX = x, maxY = y;
                    while (q.Count > 0 && area <= 5000)
                    {
                        var (cx, cy) = q.Dequeue();
                        area++;
                        minX = Math.Min(minX, cx); minY = Math.Min(minY, cy);
                        maxX = Math.Max(maxX, cx); maxY = Math.Max(maxY, cy);
                        for (int ny = Math.Max(0, cy - 1); ny <= Math.Min(h - 1, cy + 1); ny++)
                        for (int nx = Math.Max(0, cx - 1); nx <= Math.Min(w - 1, cx + 1); nx++)
                        {
                            if (visited[nx, ny]) continue;
                            var pp = image[nx, ny];
                            float l2 = 0.2126f * pp.R + 0.7152f * pp.G + 0.0722f * pp.B;
                            if (l2 < 120)
                            {
                                visited[nx, ny] = true;
                                q.Enqueue((nx, ny));
                            }
                            else
                                visited[nx, ny] = true;
                        }
                    }

                    int bw = maxX - minX + 1;
                    int bh = maxY - minY + 1;
                    if (area > 3 && area < 3000 && bw <= 60 && bh <= 60)
                    {
                        found++;
                        if (found >= minCount) return true;
                    }
                }
            }

            return false;
        }

        // ── Generate page number text content ──────────────────────────

        public async Task<string> GeneratePageNumberTextContentAsync(
            string outputDirectory,
            int totalPages,
            string? title = null)
        {
            var lines = new List<string>
            {
                $"Book Type: {_promptProvider.BookType}",
                $"Total Pages: {totalPages}",
                $"Fonts Available: {_availableFonts.Count}",
                $"Generated: {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC",
                ""
            };

            lines.Add("Available Fonts:");
            foreach (var font in _availableFonts)
            {
                lines.Add($"  [{_availableFonts.IndexOf(font) + 1}] {font.Name} (family: {font.Family})");
            }

            if (!string.IsNullOrWhiteSpace(title))
            {
                lines.Add("");
                lines.Add($"Title: {title}");
            }

            lines.Add("");
            lines.Add("Page Number Reference:");
            for (int i = 1; i <= totalPages; i++)
            {
                var fi = PickFontForPage(i);
                var fontName = fi?.Name ?? "none";
                lines.Add($"  Page {i:D2} -> {fontName}");
            }

            var content = string.Join(Environment.NewLine, lines);
            var path = Path.Combine(outputDirectory, "page_number_reference.txt");
            await File.WriteAllTextAsync(path, content);
            return content;
        }

        // ── Generate title text content ────────────────────────────────

        public async Task<string> GenerateTitleTextContentAsync(
            string outputDirectory,
            string title,
            string theme,
            string description,
            string[] tags)
        {
            var lines = new List<string>
            {
                "═" + new string('═', 58) + "═",
                $"  Title: {title}",
                $"  Theme: {theme}",
                $"  Book Type: {_promptProvider.BookType}",
                "═" + new string('═', 58) + "═",
                "",
                "Description:",
                description,
                "",
                "Tags:",
            };

            foreach (var tag in tags)
                lines.Add($"  - {tag}");

            lines.Add("");
            lines.Add("Fonts Used:");
            foreach (var font in _availableFonts)
                lines.Add($"  - {font.Name} ({font.Family})");

            lines.Add("");
            lines.Add($"Generated: {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC");

            var content = string.Join(Environment.NewLine, lines);
            var path = Path.Combine(outputDirectory, "product_metadata.txt");
            await File.WriteAllTextAsync(path, content);
            return content;
        }

        // ── Generate product text content (full text output) ───────────

        public async Task<string> GenerateProductTextContentAsync(
            string outputDirectory,
            string title,
            string theme,
            string[] pageSubjects,
            FinishingOptions? options = null)
        {
            options ??= new FinishingOptions();
            var lines = new List<string>
            {
                "╔" + new string('═', 58) + "╗",
                "  PRODUCT TEXT CONTENT",
                "╚" + new string('═', 58) + "╝",
                "",
                $"Title: {title}",
                $"Theme: {theme}",
                $"Book Type: {_promptProvider.BookType}",
                $"Black & White: {options.ConvertToBlackAndWhite && ShouldConvertToBlackAndWhite}",
                $"Antialiasing: {options.ApplyAntialiasing}",
                $"Page Numbers: {options.AddPageNumbers}",
                $"Title Overlay: {options.AddTitleOverlay}",
                ""
            };

            lines.Add("═" + new string('═', 58) + "═");
            lines.Add("  FONTS");
            lines.Add("═" + new string('═', 58) + "═");
            foreach (var font in _availableFonts)
                lines.Add($"  {font.Name} (family: {font.Family}, file: {font.FilePath})");
            lines.Add("");

            lines.Add("═" + new string('═', 58) + "═");
            lines.Add("  PAGE SUBJECTS");
            lines.Add("═" + new string('═', 58) + "═");
            lines.Add("");
            for (int i = 0; i < pageSubjects.Length; i++)
            {
                var fi = PickFontForPage(i + 1);
                var fontName = fi?.Name ?? "none";
                lines.Add($"Page {i + 1:D2}: ({fontName}) {pageSubjects[i]}");
            }

            lines.Add("");
            lines.Add("═" + new string('═', 58) + "═");
            lines.Add("  FINISHING MANIFEST");
            lines.Add("═" + new string('═', 58) + "═");
            lines.Add("");
            lines.Add($"Generated: {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC");

            var content = string.Join(Environment.NewLine, lines);
            var path = Path.Combine(outputDirectory, "product_text_content.txt");
            await File.WriteAllTextAsync(path, content);
            return content;
        }

        // ── Save job manifest ──────────────────────────────────────────

        public async Task<string> SaveJobManifestAsync(
            string outputDirectory,
            string title,
            string theme,
            List<GenerationJob> jobs)
        {
            var manifest = new
            {
                title,
                theme,
                bookType = _promptProvider.BookType,
                jobCount = jobs.Count,
                fontsAvailable = _availableFonts.Select(f => new { f.Name, f.Family, f.FilePath }).ToList(),
                generatedAt = DateTime.UtcNow,
                jobs = jobs.Select(j => new
                {
                    j.JobId,
                    j.PageLabel,
                    j.PageNumber,
                    j.IsCover,
                    j.IsFrontCover,
                    j.IsBackCover,
                    j.AspectRatio,
                    j.BookType,
                    j.Title,
                    j.Theme,
                    j.StyleAddon,
                    j.Subject,
                    j.StoryText,
                    j.TitleOverlayText,
                    j.FooterText,
                    j.ConvertToBlackAndWhite,
                    j.Antialias,
                    j.AddPageNumberOverlay,
                    j.FinisherFontName,
                    j.FinisherFontFamily,
                    j.OutputFileName,
                    j.OutputPath,
                    promptPreview = j.Prompt.Length > 200 ? j.Prompt[..200] + "..." : j.Prompt
                }).ToList()
            };

            var json = JsonSerializer.Serialize(manifest, new JsonSerializerOptions
            {
                WriteIndented = true,
                DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
            });

            var path = Path.Combine(outputDirectory, "job_manifest.json");
            await File.WriteAllTextAsync(path, json);
            PromptRecorder.Record("BookFinisher", "job_manifest_saved", $"Saved {jobs.Count} jobs to {path}");
            return path;
        }
    }

    public class FinishingResult
    {
        public List<string> Files { get; set; } = new();
        public int CoverCount { get; set; }
        public int PageCount { get; set; }
        public string OutputDirectory { get; set; } = "";
        public List<string> FontsUsed { get; set; } = new();
    }
}
