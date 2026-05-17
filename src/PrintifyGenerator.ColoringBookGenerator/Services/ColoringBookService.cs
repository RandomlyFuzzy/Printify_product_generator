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
using PrintifyGenerator.ColoringBookGenerator.Utilities;

namespace PrintifyGenerator.ColoringBookGenerator.Services
{
    public class ColoringBookService
    {
        // Blueprint 2721 "Coloring Book" — District Photo (provider 28), variant 148586
        private const int BlueprintId     = 2721;
        private const int PrintProviderId = 28;
        private const int VariantId       = 148586;
        // Cover spread dimensions (px) from the blueprint placeholder
        private const int CoverWidth  = 5175;
        private const int CoverHeight = 3375;
        // Interior page dimensions (px) from the blueprint placeholder
        private const int PageWidth   = 2625;
        private const int PageHeight  = 3375;
        private const int PageCount   = 24;
        // For the current logic: page 1 and 24 are unique, all other pages are paired (2-3, 4-5, ...)
        // so the total number of unique images needed is:
        //   1 (page 1) + 1 (page 24) + ((PageCount - 2) / 2) (pairs)
        private const int UniqueInteriorImages = 2 + ((PageCount - 2) / 2);
        // how many extra candidate pages to generate as replacements —
        // derived from UniqueInteriorImages so a single tuning knob controls both
        private const int ExtraPageCandidates = UniqueInteriorImages / 4;

        private readonly IImageGenerator _imageGen;
        private readonly PrintifyClient  _printify;
        private readonly int             _shopId;
        private readonly string          _ollamaUrl;
        private readonly string          _ollamaModel;

        public ColoringBookService(
            IImageGenerator imageGen,
            PrintifyClient printify,
            int shopId,
            string ollamaUrl   = "http://192.168.0.181:11434",
            string ollamaModel = "gemma4:e2b")
        {
            _imageGen    = imageGen;
            _printify    = printify;
            _shopId      = shopId;
            _ollamaUrl   = ollamaUrl;
            _ollamaModel = ollamaModel;
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

            // ── 1. Queue all page-subject tasks for each PAIR of pages (2-3, 4-5, etc.), but page 1 and 24 are single, centered 3:4 images
            int pairCount = (PageCount - 2) / 2;
            onProgress?.Invoke($"Queuing {pairCount} 16:9 page-subject requests and 2 3:4 requests to Ollama...");
            var pagePairSubjectTasks = Enumerable.Range(1, pairCount)
                .Select(pairNum => GeneratePageSubjectAsync(theme, pairNum + 1)) // pairs start at page 2
                .ToArray();
            var page1SubjectTask = GeneratePageSubjectAsync(theme, 1);
            var page24SubjectTask = GeneratePageSubjectAsync(theme, PageCount);

            // ── 2. Generate covers while Ollama works in the background ────────
            onProgress?.Invoke("Generating back cover (left side)...");
            var backPath = await _imageGen.GenerateBackCoverAsync(outputDir, theme, styleAddon);

            onProgress?.Invoke("Generating front cover (right side)...");
            var frontPath = await _imageGen.GenerateFrontCoverAsync(outputDir, title, theme, styleAddon);

            onProgress?.Invoke("Stitching cover spread (back-left + front-right)...");
            var coverPath = StitchCoverSpread(backPath, frontPath, outputDir);

            // ── 3. Generate each page: cover, page 1, spreads (2-3, 4-5, ...), page 24
            onProgress?.Invoke($"Generating {pairCount} spread images and 2 single images for first/last page...");
            var pagePaths = new List<string>(PageCount);

            // Page 1: centered 3:4
            var page1Subject = await page1SubjectTask;
            Console.WriteLine($"Generating single 3:4 image for page 1/{PageCount} with subject: {page1Subject}");
            var page1Path = await _imageGen.GeneratePageAsync(outputDir, 1, page1Subject, styleAddon);
            // Resize to fit the page and pad with white (avoid cropping important edges)
            using (var img = Image.Load<Rgba32>(page1Path))
            {
                double scale = Math.Min((double)PageWidth / Math.Max(1, img.Width), (double)PageHeight / Math.Max(1, img.Height));
                int scaledW = Math.Max(1, (int)Math.Round(img.Width * scale));
                int scaledH = Math.Max(1, (int)Math.Round(img.Height * scale));

                using var canvas = new Image<Rgba32>(PageWidth, PageHeight, new Rgba32(255, 255, 255));
                using var resized = img.Clone(ctx => ctx.Resize(scaledW, scaledH, KnownResamplers.Lanczos3));
                int offsetX = (PageWidth - scaledW) / 2;
                int offsetY = (PageHeight - scaledH) / 2;
                canvas.Mutate(ctx => ctx.DrawImage(resized, new Point(offsetX, offsetY), 1f));
                canvas.SaveAsJpeg(page1Path);
            }
            Console.WriteLine($"Saved page 1 -> {page1Path}");
            pagePaths.Add(page1Path);
            onPageProgress?.Invoke(1, PageCount);

            // Spreads: pages 2-3, 4-5, ..., 22-23
            int spreadNum = 1;
            for (int page = 2; page < PageCount; page += 2, spreadNum++)
            {
                var subjectTask = await Task.WhenAny(pagePairSubjectTasks);
                var subject = await subjectTask;
                pagePairSubjectTasks = pagePairSubjectTasks.Where(t => t != subjectTask).ToArray();
                Console.WriteLine($"Generating spread {spreadNum}/{pairCount} — pages {page}-{page+1} with subject: {subject}");
                int spreadHeight = PageHeight;
                int spreadWidth = (int)Math.Round(spreadHeight * 16.0 / 9.0);
                var spreadImagePath = await _imageGen.GeneratePageAsync(outputDir, page, subject, styleAddon);

                // Resize to fit the spread inside the target spread canvas and pad with white,
                // then split into left/right PageWidth×PageHeight images (no destructive cropping of source)
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

                    // Trim a small percentage off each outer side of the spread, split the
                    // remaining center region in half, then shift each half 2% outward
                    // when placing it into the PageWidth×PageHeight canvas. This helps hide
                    // the seam by nudging pages outward from the center.
                    const double trimPercent = 0.015; // 1.5% each side
                    int trimPx = (int)Math.Round(spreadWidth * trimPercent);
                    int innerX = Math.Max(0, trimPx);
                    int innerW = Math.Max(1, spreadWidth - 2 * trimPx);

                    var leftPath = Path.Combine(outputDir, $"page_{page:D2}.jpg");
                    var rightPath = Path.Combine(outputDir, $"page_{page + 1:D2}.jpg");

                    using (var inner = canvas.Clone(ctx => ctx.Crop(new Rectangle(innerX, 0, innerW, spreadHeight))))
                    {
                        int halfInnerW = inner.Width / 2;
                        int movePx = (int)Math.Round(PageWidth * 0.02); // 2% of page width

                        using (var leftHalf = inner.Clone(ctx => ctx.Crop(new Rectangle(0, 0, halfInnerW, spreadHeight))))
                        using (var rightHalf = inner.Clone(ctx => ctx.Crop(new Rectangle(halfInnerW, 0, inner.Width - halfInnerW, spreadHeight))))
                        {
                            // Left page: scale-to-fit the left half into page canvas, then shift left
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

                            // Right page: scale-to-fit the right half into page canvas, then shift right
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
                        Console.WriteLine($"  trimmed {trimPx}px each side -> innerW={innerW}, half={inner.Width/2}, move={movePx}px");
                    }
                }

                var leftFile = Path.Combine(outputDir, $"page_{page:D2}.jpg");
                var rightFile = Path.Combine(outputDir, $"page_{page + 1:D2}.jpg");
                Console.WriteLine($"Saved pages {page}-{page+1} -> {leftFile}, {rightFile}");
                pagePaths.Add(leftFile);
                pagePaths.Add(rightFile);
                onPageProgress?.Invoke(page + 1, PageCount);
            }

            // Page 24: centered 3:4 (scale-to-fit + pad to avoid destructive cropping)
            var page24Subject = await page24SubjectTask;
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

            // ── 4. Resolve blueprint variants from Printify (fall back to configured constant) ──
            onProgress?.Invoke("Resolving blueprint variant IDs from Printify...");
            int selectedVariantId = VariantId;
            List<Variant>? resolvedVariants = null;
            var availableVariantIds = new List<int> { VariantId };
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
                Console.WriteLine($"Warning: failed to fetch blueprint variants: {ex.Message}. Using fallback variant {VariantId}.");
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
                // Fallback: ONE PrintArea containing cover + all pages for all variant IDs
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
                const double seamShiftRatio = 0.030730129; // ~3.0730129%
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
                // Save any prompts recorded during generation
                await PromptRecorder.SaveToDirectoryAsync(outputDir);
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
        private static string EnsurePageSize(string imagePath)
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
        private static string StitchCoverSpread(string backPath, string frontPath, string outputDir)
        {
            int halfWidth = CoverWidth / 2;         // 2587
            int remainder = CoverWidth - halfWidth; // 2588 for the front (right)

            using var back  = Image.Load<Rgba32>(backPath);
            using var front = Image.Load<Rgba32>(frontPath);

            back.Mutate(ctx  => ctx.Resize(halfWidth, CoverHeight));
            front.Mutate(ctx => ctx.Resize(remainder, CoverHeight));

            using var spread = new Image<Rgba32>(CoverWidth, CoverHeight);
            spread.Mutate(ctx =>
            {
                ctx.DrawImage(back,  new Point(0,         0), 1f);
                ctx.DrawImage(front, new Point(halfWidth, 0), 1f);
            });

            var outPath = Path.Combine(outputDir, "cover_spread.png");
            spread.SaveAsPng(outPath);
            return outPath;
        }

        private static PrintArea BuildPrintArea(string position, string imageId, IEnumerable<int> variantIds)
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

        private async Task<string> GeneratePageSubjectAsync(string theme, int pageNumber)
        {
            const int maxRetries = 3;
            var prompt =
                $"You are helping create a children's coloring book with the theme \"{theme}\".\n" +
                $"Generate a single unique page subject for page {pageNumber} of the interior coloring pages.\n" +
                "The subject should be a short vivid scene description suitable for a child's coloring page more detailed the better upto 1000 characters.\n" +
                "Return ONLY the subject as plain text. No explanation, no markdown, no extra text.\n" +
                "Example: two rabbits having a tea party in a garden";

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
                        return body;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"  [Ollama] Page {pageNumber} attempt {attempt}/{maxRetries} failed: {ex.Message}");
                }

                if (attempt < maxRetries)
                    await Task.Delay(1500);
            }

            Console.WriteLine($"  [Ollama] Page {pageNumber} all retries exhausted — using fallback.");
            return BuildPageSubjectsFallback(theme)[pageNumber - 1];
        }

        private static string[] BuildPageSubjectsFallback(string theme)
        {
            var t = theme.ToLowerInvariant();
            return new[]
            {
                $"a cute baby animal from the {t} theme playing outdoors",
                $"a happy family scene in a {t} setting",
                $"a magical garden filled with {t}-inspired plants and flowers",
                $"an adventurous child exploring a {t} landscape",
                $"a cozy house or home decorated with {t} elements",
                $"a friendly creature from the {t} world waving hello",
                $"a festive celebration or party with {t} decorations",
                $"a busy market or village in a {t} world",
                $"a playful scene at the beach or outdoors with {t} characters",
                $"a magical flying vehicle or transport in a {t} sky",
                $"a whimsical forest with {t} animals hiding among the trees",
                $"a fun food scene featuring {t}-themed treats and snacks",
                $"a rainy day with {t} characters jumping in puddles under umbrellas",
                $"a starry night sky with {t}-themed constellation art",
                $"a child reading a book surrounded by {t} characters",
                $"a silly robot or machine built from {t} objects",
                $"a treasure hunt map leading through a {t} adventure",
                $"a farm or garden with {t} animals doing chores",
                $"an underwater scene with {t}-inspired sea creatures",
                $"a snow day with {t} characters building a snowman",
                $"a superhero version of a {t} character saving the day",
                $"a sports day with {t} characters playing their favourite game",
                $"a music concert with {t} animals playing instruments",
                $"a bedtime scene with {t} characters saying goodnight under the moon",
            };
        }

        private static string BuildDescription(string theme)
            => $"A beautiful children's coloring book with a {theme} theme. " +
               "Includes a full-color cover and 24 black-and-white coloring pages. " +
               "Perfect for kids aged 3+. Printed on high-quality 8.5\" x 11\" paper.";

        private static List<string> BuildTags(string theme)
            => new List<string>
            {
                "coloring book", "kids", "children", "activity book",
                theme.ToLowerInvariant(), "coloring pages", "printify"
            };

        private static string SanitizeTitleForFolder(string title)
        {
            if (string.IsNullOrWhiteSpace(title))
                return "coloring_book";

            // Replace invalid filename chars with underscore
            var invalid = Path.GetInvalidFileNameChars();
            var cleaned = new string(title.Select(ch => invalid.Contains(ch) ? '_' : ch).ToArray());
            // Collapse whitespace to single underscore
            cleaned = Regex.Replace(cleaned, "\\s+", "_");
            // Trim leading/trailing underscores
            cleaned = cleaned.Trim('_');
            if (string.IsNullOrEmpty(cleaned))
                return "coloring_book";
            return cleaned;
        }
    }
}
