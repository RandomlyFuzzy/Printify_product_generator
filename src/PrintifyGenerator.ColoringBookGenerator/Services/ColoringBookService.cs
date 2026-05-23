using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using System.Text.RegularExpressions;
using PrintifyGenerator.ColoringBookGenerator.Models;
using PrintifyGenerator.ColoringBookGenerator.Utilities;
using PrintifyGenerator.ColoringBookGenerator.Services.PromptProviders;

namespace PrintifyGenerator.ColoringBookGenerator.Services
{
    public class ColoringBookService
    {
        private BlueprintSpec Bp => _promptProvider.Blueprint;
        private int BlueprintId     => Bp.BlueprintId;
        private int PrintProviderId => Bp.PrintProviderId;
        private int DefaultVariantId => Bp.DefaultVariantId;
        private int CoverWidth  => Bp.CoverWidth;
        private int CoverHeight => Bp.CoverHeight;
        private int PageWidth   => Bp.PageWidth;
        private int PageHeight  => Bp.PageHeight;
        private int PageCount   => Bp.PageCount;
        private int UniqueInteriorImages => 2 + ((PageCount - 2) / 2);
        private int ExtraPageCandidates => UniqueInteriorImages / 4;

        private readonly IImageGenerator _imageGen;
        private readonly PrintifyClient  _printify;
        private readonly int             _shopId;
        private readonly string          _ollamaUrl;
        private readonly string          _ollamaModel;
        private readonly IPromptProvider _promptProvider;
        private readonly BookFinisher    _finisher;

        private readonly List<GenerationJob> _jobs = new();

        private record PageSubject(string Scene, string OverlayLeft, string? OverlayRight = null, string? PageTextLeft = null, string? PageTextRight = null);

        public ColoringBookService(
            IImageGenerator imageGen,
            PrintifyClient printify,
            int shopId,
            IPromptProvider promptProvider,
            string ollamaUrl   = "http://192.168.0.181:11434",
            string ollamaModel = "gemma4:e2b")
        {
            _imageGen    = imageGen;
            _printify    = printify;
            _shopId      = shopId;
            _promptProvider = promptProvider;
            _ollamaUrl   = ollamaUrl;
            _ollamaModel = ollamaModel;
            _finisher    = new BookFinisher(promptProvider);
        }

        // ── Entry point ────────────────────────────────────────────────────────

        public async Task<string> GenerateAndPublishAsync(
            string title,
            string theme,
            string styleAddon,
            Action<string>? onProgress = null,
            Action<int, int>? onPageProgress = null)
        {
            // Create an output directory named with the sanitized title and timestamp
            var sanitizedTitle = SanitizeTitleForFolder(title);
            var outputDir = Path.Combine("output", $"{sanitizedTitle}_{DateTime.UtcNow:yyyyMMdd_HHmmss}");
            Directory.CreateDirectory(outputDir);
            // Save any prompts recorded so far (e.g. Ollama prompts generated before the output dir existed)
            await PromptRecorder.SaveToDirectoryAsync(outputDir);

            // ── 1. Generate all page subjects ─────────────────────────────────────
            int pairCount = (PageCount - 2) / 2;
            string page1Subject = "";
            string page24Subject = "";
            string[]? orderedSpreadSubjects = null;
            Task<PageSubject>[]? pagePairSubjectTasks = null;
            Task<PageSubject>? page1SubjectTask = null;
            Task<PageSubject>? page24SubjectTask = null;

            var storyPrompt = _promptProvider.BuildFullStoryPrompt(theme);
            if (storyPrompt != null)
            {
                onProgress?.Invoke("Generating a continuous story for all pages...");
                var allSubjects = await GenerateFullStoryAsync(theme, storyPrompt);
                page1Subject = allSubjects[0];
                page24Subject = allSubjects[pairCount + 1];
                orderedSpreadSubjects = allSubjects[1..(pairCount + 1)];
            }
            else
            {
                onProgress?.Invoke($"Queuing {pairCount} 16:9 page-subject requests and 2 3:4 requests to Ollama...");
                pagePairSubjectTasks = Enumerable.Range(1, pairCount)
                    .Select(pairNum => GeneratePageSubjectAsync(theme, pairNum + 1))
                    .ToArray();
                page1SubjectTask = GeneratePageSubjectAsync(theme, 1);
                page24SubjectTask = GeneratePageSubjectAsync(theme, PageCount);
            }

            // ── 2. Generate covers while Ollama works in the background ────────
            onProgress?.Invoke("Generating back cover (left side)...");
            var backPath = await GenerateAndValidateCoverAsync(outputDir, isFront: false, title: title, theme: theme, styleAddon: styleAddon);

            onProgress?.Invoke("Generating front cover (right side)...");
            var frontPath = await GenerateAndValidateCoverAsync(outputDir, isFront: true, title: title, theme: theme, styleAddon: styleAddon);

            onProgress?.Invoke("Stitching cover spread (back-left + front-right)...");
            var coverPath = StitchCoverSpread(backPath, frontPath, outputDir);

            // ── 3. Generate each page: cover, page 1, spreads (2-3, 4-5, ...), page 24
            onProgress?.Invoke($"Generating {pairCount} spread images and 2 single images for first/last page...");
            var pagePaths = new List<string>(PageCount);
            var pageSubjects = new Dictionary<int, string>();
            var pageOverlays = new List<string>();
            var pageTexts = new List<string>();

            // Page 1: centered 3:4
            PageSubject? p1Result = null;
            if (page1SubjectTask != null) p1Result = await page1SubjectTask;
            page1Subject = p1Result?.Scene ?? page1Subject;
            pageSubjects[1] = page1Subject;
            Console.WriteLine($"Generating single 3:4 image for page 1/{PageCount} with subject: {page1Subject}");
            var page1Path = await GenerateAndProcessSinglePageAsync(outputDir, 1, page1Subject, styleAddon, onProgress);
            Console.WriteLine($"Saved page 1 -> {page1Path}");
            pagePaths.Add(page1Path);
            pageOverlays.Add(p1Result?.OverlayLeft ?? "");
            pageTexts.Add(p1Result?.PageTextLeft ?? "");
            onPageProgress?.Invoke(1, PageCount);

            // Spreads: pages 2-3, 4-5, ..., 22-23
            int spreadNum = 1;
            for (int page = 2; page < PageCount; page += 2, spreadNum++)
            {
                string subject;
                PageSubject? ps = null;
                if (orderedSpreadSubjects != null)
                {
                    subject = orderedSpreadSubjects[spreadNum - 1];
                }
                else
                {
                    var subjectTask = await Task.WhenAny(pagePairSubjectTasks!);
                    ps = await subjectTask;
                    pagePairSubjectTasks = pagePairSubjectTasks!.Where(t => t != subjectTask).ToArray();
                    subject = ps?.Scene ?? "";
                }
                pageSubjects[page] = subject;
                pageSubjects[page + 1] = subject;
                Console.WriteLine($"Generating spread {spreadNum}/{pairCount} — pages {page}-{page+1} with subject: {subject}");
                var (leftFile, rightFile) = await GenerateAndProcessSpreadAsync(outputDir, page, subject, styleAddon, onProgress);
                Console.WriteLine($"Saved pages {page}-{page+1} -> {leftFile}, {rightFile}");
                pagePaths.Add(leftFile);
                pageOverlays.Add(ps?.OverlayLeft ?? "");
                pageTexts.Add(ps?.PageTextLeft ?? "");
                pagePaths.Add(rightFile);
                pageOverlays.Add(ps?.OverlayRight ?? ps?.OverlayLeft ?? "");
                pageTexts.Add(ps?.PageTextRight ?? ps?.PageTextLeft ?? "");
                onPageProgress?.Invoke(page + 1, PageCount);
            }

            // Page 24: centered 3:4 (scale-to-fit + pad to avoid destructive cropping)
            PageSubject? p24Result = null;
            if (page24SubjectTask != null) p24Result = await page24SubjectTask;
            page24Subject = p24Result?.Scene ?? page24Subject;
            Console.WriteLine($"Generating single 3:4 image for page {PageCount}/{PageCount} with subject: {page24Subject}");
            var page24Path = await _imageGen.GeneratePageAsync(outputDir, PageCount, page24Subject, styleAddon);
            using (var img = Image.Load<Rgba32>(page24Path))
            {
                double scale = Math.Min((double)PageWidth / Math.Max(1, img.Width), (double)PageHeight / Math.Max(1, img.Height));
                int scaledW = Math.Max(1, (int)Math.Round(img.Width * scale));
                int scaledH = Math.Max(1, (int)Math.Round(img.Height * scale));

                using var canvas24 = new Image<Rgba32>(PageWidth, PageHeight, new Rgba32(255, 255, 255));
                using var resized24 = img.Clone(ctx => ctx.Resize(scaledW, scaledH, KnownResamplers.Lanczos3));
                int offsetX24 = (PageWidth - scaledW) / 2;
                int offsetY24 = (PageHeight - scaledH) / 2;
                canvas24.Mutate(ctx => ctx.DrawImage(resized24, new Point(offsetX24, offsetY24), 1f));
                canvas24.SaveAsJpeg(page24Path);
            }
            Console.WriteLine($"Saved page {PageCount} -> {page24Path}");
            pagePaths.Add(page24Path);
            pageOverlays.Add(p24Result?.OverlayLeft ?? "");
            pageTexts.Add(p24Result?.PageTextLeft ?? "");
            onPageProgress?.Invoke(PageCount, PageCount);

            // ── 3. Upload only the cover spread and generate a few extra
            // candidate pages (don't upload front/back separately). We'll
            // keep the original 24 pages but generate extras and swap in
            // replacements if a primary page looks bad.
            onProgress?.Invoke("Uploading cover spread to Printify...");
            var coverUpload = await _printify.UploadImageFromFileAsync(coverPath);

            // Skipping extra candidate pages and best-effort replacement for paired 16:9 mode

            // Now upload the finalized 24 interior pages (only these are sent).
            // Each page is already sized to PageWidth x PageHeight.
            var pageUploadIds = new List<string>(PageCount);
            for (int i = 0; i < PageCount; i++)
            {
                onProgress?.Invoke($"Uploading page {i + 1}/{PageCount} to Printify...");
                var upload = await _printify.UploadImageFromFileAsync(pagePaths[i]);
                pageUploadIds.Add(upload.Id);
            }

            // Build cumulative page texts by concatenating previous page texts (page 1..N)
            var cumulativePageTexts = new List<string>(pageTexts.Count);
            for (int i = 0; i < pageTexts.Count; i++)
            {
                var parts = pageTexts.Take(i + 1).Where(s => !string.IsNullOrWhiteSpace(s));
                cumulativePageTexts.Add(string.Join(" ", parts).Trim());
            }

            // ── Overlay story text on page images (for story books) ──────
            bool isStoryBook = _promptProvider.BookType.Contains("Story", StringComparison.OrdinalIgnoreCase);
            if (isStoryBook)
            {
                onProgress?.Invoke("Laying story text onto page images...");
                for (int i = 0; i < pagePaths.Count && i < pageTexts.Count; i++)
                {
                    var pageText = pageTexts[i]?.Trim();
                    if (string.IsNullOrWhiteSpace(pageText)) continue;
                    _finisher.ApplyStoryTextToFile(pagePaths[i], pageText);
                }
            }

            // ── 4. Resolve blueprint variants from Printify (fall back to configured constant) ──
            onProgress?.Invoke("Resolving blueprint variant IDs from Printify...");
            int selectedVariantId = DefaultVariantId;
            List<Variant>? resolvedVariants = null;
            var availableVariantIds = new List<int> { DefaultVariantId };
            try
            {
                var variantResp = await _printify.GetBlueprintVariantsAsync(BlueprintId, PrintProviderId);
                if (variantResp?.Variants != null && variantResp.Variants.Count > 0)
                {
                    // Keep the full variant list so we can populate the create payload correctly
                    resolvedVariants = variantResp.Variants;
                    selectedVariantId = resolvedVariants[0].Id;
                    availableVariantIds = resolvedVariants.Select(v => v.Id).Distinct().ToList();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Warning: failed to fetch blueprint variants: {ex.Message}. Using fallback variant {DefaultVariantId}.");
            }

            // ── 5. Build print areas using the resolved variant ids.
            // Printify requires ALL placeholders for a given variant to be inside ONE
            // print_area entry. Splitting the same variant across multiple print_areas
            // triggers a validation error (code 8251). We group by identical position
            // sets so variants with the same placeholders share a single PrintArea.
            List<PrintArea> printAreas;

            if (resolvedVariants != null && resolvedVariants.Count > 0)
            {
                // Build: variantId -> ordered list of positions for that variant
                var variantToPositions = new Dictionary<int, List<string>>();
                foreach (var v in resolvedVariants)
                {
                    if (!variantToPositions.ContainsKey(v.Id))
                        variantToPositions[v.Id] = new List<string>();

                    if (v.Placeholders != null)
                    {
                        foreach (var ph in v.Placeholders)
                        {
                            var pos = ph?.Position;
                            if (!string.IsNullOrWhiteSpace(pos) && !variantToPositions[v.Id].Contains(pos))
                                variantToPositions[v.Id].Add(pos);
                        }
                    }
                }

                // Group variants that have the identical set of positions into one PrintArea
                var groupByPositions = new Dictionary<string, (List<int> variantIds, List<string> positions)>();
                foreach (var (variantId, positions) in variantToPositions)
                {
                    var sortedKey = string.Join("|", positions.OrderBy(p => p));
                    if (!groupByPositions.TryGetValue(sortedKey, out var group))
                    {
                        group = (new List<int>(), positions);
                        groupByPositions[sortedKey] = group;
                    }
                    if (!group.variantIds.Contains(variantId))
                        group.variantIds.Add(variantId);
                }

                // Variants with no placeholders: attach to the first group so they
                // still appear in print_areas and pass Printify's variant validation.
                var noPhVariants = resolvedVariants
                    .Where(v => v.Placeholders == null || v.Placeholders.Count == 0)
                    .ToList();
                if (noPhVariants.Count > 0 && groupByPositions.Count > 0)
                {
                    var firstGroup = groupByPositions.First().Value;
                    foreach (var nv in noPhVariants)
                        if (!firstGroup.variantIds.Contains(nv.Id)) firstGroup.variantIds.Add(nv.Id);
                }

                // Helper: sort key — cover=-1, page_N=N, other=MaxValue
                static int PositionSortKey(string p)
                {
                    if (p.Equals("cover", StringComparison.OrdinalIgnoreCase)) return -1;
                    if (p.StartsWith("page_", StringComparison.OrdinalIgnoreCase)
                        && int.TryParse(p.AsSpan(5), out var n)) return n;
                    return int.MaxValue;
                }

                printAreas = new List<PrintArea>();
                foreach (var (_, (variantIds, positions)) in groupByPositions)
                {
                    var placeholders = new List<PrintAreaPlaceholder>();
                    foreach (var position in positions.OrderBy(PositionSortKey))
                    {
                        string? uploadId = null;
                        if (position.Equals("cover", StringComparison.OrdinalIgnoreCase))
                        {
                            uploadId = coverUpload.Id;
                        }
                        else if (position.StartsWith("page_", StringComparison.OrdinalIgnoreCase)
                                 && int.TryParse(position.AsSpan(5), out var pageIdx)
                                 && pageIdx >= 1 && pageIdx <= pageUploadIds.Count)
                        {
                            uploadId = pageUploadIds[pageIdx - 1];
                        }

                        if (string.IsNullOrWhiteSpace(uploadId)) continue;

                        placeholders.Add(new PrintAreaPlaceholder
                        {
                            Position = position,
                            DecorationMethod = "digital-printing",
                            Images = new List<PrintAreaImage>
                            {
                                new PrintAreaImage
                                {
                                    Id = uploadId,
                                    X = 0.5,
                                    Y = 0.5,
                                    Scale = 1.0,
                                    Width = 1,
                                    Height = 1
                                }
                            }
                        });
                    }

                    if (placeholders.Count > 0)
                    {
                        printAreas.Add(new PrintArea
                        {
                            VariantIds = variantIds.Distinct().ToList(),
                            Placeholders = placeholders
                        });
                    }
                }
            }
            else
            {
                var fallbackPlaceholders = new List<PrintAreaPlaceholder>
                {
                    new PrintAreaPlaceholder
                    {
                        Position = "cover",
                        DecorationMethod = "digital-printing",
                        Images = new List<PrintAreaImage>
                        {
                            new PrintAreaImage { Id = coverUpload.Id, X = 0.5, Y = 0.5, Scale = 0.85, Width = 1, Height = 1, Angle = 0 }
                        }
                    }
                };
                // Small horizontal nudge (same percentage used when splitting spreads)
                const double seamShiftRatio = 0.02; // 2%
                for (int i = 1; i <= PageCount; i++)
                {
                    double x = 0.5;
                    // Pages 2..(PageCount-1) are paired spreads: even pages = left, odd = right
                    if (i > 1 && i < PageCount)
                    {
                        if (i % 2 == 0)
                        {
                            // left page: shift center slightly left
                            x = Math.Max(0.0, 0.5 - seamShiftRatio);
                        }
                        else
                        {
                            // right page: shift center slightly right
                            x = Math.Min(1.0, 0.5 + seamShiftRatio);
                        }
                    }

                    fallbackPlaceholders.Add(new PrintAreaPlaceholder
                    {
                        Position = $"page_{i}",
                        DecorationMethod = "digital-printing",
                        Images = new List<PrintAreaImage>
                        {
                            new PrintAreaImage { Id = pageUploadIds[i - 1], X = x, Y = 0.5, Scale = 0.85, Width = 1, Height = 1, Angle = 0 }
                        }
                    });
                }
                printAreas = new List<PrintArea>
                {
                    new PrintArea { VariantIds = availableVariantIds.ToList(), Placeholders = fallbackPlaceholders }
                };
            }

            // ── 6. Create the product (write payload to disk for debugging) ─────
            onProgress?.Invoke("Creating product on Printify...");

            // Log uploaded image filename → positions → upload id for diagnostics
            try
            {
                var uploadFileNameById = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                if (!string.IsNullOrWhiteSpace(coverUpload?.Id))
                    uploadFileNameById[coverUpload.Id] = Path.GetFileName(coverPath);

                for (int i = 0; i < pageUploadIds.Count; i++)
                {
                    var uid = pageUploadIds[i];
                    if (string.IsNullOrWhiteSpace(uid)) continue;
                    var fname = Path.GetFileName(pagePaths[i]);
                    if (!uploadFileNameById.ContainsKey(uid))
                        uploadFileNameById[uid] = fname;
                }

                var placements = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);
                foreach (var pa in printAreas)
                {
                    var placeholders = pa.Placeholders ?? new List<PrintAreaPlaceholder>();
                    foreach (var ph in placeholders)
                    {
                        var pos = ph?.Position ?? "unknown";
                        var imgs = ph.Images ?? new List<PrintAreaImage>();
                        foreach (var img in imgs)
                        {
                            var uid = img.Id;
                            if (string.IsNullOrWhiteSpace(uid)) continue;
                            if (!placements.TryGetValue(uid, out var list)) { list = new List<string>(); placements[uid] = list; }
                            if (!list.Contains(pos)) list.Add(pos);
                        }
                    }
                }

                var export = placements.Select(kvp => new
                {
                    upload_id = kvp.Key,
                    file_name = uploadFileNameById.TryGetValue(kvp.Key, out var n) ? n : null,
                    positions = kvp.Value
                }).ToList();

                var mapPath = Path.Combine(outputDir, "printify_image_placements.json");
                await File.WriteAllTextAsync(mapPath, JsonSerializer.Serialize(export, new JsonSerializerOptions { WriteIndented = true }));

                foreach (var e in export)
                {
                    var fn = e.file_name ?? "(unknown)";
                    var posList = (e.positions as List<string>) ?? new List<string>();
                    Console.WriteLine($"Placed image {fn} at positions: {string.Join(", ", posList)} -> upload id {e.upload_id}");
                }
            }
            catch
            {
                // best-effort only
            }

            var createRequest = new CreateProductRequest
            {
                Title           = title,
                Description     = BuildDescription(theme),
                BlueprintId     = BlueprintId,
                PrintProviderId = PrintProviderId,
                Variants = resolvedVariants != null
                    ? resolvedVariants.Select(v => new CreateProductVariant
                    {
                        Id = v.Id,
                        Price = v.Price ?? (v.Prices != null && v.Prices.Count > 0 ? v.Prices[0].Price : 1768),
                        IsEnabled = true
                    }).ToList()
                    : new List<CreateProductVariant>
                    {
                        new CreateProductVariant
                        {
                            Id = selectedVariantId,
                            Price = 1768,
                            IsEnabled = true
                        }
                    },
                PrintAreas = printAreas,
                Tags       = BuildTags(theme)
            };

            // Sanity-check: ensure all variant IDs declared in `variants` are present
            // in at least one `print_areas.*.variant_ids`. If not, add them to the
            // cover print area to satisfy Printify validation.
            try
            {
                var variantIdsInVariants = new HashSet<int>(createRequest.Variants.Select(v => v.Id));
                var variantIdsInPrintAreas = new HashSet<int>(createRequest.PrintAreas.SelectMany(pa => pa.VariantIds));
                var missingInPrintAreas = variantIdsInVariants.Except(variantIdsInPrintAreas).ToList();
                if (missingInPrintAreas.Count > 0 && createRequest.PrintAreas.Count > 0)
                {
                    // add missing ids to the first print area (cover)
                    var cover = createRequest.PrintAreas[0];
                    foreach (var id in missingInPrintAreas)
                    {
                        if (!cover.VariantIds.Contains(id)) cover.VariantIds.Add(id);
                    }

                    var diagPath = Path.Combine(outputDir, "printify_create_request_validated.json");
                    await File.WriteAllTextAsync(diagPath, JsonSerializer.Serialize(new
                    {
                        missingInPrintAreas,
                        variantIdsInVariants = variantIdsInVariants.ToList(),
                        variantIdsInPrintAreas = variantIdsInPrintAreas.ToList()
                    }, new JsonSerializerOptions { WriteIndented = true }));
                }
            }
            catch
            {
                // best-effort diagnostics only
            }

            try
            {
                var payloadPath = Path.Combine(outputDir, "printify_create_request.json");
                var saveOpts = new JsonSerializerOptions { WriteIndented = true, DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull };
                await File.WriteAllTextAsync(payloadPath, JsonSerializer.Serialize(createRequest, saveOpts));
                Console.WriteLine($"Wrote Printify create-product payload to: {payloadPath}");

                var product = await _printify.CreateProductAsync(_shopId, createRequest);
                // Phase 7 behavior: Do NOT publish the product, only create it as a draft.
                onProgress?.Invoke($"Created product {product.Id} as draft (not published).");
                // Save product info
                await SaveProductInfoAsync(outputDir, product.Id);
                // Save prompts and job manifest (include overlays and upload ids)
                await PromptRecorder.SaveToDirectoryAsync(outputDir);
                await SaveJobManifestAsync(outputDir, title, theme, styleAddon, pagePaths, pageSubjects, pageOverlays, cumulativePageTexts, pageUploadIds);
                return product.Id;
            }
            catch (PrintifyApiException pex)
            {
                var errPath = Path.Combine(outputDir, "printify_create_response_error.json");
                await File.WriteAllTextAsync(errPath, pex.ResponseBody ?? pex.Message);
                Console.WriteLine($"Printify API error {pex.StatusCode}: {pex.Message}");
                Console.WriteLine($"Saved API response body to: {errPath}");
                // Save prompts so we can reproduce the failing request
                await PromptRecorder.SaveToDirectoryAsync(outputDir);
                throw;
            }

            
        }

        // ── Helpers ────────────────────────────────────────────────────────────

        /// <summary>
        /// Ensures an interior page image is exactly PageWidth × PageHeight pixels.
        /// Scales to cover (fill without white bars) then saves back in-place.
        /// Returns the same path so callers can use it directly.
        /// </summary>
        private string EnsurePageSize(string imagePath)
        {
            using var img = Image.Load<Rgba32>(imagePath);
            if (img.Width == PageWidth && img.Height == PageHeight)
                return imagePath;

            // Scale-to-fit: resize the image to fit within the target page and pad with white.
            double scaleX = (double)PageWidth / Math.Max(1, img.Width);
            double scaleY = (double)PageHeight / Math.Max(1, img.Height);
            double scale = Math.Min(scaleX, scaleY);

            int scaledW = Math.Max(1, (int)Math.Round(img.Width * scale));
            int scaledH = Math.Max(1, (int)Math.Round(img.Height * scale));

            using var canvas = new Image<Rgba32>(PageWidth, PageHeight, new Rgba32(255, 255, 255));
            using var resized = img.Clone(ctx => ctx.Resize(scaledW, scaledH, KnownResamplers.Lanczos3));
            int offsetX = (PageWidth - scaledW) / 2;
            int offsetY = (PageHeight - scaledH) / 2;
            canvas.Mutate(ctx => ctx.DrawImage(resized, new Point(offsetX, offsetY), 1f));
            canvas.SaveAsJpeg(imagePath);
            return imagePath;
        }

        /// <summary>
        /// Loads the back (left) and front (right) images, resizes each to half the
        /// cover spread width at full cover height, then composites them side by side.
        /// </summary>
        private string StitchCoverSpread(string backPath, string frontPath, string outputDir)
        {
            int totalWidth  = CoverWidth;
            int totalHeight = CoverHeight;
            int spinePx = (int)Math.Round(totalWidth * Bp.SpineGapPercent / 100.0);
            int sideWidth = (totalWidth - spinePx) / 2;
            int frontWidth = totalWidth - spinePx - sideWidth;

            using var back  = Image.Load<Rgba32>(backPath);
            using var front = Image.Load<Rgba32>(frontPath);

            back.Mutate(ctx  => ctx.Resize(sideWidth,  totalHeight));
            front.Mutate(ctx => ctx.Resize(frontWidth, totalHeight));

            using var spread = new Image<Rgba32>(totalWidth, totalHeight);
            spread.Mutate(ctx =>
            {
                ctx.DrawImage(back,  new Point(0,                 0), 1f);
                ctx.DrawImage(front, new Point(sideWidth + spinePx, 0), 1f);
            });

            var outPath = Path.Combine(outputDir, "cover_spread.png");
            spread.SaveAsPng(outPath);
            return outPath;
        }

        private async Task<string> GenerateAndValidateCoverAsync(string outputDir, bool isFront, string title, string theme, string styleAddon)
        {
            const int maxAttempts = 3;
            string? promptPrefix = null;
            string path = string.Empty;
            for (int attempt = 1; attempt <= maxAttempts; attempt++)
            {
                try
                {
                    if (isFront)
                        path = await _imageGen.GenerateFrontCoverAsync(outputDir, title, theme, styleAddon, promptPrefix);
                    else
                        path = await _imageGen.GenerateBackCoverAsync(outputDir, theme, styleAddon, promptPrefix);

                    var issues = _finisher.ValidateImageFile(path, null);
                    if (issues.Count == 0)
                        return path;

                    Console.WriteLine($"Cover validation failed (attempt {attempt}/{maxAttempts}): {string.Join("; ", issues)}");
                    if (attempt < maxAttempts)
                    {
                        string Shorten(string s) => Regex.Replace(s, "\\s+", " ").Split(new[] { '\n', '.' }, StringSplitOptions.RemoveEmptyEntries)[0].Trim();
                        promptPrefix = "Avoid: " + string.Join(", ", issues.Select(Shorten)) + ". Do not include barcodes, price tags, stickers, or any visible text; leave the bottom 15% clear.";
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Cover generation error: {ex.Message}");
                    if (attempt == maxAttempts) throw;
                }
            }
            return path;
        }

        private async Task<string> GenerateAndProcessSinglePageAsync(string outputDir, int pageNumber, string subject, string styleAddon, Action<string>? onProgress = null)
        {
            const int maxAttempts = 3;
            string? promptPrefix = null;
            string outPath = Path.Combine(outputDir, $"page_{pageNumber:D2}.jpg");

            for (int attempt = 1; attempt <= maxAttempts; attempt++)
            {
                onProgress?.Invoke($"Generating page {pageNumber} (attempt {attempt})...");
                var generated = await _imageGen.GeneratePageAsync(outputDir, pageNumber, subject, styleAddon, promptPrefix);
                try
                {
                    // Resize and pad to PageWidth x PageHeight (same behavior as original flow)
                    using (var img = Image.Load<Rgba32>(generated))
                    {
                        double scale = Math.Min((double)PageWidth / Math.Max(1, img.Width), (double)PageHeight / Math.Max(1, img.Height));
                        int scaledW = Math.Max(1, (int)Math.Round(img.Width * scale));
                        int scaledH = Math.Max(1, (int)Math.Round(img.Height * scale));

                        using var canvas = new Image<Rgba32>(PageWidth, PageHeight, new Rgba32(255, 255, 255));
                        using var resized = img.Clone(ctx => ctx.Resize(scaledW, scaledH, KnownResamplers.Lanczos3));
                        int offsetX = (PageWidth - scaledW) / 2;
                        int offsetY = (PageHeight - scaledH) / 2;
                        canvas.Mutate(ctx => ctx.DrawImage(resized, new Point(offsetX, offsetY), 1f));
                        canvas.SaveAsJpeg(outPath);
                    }

                    var issues = _finisher.ValidateImageFile(outPath, pageNumber);
                    if (issues.Count == 0)
                        return outPath;

                    Console.WriteLine($"Page {pageNumber} validation failed (attempt {attempt}/{maxAttempts}): {string.Join("; ", issues)}");
                    if (attempt < maxAttempts)
                    {
                        string Shorten(string s) => Regex.Replace(s, "\\s+", " ").Split(new[] { '\n', '.' }, StringSplitOptions.RemoveEmptyEntries)[0].Trim();
                        promptPrefix = "Avoid: " + string.Join(", ", issues.Select(Shorten)) + ". Do not include barcodes, price tags, stickers, or any visible text; leave the bottom 15% clear.";
                        // remove the produced file so next attempt writes cleanly
                        try { if (File.Exists(outPath)) File.Delete(outPath); } catch { }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error processing page {pageNumber}: {ex.Message}");
                    if (attempt == maxAttempts) throw;
                }
            }
            return outPath;
        }

        private async Task<(string leftPath, string rightPath)> GenerateAndProcessSpreadAsync(string outputDir, int leftPageNumber, string subject, string styleAddon, Action<string>? onProgress = null)
        {
            const int maxAttempts = 3;
            string? promptPrefix = null;
            int spreadHeight = PageHeight;
            int spreadWidth = (int)Math.Round(spreadHeight * 16.0 / 9.0);

            string leftPath = Path.Combine(outputDir, $"page_{leftPageNumber:D2}.jpg");
            string rightPath = Path.Combine(outputDir, $"page_{leftPageNumber + 1:D2}.jpg");

            for (int attempt = 1; attempt <= maxAttempts; attempt++)
            {
                onProgress?.Invoke($"Generating spread starting page {leftPageNumber} (attempt {attempt})...");
                var spreadImagePath = await _imageGen.GeneratePageAsync(outputDir, leftPageNumber, subject, styleAddon, promptPrefix);
                try
                {
                    using (var spreadImg = Image.Load<Rgba32>(spreadImagePath))
                    {
                        double fitScale = Math.Min((double)spreadWidth / Math.Max(1, spreadImg.Width), (double)spreadHeight / Math.Max(1, spreadImg.Height));
                        int scaledW = Math.Max(1, (int)Math.Round(spreadImg.Width * fitScale));
                        int scaledH = Math.Max(1, (int)Math.Round(spreadImg.Height * fitScale));

                        using var canvas = new Image<Rgba32>(spreadWidth, spreadHeight, new Rgba32(255, 255, 255));
                        using var resizedSpread = spreadImg.Clone(ctx => ctx.Resize(scaledW, scaledH, KnownResamplers.Lanczos3));
                        int offsetX = (spreadWidth - scaledW) / 2;
                        int offsetY = (spreadHeight - scaledH) / 2;
                        canvas.Mutate(ctx => ctx.DrawImage(resizedSpread, new Point(offsetX, offsetY), 1f));

                        const double trimPercent = 0.02; // 2% each side
                        int trimPx = (int)Math.Round(spreadWidth * trimPercent);
                        int innerX = Math.Max(0, trimPx);
                        int innerW = Math.Max(1, spreadWidth - 2 * trimPx);

                        using (var inner = canvas.Clone(ctx => ctx.Crop(new Rectangle(innerX, 0, innerW, spreadHeight))))
                        {
                            int halfInnerW = inner.Width / 2;
                            int movePx = (int)Math.Round(PageWidth * 0.02); // 2% of page width

                            using (var leftHalf = inner.Clone(ctx => ctx.Crop(new Rectangle(0, 0, halfInnerW, spreadHeight))))
                            using (var rightHalf = inner.Clone(ctx => ctx.Crop(new Rectangle(halfInnerW, 0, inner.Width - halfInnerW, spreadHeight))))
                            {
                                double scaleL = Math.Min((double)PageWidth / Math.Max(1, leftHalf.Width), (double)PageHeight / Math.Max(1, leftHalf.Height));
                                int scaledLW = Math.Max(1, (int)Math.Round(leftHalf.Width * scaleL));
                                int scaledLH = Math.Max(1, (int)Math.Round(leftHalf.Height * scaleL));

                                using (var leftCanvas = new Image<Rgba32>(PageWidth, PageHeight, new Rgba32(255, 255, 255)))
                                {
                                    using var leftResized = leftHalf.Clone(ctx => ctx.Resize(scaledLW, scaledLH, KnownResamplers.Lanczos3));
                                    int leftOffsetX = (PageWidth - scaledLW) / 2 - movePx;
                                    int leftOffsetY = (PageHeight - scaledLH) / 2;
                                    leftCanvas.Mutate(ctx => ctx.DrawImage(leftResized, new Point(leftOffsetX, leftOffsetY), 1f));
                                    leftCanvas.SaveAsJpeg(leftPath);
                                }

                                double scaleR = Math.Min((double)PageWidth / Math.Max(1, rightHalf.Width), (double)PageHeight / Math.Max(1, rightHalf.Height));
                                int scaledRW = Math.Max(1, (int)Math.Round(rightHalf.Width * scaleR));
                                int scaledRH = Math.Max(1, (int)Math.Round(rightHalf.Height * scaleR));

                                using (var rightCanvas = new Image<Rgba32>(PageWidth, PageHeight, new Rgba32(255, 255, 255)))
                                {
                                    using var rightResized = rightHalf.Clone(ctx => ctx.Resize(scaledRW, scaledRH, KnownResamplers.Lanczos3));
                                    int rightOffsetX = (PageWidth - scaledRW) / 2 + movePx;
                                    int rightOffsetY = (PageHeight - scaledRH) / 2;
                                    rightCanvas.Mutate(ctx => ctx.DrawImage(rightResized, new Point(rightOffsetX, rightOffsetY), 1f));
                                    rightCanvas.SaveAsJpeg(rightPath);
                                }
                            }
                        }
                    }

                    var leftIssues = _finisher.ValidateImageFile(leftPath, leftPageNumber);
                    var rightIssues = _finisher.ValidateImageFile(rightPath, leftPageNumber + 1);
                    var allIssues = leftIssues.Concat(rightIssues).ToList();
                    if (allIssues.Count == 0)
                        return (leftPath, rightPath);

                    Console.WriteLine($"Spread validation failed for pages {leftPageNumber}-{leftPageNumber + 1} (attempt {attempt}/{maxAttempts}): {string.Join("; ", allIssues)}");
                    if (attempt < maxAttempts)
                    {
                        string Shorten(string s) => Regex.Replace(s, "\\s+", " ").Split(new[] { '\n', '.' }, StringSplitOptions.RemoveEmptyEntries)[0].Trim();
                        promptPrefix = "Avoid: " + string.Join(", ", allIssues.Select(Shorten)) + ". Do not include barcodes, price tags, stickers, or any visible text; leave the bottom 15% clear.";
                        try { if (File.Exists(leftPath)) File.Delete(leftPath); } catch { }
                        try { if (File.Exists(rightPath)) File.Delete(rightPath); } catch { }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error processing spread starting {leftPageNumber}: {ex.Message}");
                    if (attempt == maxAttempts) throw;
                }
            }

            return (leftPath, rightPath);
        }

        internal static PrintArea BuildPrintArea(string position, string imageId, IEnumerable<int> variantIds)
            => new PrintArea
            {
                VariantIds = variantIds?.ToList() ?? new List<int>(),
                Placeholders = new List<PrintAreaPlaceholder>
                {
                    new PrintAreaPlaceholder
                    {
                        Position         = position,
                        DecorationMethod = "digital-printing",
                        Images = new List<PrintAreaImage>
                        {
                            new PrintAreaImage
                            {
                                Id    = imageId,
                                X     = 0.5,
                                Y     = 0.5,
                                Scale = 1.0,
                                Width = 1,
                                Height = 1,
                                Angle = 0
                            }
                        }
                    }
                }
            };

        private async Task<string[]> GenerateFullStoryAsync(string theme, string storyPrompt)
        {
            const int maxRetries = 3;
            string raw = "";

            PromptRecorder.Record("GenerateFullStoryAsync", "full_story", storyPrompt);
            using var ollama = new OllamaClient(_ollamaUrl);

            for (int attempt = 1; attempt <= maxRetries; attempt++)
            {
                try
                {
                    raw = await ollama.GenerateAsync(_ollamaModel, storyPrompt);
                    var body = raw.Trim();
                    // Extract "response" from Ollama JSON envelope
                    if (body.StartsWith('{'))
                    {
                        using var env = JsonDocument.Parse(body);
                        if (env.RootElement.TryGetProperty("response", out var resp))
                            body = resp.GetString()?.Trim() ?? body;
                    }
                    // Strip markdown fences
                    if (body.StartsWith("```"))
                    {
                        var firstNewline = body.IndexOf('\n');
                        var lastFence = body.LastIndexOf("```");
                        if (firstNewline > 0 && lastFence > firstNewline)
                            body = body[(firstNewline + 1)..lastFence].Trim();
                    }
                    using var result = JsonDocument.Parse(body);
                    if (result.RootElement.ValueKind == JsonValueKind.Array)
                    {
                        var segments = new List<string>();
                        foreach (var el in result.RootElement.EnumerateArray())
                        {
                            var s = el.GetString()?.Trim();
                            if (!string.IsNullOrWhiteSpace(s))
                                segments.Add(s);
                        }
                        int neededSegments = (PageCount - 2) / 2 + 2;
                        if (segments.Count >= neededSegments)
                        {
                            PromptRecorder.Record("GenerateFullStoryAsync", "parsed_segments", string.Join("\n---\n", segments.Select((s, i) => $"Segment {i + 1}: {s}")));
                            PromptRecorder.Record("GenerateFullStoryAsync", "raw_response", body);
                            return segments.ToArray();
                        }
                        Console.WriteLine($"  [Ollama] Full story attempt {attempt}/{maxRetries}: got {segments.Count} segments, need {neededSegments}");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"  [Ollama] Full story attempt {attempt}/{maxRetries} failed: {ex.Message}");
                }
                if (attempt < maxRetries)
                    await Task.Delay(1500);
            }
            Console.WriteLine("  [Ollama] Full story generation failed — using fallback subjects.");
            return _promptProvider.BuildPageSubjectsFallback(theme);
        }

        private async Task<PageSubject> GeneratePageSubjectAsync(string theme, int pageNumber)
        {
            const int maxRetries = 3;
            var prompt = _promptProvider.BuildPageSubjectPrompt(theme, pageNumber);

            using var ollama = new OllamaClient(_ollamaUrl);
            for (int attempt = 1; attempt <= maxRetries; attempt++)
            {
                try
                {
                    // record the page-subject prompt for reproducibility
                    var raw  = await ollama.GenerateAsync(_ollamaModel, prompt);
                    var body = raw.Trim();
                    PromptRecorder.Record("GeneratePageSubjectAsync", $"page_{pageNumber}", raw);

                    // Extract "response" from the Ollama JSON envelope
                    if (body.StartsWith('{'))
                    {
                        using var env = JsonDocument.Parse(body);
                        if (env.RootElement.TryGetProperty("response", out var resp))
                            body = resp.GetString()?.Trim() ?? body;
                    }

                    if (!string.IsNullOrWhiteSpace(body))
                    {
                        // Try to parse a JSON object with fields { scene, overlay, overlay_left, overlay_right }
                        try
                        {
                            using var doc = JsonDocument.Parse(body);
                            if (doc.RootElement.ValueKind == JsonValueKind.Object)
                            {
                                var root = doc.RootElement;
                                static string? GetProp(JsonElement el, params string[] names)
                                {
                                    foreach (var n in names)
                                        if (el.TryGetProperty(n, out var p) && p.ValueKind == JsonValueKind.String)
                                            return p.GetString();
                                    return null;
                                }

                                var scene = GetProp(root, "scene", "Scene", "description", "scene_description");
                                var overlay = GetProp(root, "overlay", "Overlay", "overlay_text", "caption");
                                var overlayLeft = GetProp(root, "overlay_left", "overlayLeft", "overlay_left_text");
                                var overlayRight = GetProp(root, "overlay_right", "overlayRight", "overlay_right_text");

                                var pageText = GetProp(root, "page_text", "pageText", "text", "caption");
                                var pageTextLeft = GetProp(root, "page_text_left", "pageTextLeft", "page_text_left", "text_left");
                                var pageTextRight = GetProp(root, "page_text_right", "pageTextRight", "page_text_right", "text_right");

                                // If single "overlay" provided for spreads, use it for both sides
                                if (overlayLeft == null && overlay != null) overlayLeft = overlay;
                                if (overlayRight == null && overlay != null) overlayRight = overlay;

                                // If single page_text provided, use it for both sides
                                if (pageTextLeft == null && pageText != null) pageTextLeft = pageText;
                                if (pageTextRight == null && pageText != null) pageTextRight = pageText;

                                if (!string.IsNullOrWhiteSpace(scene))
                                    return new PageSubject(
                                        scene.Trim(),
                                        overlayLeft?.Trim() ?? "",
                                        overlayRight?.Trim(),
                                        pageTextLeft?.Trim() ?? pageText?.Trim() ?? "",
                                        pageTextRight?.Trim() ?? pageText?.Trim());
                            }
                        }
                        catch { /* not JSON or malformed — fall back below */ }

                        // Not JSON / couldn't extract fields: treat entire body as the scene
                        return new PageSubject(body.Trim(), "", null, string.Empty, null);
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"  [Ollama] Page {pageNumber} attempt {attempt}/{maxRetries} failed: {ex.Message}");
                }

                if (attempt < maxRetries)
                    await Task.Delay(1500);
            }

            Console.WriteLine($"  [Ollama] Page {pageNumber} all retries exhausted — using fallback.");
            var fallback = _promptProvider.BuildPageSubjectsFallback(theme)[pageNumber - 1];
            return new PageSubject(fallback, "", null);
        }

        private string BuildDescription(string theme)
            => _promptProvider.BuildDescription(theme);

        private List<string> BuildTags(string theme)
            => new List<string>(_promptProvider.BuildTags(theme));

        internal static string SanitizeTitleForFolder(string title)
        {
            if (string.IsNullOrWhiteSpace(title))
                return "coloring_book";

            var invalid = Path.GetInvalidFileNameChars();
            var cleaned = new string(title.Select(ch => invalid.Contains(ch) ? '_' : ch).ToArray());
            cleaned = Regex.Replace(cleaned, "\\s+", "_");
            cleaned = cleaned.Trim('_');
            if (string.IsNullOrEmpty(cleaned))
                return "coloring_book";
            return cleaned;
        }

        // ── Job manifest ───────────────────────────────────────────────

        private async Task SaveJobManifestAsync(string outputDir, string title, string theme, string styleAddon, List<string> pagePaths, Dictionary<int, string> pageSubjects, List<string> pageOverlays, List<string> pageTexts, List<string> pageUploadIds)
        {
            var jobs = new List<GenerationJob>();

            var bp = Bp;

            // Back cover
            jobs.Add(new GenerationJob
            {
                PageLabel = "back_cover",
                IsCover = true,
                IsBackCover = true,
                Title = title,
                Theme = theme,
                StyleAddon = styleAddon,
                BookType = _promptProvider.BookType,
                AspectRatio = bp.CoverAspectRatio,
                ConvertToBlackAndWhite = _finisher.ShouldConvertToBlackAndWhite,
                OutputFileName = "back_cover.jpg",
                OutputPath = Path.Combine(outputDir, "back_cover.jpg")
            });

            // Front cover
            jobs.Add(new GenerationJob
            {
                PageLabel = "front_cover",
                IsCover = true,
                IsFrontCover = true,
                Title = title,
                Theme = theme,
                StyleAddon = styleAddon,
                BookType = _promptProvider.BookType,
                AspectRatio = bp.CoverAspectRatio,
                ConvertToBlackAndWhite = _finisher.ShouldConvertToBlackAndWhite,
                OutputFileName = "front_cover.jpg",
                OutputPath = Path.Combine(outputDir, "front_cover.jpg")
            });

            // Cover spread
            jobs.Add(new GenerationJob
            {
                PageLabel = "cover_spread",
                IsCover = true,
                Title = title,
                Theme = theme,
                StyleAddon = styleAddon,
                BookType = _promptProvider.BookType,
                AspectRatio = bp.CoverSpreadAspectRatio,
                ConvertToBlackAndWhite = _finisher.ShouldConvertToBlackAndWhite,
                OutputFileName = "cover_spread.png",
                OutputPath = Path.Combine(outputDir, "cover_spread.png")
            });

            // Interior pages
            for (int i = 0; i < pagePaths.Count; i++)
            {
                var pageNum = i + 1;
                var ratio = (pageNum == 1 || pageNum == bp.PageCount) ? bp.PageAspectRatio : bp.SpreadAspectRatio;
                jobs.Add(new GenerationJob
                {
                    PageNumber = pageNum,
                    PageLabel = $"page_{pageNum:D2}",
                    Title = title,
                    Theme = theme,
                    StyleAddon = styleAddon,
                    BookType = _promptProvider.BookType,
                    AspectRatio = ratio,
                    ConvertToBlackAndWhite = _finisher.ShouldConvertToBlackAndWhite,
                    OutputFileName = Path.GetFileName(pagePaths[i]),
                    OutputPath = pagePaths[i],
                    Subject = pageSubjects != null && pageSubjects.TryGetValue(pageNum, out var subj) ? subj : "",
                    Metadata = new Dictionary<string, string>
                    {
                        { "overlay", pageOverlays != null && pageOverlays.Count > i ? pageOverlays[i] : "" },
                        { "upload_id", pageUploadIds != null && pageUploadIds.Count > i ? pageUploadIds[i] : "" },
                        { "page_text", pageTexts != null && pageTexts.Count > i ? pageTexts[i] : "" }
                    }
                });
            }

            // Save overlays mapping for quick inspection / downstream use
            try
            {
                var overlayExport = new List<object>();
                for (int i = 0; i < pagePaths.Count; i++)
                {
                    overlayExport.Add(new
                    {
                        page = i + 1,
                        file_name = Path.GetFileName(pagePaths[i]),
                        upload_id = pageUploadIds != null && pageUploadIds.Count > i ? pageUploadIds[i] : null,
                        overlay = pageOverlays != null && pageOverlays.Count > i ? pageOverlays[i] : null,
                        page_text = pageTexts != null && pageTexts.Count > i ? pageTexts[i] : null,
                        subject = pageSubjects != null && pageSubjects.TryGetValue(i + 1, out var s) ? s : null
                    });
                }

                var overlaysPath = Path.Combine(outputDir, "page_overlays.json");
                await File.WriteAllTextAsync(overlaysPath, JsonSerializer.Serialize(overlayExport, new JsonSerializerOptions { WriteIndented = true }));
            }
            catch
            {
                // best-effort only
            }

            await _finisher.SaveJobManifestAsync(outputDir, title, theme, jobs);
        }

        // ── Product info ──────────────────────────────────────────────

        private async Task SaveProductInfoAsync(string outputDir, string productId)
        {
            var path = Path.Combine(outputDir, "printify_product.json");
            var info = new { productId, shopId = _shopId, blueprintId = BlueprintId, printProviderId = PrintProviderId, savedAt = DateTime.UtcNow };
            await File.WriteAllTextAsync(path, JsonSerializer.Serialize(info, new JsonSerializerOptions { WriteIndented = true }));
        }

        // ── Regenerate ─────────────────────────────────────────────────

        public async Task RegeneratePageAsync(
            string outputDir,
            int pageNumber,
            string? promptAppend = null,
            Action<string>? onProgress = null)
        {
            // 1. Read product info
            var prodPath = Path.Combine(outputDir, "printify_product.json");
            if (!File.Exists(prodPath)) throw new InvalidOperationException("printify_product.json not found — run a full generation first.");
            var prodJson = await File.ReadAllTextAsync(prodPath);
            var prodInfo = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(prodJson);
            var productId = prodInfo?["productId"].GetString() ?? throw new InvalidOperationException("productId not found in printify_product.json");

            // 2. Read job manifest to find the job for this page
            var manifestPath = Path.Combine(outputDir, "job_manifest.json");
            var manifestJson = await File.ReadAllTextAsync(manifestPath);
            var manifest = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(manifestJson);
            var jobs = manifest?["jobs"].EnumerateArray().ToList() ?? throw new InvalidOperationException("No jobs in manifest");
            var jobEl = jobs.FirstOrDefault(j => j.TryGetProperty("PageNumber", out var pn) && pn.GetInt32() == pageNumber);
            if (jobEl.ValueKind == JsonValueKind.Undefined) throw new InvalidOperationException($"Page {pageNumber} not found in manifest");

            var theme = manifest["theme"].GetString() ?? "";
            var styleAddon = jobEl.GetProperty("StyleAddon").GetString() ?? "";

            // 3. Gather the original subject (if stored) or get a fresh one from Ollama
            var originalSubject = jobEl.TryGetProperty("Subject", out var sub) ? sub.GetString() ?? "" : "";
            string pageText = "";
            onProgress?.Invoke($"Getting subject for page {pageNumber}...");
            PageSubject? freshSubject = null;
            if (string.IsNullOrWhiteSpace(originalSubject))
                freshSubject = await GeneratePageSubjectAsync(theme, pageNumber);
            var subject = !string.IsNullOrWhiteSpace(originalSubject)
                ? originalSubject
                : (freshSubject?.Scene ?? "");
            pageText = !string.IsNullOrWhiteSpace(originalSubject)
                ? (jobEl.TryGetProperty("page_text", out var pt) ? pt.GetString() ?? "" : "")
                : (freshSubject?.PageTextLeft ?? "");
            if (!string.IsNullOrWhiteSpace(promptAppend))
                subject = $"{subject}, {promptAppend}";

            // 4. Build alternative output path
            var altDir = Path.Combine(outputDir, "_alternatives");
            Directory.CreateDirectory(altDir);
            var altName = $"page_{pageNumber:D2}_v2.jpg";
            var altPath = Path.Combine(altDir, altName);

            // 5. Generate new image
            onProgress?.Invoke($"Generating alternative for page {pageNumber}...");
            var genPath = await _imageGen.GeneratePageAsync(outputDir, pageNumber, subject, styleAddon);

            // 6. Resize to page dimensions and apply finishing
            onProgress?.Invoke($"Finishing alternative for page {pageNumber}...");
            // First resize the raw image to fit the page dimensions
            using (var img = Image.Load<Rgba32>(genPath))
            {
                var bp = Bp;
                double scale = Math.Min((double)bp.PageWidth / Math.Max(1, img.Width), (double)bp.PageHeight / Math.Max(1, img.Height));
                int scaledW = Math.Max(1, (int)Math.Round(img.Width * scale));
                int scaledH = Math.Max(1, (int)Math.Round(img.Height * scale));
                using var canvas = new Image<Rgba32>(bp.PageWidth, bp.PageHeight, new Rgba32(255, 255, 255));
                using var resized = img.Clone(ctx => ctx.Resize(scaledW, scaledH, KnownResamplers.Lanczos3));
                int offsetX = (bp.PageWidth - scaledW) / 2;
                int offsetY = (bp.PageHeight - scaledH) / 2;
                canvas.Mutate(ctx => ctx.DrawImage(resized, new Point(offsetX, offsetY), 1f));
                canvas.SaveAsJpeg(genPath);
            }
            var finished = await _finisher.FinishImageAsync(
                genPath, altDir,
                pageNumber: pageNumber,
                options: new FinishingOptions
                {
                    ConvertToBlackAndWhite = _finisher.ShouldConvertToBlackAndWhite,
                    AddPageNumbers = true,
                    SaveIntermediateStages = true
                });
            // Rename / copy to the canonical alternative name
            if (finished != altPath)
                File.Copy(finished, altPath, overwrite: true);

            // Apply story text overlay for story books
            bool isStoryBook = _promptProvider.BookType.Contains("Story", StringComparison.OrdinalIgnoreCase);
            if (isStoryBook && !string.IsNullOrWhiteSpace(pageText))
            {
                onProgress?.Invoke($"Laying story text onto alternative page {pageNumber}...");
                _finisher.ApplyStoryTextToFile(altPath, pageText);
            }

            // 7. Upload new image to Printify
            onProgress?.Invoke($"Uploading alternative for page {pageNumber} to Printify...");
            var uploaded = await _printify.UploadImageFromFileAsync(altPath);
            var newUploadId = uploaded.Id;

            // 8. Read existing placements to find position for this page
            var placementsPath = Path.Combine(outputDir, "printify_image_placements.json");
            List<Dictionary<string, JsonElement>>? existingPlacements = null;
            if (File.Exists(placementsPath))
            {
                var placementsJson = await File.ReadAllTextAsync(placementsPath);
                existingPlacements = JsonSerializer.Deserialize<List<Dictionary<string, JsonElement>>>(placementsJson);
            }

            // 9. Update the draft product: fetch current product, replace the placeholder
            onProgress?.Invoke($"Updating draft product with new page {pageNumber} image...");
            var currentProduct = await _printify.GetProductAsync(_shopId, productId);
            var updatedPrintAreas = new List<PrintArea>();
            var pagePosition = $"page_{pageNumber}";
            foreach (var pa in currentProduct.PrintAreas ?? new List<PrintArea>())
            {
                var updatedPlaceholders = new List<PrintAreaPlaceholder>();
                foreach (var ph in pa.Placeholders ?? new List<PrintAreaPlaceholder>())
                {
                    var pos = ph.Position ?? "";
                    if (pos.Equals(pagePosition, StringComparison.OrdinalIgnoreCase))
                    {
                        // Replace the image ID with the new upload
                        updatedPlaceholders.Add(new PrintAreaPlaceholder
                        {
                            Position = ph.Position,
                            DecorationMethod = ph.DecorationMethod,
                            Images = ph.Images?.Select(img => new PrintAreaImage
                            {
                                Id = newUploadId,
                                X = img.X,
                                Y = img.Y,
                                Scale = img.Scale,
                                Width = img.Width,
                                Height = img.Height,
                                Angle = img.Angle
                            }).ToList() ?? new List<PrintAreaImage> { new() { Id = newUploadId, X = 0.5, Y = 0.5, Scale = 1.0, Width = 1, Height = 1 } }
                        });
                    }
                    else
                    {
                        updatedPlaceholders.Add(ph);
                    }
                }
                updatedPrintAreas.Add(new PrintArea { VariantIds = pa.VariantIds, Placeholders = updatedPlaceholders });
            }

            var updateRequest = new UpdateProductRequest
            {
                PrintAreas = updatedPrintAreas
            };

            await _printify.UpdateProductAsync(_shopId, productId, updateRequest);

            // 10. Save updated placements
            var updatedPlacements = (existingPlacements ?? new List<Dictionary<string, JsonElement>>())
                .Where(p => !p.GetValueOrDefault("file_name", default).GetString()?.Equals(Path.GetFileName(altPath)) ?? true)
                .Select(p =>
                {
                    var fn = p.GetValueOrDefault("file_name", default).GetString() ?? "";
                    if (fn.Equals(Path.GetFileName(genPath), StringComparison.OrdinalIgnoreCase))
                    {
                        var d = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase)
                        {
                            ["upload_id"] = newUploadId,
                            ["file_name"] = altName,
                            ["positions"] = p.GetValueOrDefault("positions", default).EnumerateArray().Select(e => e.GetString()).ToList()!
                        };
                        return d;
                    }
                    return (object)p;
                })
                .ToList();

            // 11. Save the finisher artifacts too (copy finishing stages)
            var altFinisherDir = Path.Combine(altDir, $"_finisher_page_{pageNumber:D2}_v2");
            if (Directory.Exists(Path.Combine(Path.GetDirectoryName(genPath) ?? "", $"_finisher_page_{pageNumber:D2}")))
            {
                Directory.CreateDirectory(altFinisherDir);
                foreach (var f in Directory.GetFiles(Path.GetDirectoryName(genPath) ?? "", $"_finisher_page_{pageNumber:D2}*"))
                    File.Copy(f, Path.Combine(altFinisherDir, Path.GetFileName(f)), overwrite: true);
            }

            onProgress?.Invoke($"Alternative page {pageNumber} saved to {altPath}");
            onProgress?.Invoke($"Draft product {productId} updated with new image");
        }
    }
}
