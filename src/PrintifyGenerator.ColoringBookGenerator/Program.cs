using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using PrintifyGenerator.ColoringBookGenerator.Services;
using PrintifyGenerator.ColoringBookGenerator.Services.PromptProviders;
using PrintifyGenerator.ColoringBookGenerator.Utilities;

namespace PrintifyGenerator.ColoringBookGenerator
{
    class Program
    {

        static string model = "gemma4:e2b";
        static IPromptProvider _promptProvider = null!;
        static async Task<int> Main(string[] args)
        {
            PrintBanner();

            // ── Check for --regenerate mode ──────────────────────────────────
            if (args.Any(a => a.Equals("--regenerate", StringComparison.OrdinalIgnoreCase)))
            {
                return await RunRegenerateAsync(args);
            }

            // ── Check for --finish mode ───────────────────────────────────────
            if (args.Any(a => a.Equals("--finish", StringComparison.OrdinalIgnoreCase)))
            {
                return await RunFinisherAsync(args);
            }

            // ── Load Printify token ─────────────────────────────────────────────
            var token = LoadToken();
            if (string.IsNullOrWhiteSpace(token))
            {
                Error("No TOKEN found. Add TOKEN=<your_token> to main.env in the project root.");
                return 1;
            }

            // ── Parse arguments ─────────────────────────────────────────────────
            var bookTypeArg = args.FirstOrDefault(a => a.StartsWith("--book-type=", StringComparison.OrdinalIgnoreCase))
                ?.Substring("--book-type=".Length);
            var countArg = args.FirstOrDefault(a => a.StartsWith("--count=", StringComparison.OrdinalIgnoreCase))
                ?.Substring("--count=".Length);
            var filteredArgs = args
                .Where(a => !a.StartsWith("--book-type=", StringComparison.OrdinalIgnoreCase))
                .Where(a => !a.StartsWith("--count=", StringComparison.OrdinalIgnoreCase))
                .ToArray();

            _promptProvider = bookTypeArg?.ToLowerInvariant() switch
            {
                "picturebook" or "picture" => new PictureBookPromptProvider(),
                "storybook" or "story" => new StoryBookPromptProvider(),
                "paintbynumbers" or "paint-by-numbers" or "paint" => new PaintByNumbersPromptProvider(),
                _ => new PictureBookPromptProvider()
            };

            // Print selected provider blueprint info (was previously hard-coded in the banner)
            try
            {
                var bp = _promptProvider.Blueprint;
                Info($"Blueprint : {bp.BlueprintId} · {bp.PageWidth}x{bp.PageHeight} px · {bp.PageCount} pages");
            }
            catch
            {
                // ignore if blueprint info is unavailable
            }

            // ── Collect titles — remaining args, or auto-generate from file, or auto-generate via AI ──
            var titles = new List<string>();

            if (filteredArgs.Length > 0)
            {
                titles.AddRange(filteredArgs);
            }
            else if (File.Exists("PictureBookIdeas"))
            {
                var lines = File.ReadAllLines("PictureBookIdeas")
                    .Select(l => l.Trim())
                    .Where(l => l.Length > 0 && !l.StartsWith('#'))
                    .ToArray();
                if (lines.Length > 0)
                    titles.AddRange(lines);
            }

            if (titles.Count == 0)
            {
                int count = 1;
                if (countArg != null && int.TryParse(countArg, out var parsed) && parsed > 0)
                    count = parsed;

                Step($"Auto-generating {count} title(s) via Ollama...");
                titles.AddRange(await GenerateTitlesAsync(count));
                if (titles.Count == 0)
                {
                    Error("Failed to generate any titles. Provide titles as arguments or add them to PictureBookIdeas.");
                    return 1;
                }
            }
            else if (countArg != null && int.TryParse(countArg, out var parsedTarget) && parsedTarget > titles.Count)
            {
                int extra = parsedTarget - titles.Count;
                Step($"Generating {extra} additional title(s) via Ollama to reach target of {parsedTarget}...");
                titles.AddRange(await GenerateTitlesAsync(extra));
            }

            Info($"Book type : {_promptProvider.BookType}");
            Info($"Titles    : {titles.Count}");

            // ── Wire up services (shared across all themes) ─────────────────────
            const int etsyShopId = 27152940;
            var http     = new HttpClient();
            var freeGen = new FreeGenGenerator(http, _promptProvider);
            var comfy = new ComfyUiGenerator(_promptProvider, "http://192.168.0.181:8188");
            var imageGen = new FallbackImageGenerator(freeGen, comfy);
            var printify = new PrintifyClient(token);
            var service  = new ColoringBookService(imageGen, printify, etsyShopId, _promptProvider, "http://192.168.0.181:11434", model);

            int overallExitCode = 0;

            for (int i = 0; i < titles.Count(); i++)
            {
                var title = titles[i];
                Console.WriteLine();
                Console.ForegroundColor = ConsoleColor.Cyan;
                Console.WriteLine($"  ══ Title {i + 1}/{titles.Count()}: {title} ══");
                Console.ResetColor();

                // ── Derive theme and style add-on from the provided title via Ollama ─
                string theme;
                string styleAddon;

                var (generatedTheme, generatedStyle) = await GenerateThemeAndStyleFromTitleAsync(title);

                if (generatedTheme != null && generatedStyle != null)
                {
                    theme      = generatedTheme;
                    styleAddon = generatedStyle;
                    Info($"(Derived from title via Ollama)");
                }
                else
                {
                    Warn("Ollama derivation failed — falling back to manual input.");
                    theme      = Prompt("Theme", null);
                    styleAddon = Prompt("Style add-on", null,
                                        defaultValue: "flat vector illustration");
                }

                Console.WriteLine();
                Info($"Title      : {title}");
                Info($"Theme      : {theme}");
                Info($"Style      : {styleAddon}");
                Info($"Book type  : {_promptProvider.BookType}");
                Info($"Pages      : back cover + front cover + 24 interior pages");
                Info($"Destination: My Etsy Store (shop 27152940)");
                Console.WriteLine();

                // ── Run ─────────────────────────────────────────────────────────
                var stopwatch = System.Diagnostics.Stopwatch.StartNew();

                try
                {
                    var productId = await service.GenerateAndPublishAsync(
                        title,
                        theme,
                        styleAddon,
                        onProgress: msg => Step(msg),
                        onPageProgress: (current, total) => DrawPageProgress(current, total));

                    stopwatch.Stop();
                    Console.WriteLine();
                    Success($"Done in {stopwatch.Elapsed:mm\\:ss}");
                    Success($"Product ID : {productId}");
                    Success($"View at    : https://printify.com/app/store/products/{productId}/edit");
                }
                catch (Exception ex)
                {
                    stopwatch.Stop();
                    Console.WriteLine();
                    Error($"Failed after {stopwatch.Elapsed:mm\\:ss}: {ex.Message}");
                    overallExitCode = 1;
                }
            }

            return overallExitCode;
        }

        // ── Standalone Finisher Mode ──────────────────────────────────────────

        private static async Task<int> RunFinisherAsync(string[] args)
        {
            var inputDir = args
                .FirstOrDefault(a => a.StartsWith("--input=", StringComparison.OrdinalIgnoreCase))
                ?.Substring("--input=".Length);
            var outputDir = args
                .FirstOrDefault(a => a.StartsWith("--output=", StringComparison.OrdinalIgnoreCase))
                ?.Substring("--output=".Length);
            var bookTypeArg = args
                .FirstOrDefault(a => a.StartsWith("--book-type=", StringComparison.OrdinalIgnoreCase))
                ?.Substring("--book-type=".Length);
            var noBw = args.Any(a => a.Equals("--no-bw", StringComparison.OrdinalIgnoreCase));
            var noAa = args.Any(a => a.Equals("--no-antialias", StringComparison.OrdinalIgnoreCase));
            var pageNums = args.Any(a => a.Equals("--page-numbers", StringComparison.OrdinalIgnoreCase));

            if (string.IsNullOrWhiteSpace(inputDir))
            {
                Error("Usage: --finish --input=<directory> [--output=<directory>] [--book-type=picturebook|storybook|paintbynumbers] [--no-bw] [--no-antialias] [--page-numbers]");
                return 1;
            }

            if (!Directory.Exists(inputDir))
            {
                Error($"Input directory not found: {inputDir}");
                return 1;
            }

            var provider = (bookTypeArg?.ToLowerInvariant()) switch
            {
                "picturebook" or "picture" => (IPromptProvider)new PictureBookPromptProvider(),
                "storybook" or "story" => new StoryBookPromptProvider(),
                "paintbynumbers" or "paint-by-numbers" or "paint" => new PaintByNumbersPromptProvider(),
                _ => new PictureBookPromptProvider()
            };

            var finisher = new BookFinisher(provider);
            var options = new FinishingOptions
            {
                ConvertToBlackAndWhite = !noBw,
                ApplyAntialiasing = !noAa,
                AddPageNumbers = pageNums,
                AddTitleOverlay = false,
                SaveIntermediateStages = true,
                SupersampleFactor = 8,
                GaussianBlurSigma = 1f
            };

            Step($"Book type: {provider.BookType}");
            Step($"Input: {inputDir}");
            Step($"Output: {outputDir ?? Path.Combine(inputDir, "finished")}");
            Step($"B&W conversion: {options.ConvertToBlackAndWhite}");
            Step($"Antialiasing: {options.ApplyAntialiasing}");
            Step($"Page numbers: {options.AddPageNumbers}");
            Console.WriteLine();

            var result = await finisher.FinishDirectoryAsync(inputDir, outputDir, options);
            var textContent = await finisher.GeneratePageNumberTextContentAsync(
                result.OutputDirectory,
                result.PageCount);
            var titleContent = await finisher.GenerateTitleTextContentAsync(
                result.OutputDirectory,
                "Untitled",
                "",
                provider.BuildDescription(""),
                provider.BuildTags(""));
            var subjects = provider.BuildPageSubjectsFallback("");
            var productText = await finisher.GenerateProductTextContentAsync(
                result.OutputDirectory,
                "Untitled",
                "",
                subjects,
                options);

            Success($"Finished {result.Files.Count} images ({result.CoverCount} covers, {result.PageCount} pages)");
            Success($"Output: {result.OutputDirectory}");
            Success($"Text content saved to output directory");

            return 0;
        }

        // ── Regenerate Mode ─────────────────────────────────────────────

        private static async Task<int> RunRegenerateAsync(string[] args)
        {
            var inputDir = args
                .FirstOrDefault(a => a.StartsWith("--input=", StringComparison.OrdinalIgnoreCase))
                ?.Substring("--input=".Length);
            var pageStr = args
                .FirstOrDefault(a => a.StartsWith("--page=", StringComparison.OrdinalIgnoreCase))
                ?.Substring("--page=".Length);
            var bookTypeArg = args
                .FirstOrDefault(a => a.StartsWith("--book-type=", StringComparison.OrdinalIgnoreCase))
                ?.Substring("--book-type=".Length);
            var appendArg = args
                .FirstOrDefault(a => a.StartsWith("--append=", StringComparison.OrdinalIgnoreCase))
                ?.Substring("--append=".Length);

            if (string.IsNullOrWhiteSpace(inputDir) || string.IsNullOrWhiteSpace(pageStr))
            {
                Error("Usage: --regenerate --input=<directory> --page=<number> [--append=\"prompt suffix\"] [--book-type=picturebook|storybook|paintbynumbers]");
                return 1;
            }

            if (!Directory.Exists(inputDir))
            {
                Error($"Input directory not found: {inputDir}");
                return 1;
            }

            if (!int.TryParse(pageStr, out var pageNumber) || pageNumber < 1)
            {
                Error($"Invalid page number: {pageStr}");
                return 1;
            }

            var token = LoadToken();
            if (string.IsNullOrWhiteSpace(token))
            {
                Error("No TOKEN found. Add TOKEN=<your_token> to main.env in the project root.");
                return 1;
            }

            var provider = (bookTypeArg?.ToLowerInvariant()) switch
            {
                "picturebook" or "picture" => (IPromptProvider)new PictureBookPromptProvider(),
                "storybook" or "story" => new StoryBookPromptProvider(),
                "paintbynumbers" or "paint-by-numbers" or "paint" => new PaintByNumbersPromptProvider(),
                _ => new PictureBookPromptProvider()
            };

            _promptProvider = provider;

            var http = new HttpClient();
            var freeGen = new FreeGenGenerator(http, provider);
            var comfy = new ComfyUiGenerator(provider, "http://192.168.0.181:8188");
            var imageGen = new FallbackImageGenerator(freeGen, comfy);
            var printify = new PrintifyClient(token);
            const int etsyShopId = 27152940;
            var service = new ColoringBookService(imageGen, printify, etsyShopId, provider, "http://192.168.0.181:11434", model);

            Step($"Regenerating page {pageNumber} in {inputDir}");
            Step($"Book type: {provider.BookType}");
            if (!string.IsNullOrWhiteSpace(appendArg))
                Step($"Prompt append: {appendArg}");

            try
            {
                await service.RegeneratePageAsync(inputDir, pageNumber, appendArg, msg => Step(msg));
                Success($"Alternative for page {pageNumber} created and saved in draft product");
            }
            catch (Exception ex)
            {
                Error($"Failed: {ex.Message}");
                return 1;
            }

            return 0;
        }

        // ── UI helpers ──────────────────────────────────────────────────────────

        private static void PrintBanner()
        {
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine("╔══════════════════════════════════════════════════════╗");
            Console.WriteLine("║       Printify Book Generator                        ║");
            // Blueprint info is printed after the prompt provider is selected
            Console.WriteLine("╚══════════════════════════════════════════════════════╝");
            Console.ResetColor();
            Console.WriteLine();
        }

        private static string Prompt(string label, string? argValue, string? defaultValue = null)
        {
            if (!string.IsNullOrWhiteSpace(argValue))
                return argValue!;

            var hint = defaultValue != null ? $" [{defaultValue}]" : "";
            Console.Write($"  {label}{hint}: ");
            var input = Console.ReadLine()?.Trim();

            if (string.IsNullOrWhiteSpace(input) && defaultValue != null)
                return defaultValue;

            while (string.IsNullOrWhiteSpace(input))
            {
                Console.Write($"  {label} (required): ");
                input = Console.ReadLine()?.Trim();
            }

            return input!;
        }

        private static void Step(string msg)
        {
            Console.ForegroundColor = ConsoleColor.DarkCyan;
            Console.Write("  ► ");
            Console.ResetColor();
            Console.WriteLine(msg);
        }

        private static void Info(string msg)
        {
            Console.ForegroundColor = ConsoleColor.Gray;
            Console.WriteLine($"  {msg}");
            Console.ResetColor();
        }

        private static void Success(string msg)
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine($"  ✓ {msg}");
            Console.ResetColor();
        }

        private static void Error(string msg)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"  ✗ {msg}");
            Console.ResetColor();
        }

        private static void Warn(string msg)
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine($"  ⚠ {msg}");
            Console.ResetColor();
        }

        private static void DrawPageProgress(int current, int total)
        {
            const int barWidth = 30;
            var filled = (int)Math.Round((double)current / total * barWidth);
            var bar    = new string('█', filled) + new string('░', barWidth - filled);
            var pct    = (int)Math.Round((double)current / total * 100);
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.Write($"\r  [{bar}] {current}/{total} ({pct}%)   ");
            Console.ResetColor();
            if (current == total)
                Console.WriteLine();
        }

        // ── Ollama generation ───────────────────────────────────────────────────

        private static async Task<(string? theme, string? style)> GenerateThemeAndStyleFromTitleAsync(string title)
        {
            var ollamaPrompt = _promptProvider.BuildThemeAndStylePrompt(title);

            string raw = "";
            try
            {
                PromptRecorder.Record("GenerateThemeAndStyleFromTitleAsync", "title->theme", ollamaPrompt);
                Step($"Sending request to Ollama ({model})...");
                using var ollama = new OllamaClient("http://192.168.0.181:11434");
                raw = await ollama.GenerateAsync(model, ollamaPrompt);
                Step("Ollama response received.");

                var body = raw.Trim();
                if (body.StartsWith('{'))
                {
                    using var doc = JsonDocument.Parse(body);
                    if (doc.RootElement.TryGetProperty("response", out var resp))
                        body = resp.GetString()?.Trim() ?? body;
                }
                else
                {
                    Console.WriteLine("Ollama response does not start with '{', using raw response body. " + raw);
                }

                if (body.StartsWith("```"))
                {
                    var firstNewline = body.IndexOf('\n');
                    var lastFence    = body.LastIndexOf("```");
                    if (firstNewline > 0 && lastFence > firstNewline)
                        body = body[(firstNewline + 1)..lastFence].Trim();
                }

                using var result = JsonDocument.Parse(body);
                var root = result.RootElement;
                var theme = root.TryGetProperty("theme", out var t) ? t.GetString() : null;
                var style = root.TryGetProperty("style", out var s) ? s.GetString() : null;

                if (!string.IsNullOrWhiteSpace(theme) && !string.IsNullOrWhiteSpace(style))
                {
                    theme = theme.Trim();
                    style = style.Trim();
                    var baseTerms = _promptProvider.BaseStyleTerms;
                    if (!style.Contains(baseTerms, StringComparison.OrdinalIgnoreCase))
                        style = baseTerms + ", " + style;
                    return (theme, style);
                }
            }
            catch (Exception ex)
            {
                Warn($"Ollama error: {ex.Message}" + raw);
            }

            return (null, null);
        }

        // ── Title generation ─────────────────────────────────────────────────────

        private static async Task<List<string>> GenerateTitlesAsync(int count)
        {
            var prompt = _promptProvider.BuildTitleGenerationPrompt(count);
            PromptRecorder.Record("GenerateTitlesAsync", "title_generation", prompt);

            string raw = "";
            try
            {
                Step($"Requesting {count} title(s) from Ollama ({model})...");
                using var ollama = new OllamaClient("http://192.168.0.181:11434");
                raw = await ollama.GenerateAsync(model, prompt);
                Step("Ollama title response received.");

                var body = raw.Trim();
                if (body.StartsWith('{'))
                {
                    using var doc = JsonDocument.Parse(body);
                    if (doc.RootElement.TryGetProperty("response", out var resp))
                        body = resp.GetString()?.Trim() ?? body;
                }

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
                    var titles = new List<string>();
                    foreach (var el in result.RootElement.EnumerateArray())
                    {
                        var t = el.GetString()?.Trim();
                        if (!string.IsNullOrWhiteSpace(t))
                            titles.Add(t);
                    }
                    PromptRecorder.Record("GenerateTitlesAsync", "generated_titles", string.Join("\n", titles));
                    return titles;
                }
            }
            catch (Exception ex)
            {
                Warn($"Title generation error: {ex.Message}" + raw);
            }

            return new List<string>();
        }

        // ── Token loading ───────────────────────────────────────────────────────

        private static string? LoadToken()
        {
            var env = Environment.GetEnvironmentVariable("TOKEN");
            if (!string.IsNullOrWhiteSpace(env))
                return env;

            var dir = Directory.GetCurrentDirectory();
            for (int i = 0; i < 5; i++)
            {
                var candidate = Path.Combine(dir, "main.env");
                if (File.Exists(candidate))
                {
                    foreach (var line in File.ReadAllLines(candidate))
                    {
                        var trimmed = line.Trim();
                        if (trimmed.StartsWith("TOKEN=", StringComparison.OrdinalIgnoreCase))
                            return trimmed.Substring("TOKEN=".Length).Trim();
                    }
                }
                var parent = Directory.GetParent(dir)?.FullName;
                if (parent == null) break;
                dir = parent;
            }

            return null;
        }
    }
}
