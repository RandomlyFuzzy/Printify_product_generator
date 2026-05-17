using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using PrintifyGenerator.ColoringBookGenerator.Services;
using PrintifyGenerator.ColoringBookGenerator.Utilities;

namespace PrintifyGenerator.ColoringBookGenerator
{
    class Program
    {

        static string model = "gemma4:e2b";
        static async Task<int> Main(string[] args)
        {
            PrintBanner();

            // ── Load Printify token ─────────────────────────────────────────────
            var token = LoadToken();
            if (string.IsNullOrWhiteSpace(token))
            {
                Error("No TOKEN found. Add TOKEN=<your_token> to main.env in the project root.");
                return 1;
            }
            string[] lines = File.ReadAllLines("PictureBookIdeas");

            Console.WriteLine(args.Length);
    
            // ── Collect titles — each argument is a separate book title ──────────
            var titles = args.Length > 1
                ? args.ToList()
                : lines.ToList();


            // ── Wire up services (shared across all themes) ─────────────────────
            const int etsyShopId = 27152940;
            var http     = new HttpClient();
            // Primary: FreeGen (cloud), Fallback: local/remote ComfyUI so generation continues during rate-limits
            var freeGen = new FreeGenGenerator(http);
            var comfy = new ComfyUiGenerator("http://192.168.0.181:8188");
            var imageGen = new FallbackImageGenerator(freeGen, comfy);
            var printify = new PrintifyClient(token);
            var service  = new ColoringBookService(imageGen, printify, etsyShopId, "http://192.168.0.181:11434", model);

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
                                        defaultValue: "flat vector illustration,");
                }

                Console.WriteLine();
                Info($"Title      : {title}");
                Info($"Theme      : {theme}");
                Info($"Style      : {styleAddon}");
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

        // ── UI helpers ──────────────────────────────────────────────────────────

        private static void PrintBanner()
        {
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine("╔══════════════════════════════════════════════════════╗");
            Console.WriteLine("║          Printify Coloring Book Generator            ║");
            Console.WriteLine("║  blueprint 2721 · District Photo · 8.5\" × 11\"        ║");
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

        private static async Task<(string? title, string? style)> GenerateTitleAndStyleAsync(string theme)
        {
            var ollamaPrompt =
                @"You are a product copywriter for a print-on-demand coloring book store.

Given a theme, generate a single valid JSON object with two fields: ""title"" and ""style"".

────────────────────
FIELD 1: ""title""
────────────────────
Requirements:
- 4–8 words total
- Must end with exactly: ""Coloring Book"" or ""Colouring Book""
- Must be marketable, specific, and appealing
- Avoid generic marketing words (e.g., Amazing, Ultimate, Best, Fun)
- Choose the most appropriate audience based on the theme:
  - Children (ages 4–10) OR Adult relaxation / mindfulness

Good examples:
- ""Jungle Safari Adventure Coloring Book""
- ""Magical Fairy Garden Coloring Book""
- ""Cute Kawaii Animals Colouring Book""

────────────────────
FIELD 2: ""style""
────────────────────
This is a comma-separated list of AI image-generation keywords for a single illustration.

Rules:
- DO NOT describe a book page, paper, borders, or page layout
- DO NOT include ""coloring book page"" or any physical medium references
- The output must describe a clean standalone illustration only

- MUST always include these base terms:
  ""thick black outlines, white fill, no shading, no color""

- Add 3–5 theme-specific descriptors describing:
  • subject matter (e.g., jungle animals, fantasy creatures, space scenes)
  • visual mood (e.g., cute, whimsical, serene, playful, detailed, intricate)
  • composition style (e.g., centered composition, symmetrical design, isolated scene)

IMPORTANT QUALITY REQUIREMENTS:
- Output must be visually clean and free of artifacts
- Ensure the description produces crisp, uncluttered line art
- Avoid complex background noise or messy compositions
- Prefer clear separation between subjects and background elements

Good examples:
- ""thick black outlines, white fill, no shading, no color, cute cartoon jungle animals, playful composition, clean isolated scene""
- ""thick black outlines, white fill, no shading, no color, intricate mandala patterns, symmetrical design, floral geometry, clean crisp lines""

────────────────────
THEME
────────────────────
Theme: ""{theme}""

────────────────────
OUTPUT RULES
────────────────────
- Return ONLY one valid JSON object
- No markdown, no explanations, no extra text
- Ensure clean, structured text output with no artifacts or formatting noise
- Output must start with { and end with }
- Must be valid JSON (all keys and values use double quotes)
- Do not include single quotes anywhere

Example:
{""title"":""Jungle Safari Adventure Coloring Book"",""style"":""thick black outlines, white fill, no shading, no color, cute jungle animals, tropical setting, clean isolated composition""}";

            string raw = "";
            try
            {
                // record the prompt for later inspection
                PromptRecorder.Record("GenerateTitleAndStyleAsync", "theme->title", ollamaPrompt);
                Step($"Sending request to Ollama ({model})...");
                using var ollama = new OllamaClient("http://192.168.0.181:11434");
                raw = await ollama.GenerateAsync(model, ollamaPrompt);
                Step("Ollama response received.");

                // Extract the "response" field from the Ollama envelope
                var body = raw.Trim();
                if (body.StartsWith('{'))
                {
                    using var doc = JsonDocument.Parse(body);
                    if (doc.RootElement.TryGetProperty("response", out var resp))
                        body = resp.GetString()?.Trim() ?? body;
                }else {
                    Console.WriteLine("Ollama response does not start with '{', using raw response body. "+raw);
                }

                // Strip markdown fences if present
                if (body.StartsWith("```"))
                {
                    var firstNewline = body.IndexOf('\n');
                    var lastFence    = body.LastIndexOf("```");
                    if (firstNewline > 0 && lastFence > firstNewline)
                        body = body[(firstNewline + 1)..lastFence].Trim();
                }

                using var result = JsonDocument.Parse(body);
                var root = result.RootElement;
                var title = root.TryGetProperty("title", out var t) ? t.GetString() : null;
                var style = root.TryGetProperty("style", out var s) ? s.GetString() : null;

                if (!string.IsNullOrWhiteSpace(title) && !string.IsNullOrWhiteSpace(style))
                    return (title.Trim(), style.Trim());
            }
            catch (Exception ex)
            {
                Warn($"Ollama error: {ex.Message}"+raw);
            }

            return (null, null);
        }

        private static async Task<(string? theme, string? style)> GenerateThemeAndStyleFromTitleAsync(string title)
        {
            var ollamaPrompt = $@"You are a product copywriter specialising in children's coloring books (ages 3-10).

Task: Given a single BOOK TITLE, produce a concise 'theme' and a 'style' string suitable for generating interior coloring illustrations.

Rules for the 'theme' field:
- 10-20 words, lower-case, no punctuation suitable for a child to understand.
- Describe the interior subject (e.g. 'jungle animals', 'space robots', 'cozy bakery').
- Do NOT include the words 'coloring', 'colouring', or 'book'.

Rules for the 'style' field:
- A comma-separated list of image-generation keywords for ONE illustration.
- MUST include the base terms: ""thick black outlines, white fill, no shading, no color"".
- Add 3–5 short descriptors (subject matter, mood, composition). Keep phrases short.
- Do NOT mention paper, page layout, borders, or the words 'coloring book'.

BOOK TITLE:
""{title}""

OUTPUT FORMAT:
- Return ONLY one valid JSON object with exactly two fields: ""theme"" and ""style"".
- No markdown, no explanation, no extra text. Start with {{ and end with }}.

Examples:
{{""theme"":""jungle animals"",""style"":""thick black outlines, white fill, no shading, no color, cute jungle animals, playful composition, centered subject""}}
{{""theme"":""cozy witch bakery"",""style"":""thick black outlines, white fill, no shading, no color, cozy witch baking, whimsical kitchen, detailed props""}}";

            string raw = "";
            try
            {
                // record the prompt so it can be reproduced later
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
                    var baseTerms = "thick black outlines, white fill, no shading, no color";
                    if (!style.Contains("thick black outlines", StringComparison.OrdinalIgnoreCase))
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

        // ── Token loading ───────────────────────────────────────────────────────

        private static string? LoadToken()
        {
            // Check environment variable first
            var env = Environment.GetEnvironmentVariable("TOKEN");
            if (!string.IsNullOrWhiteSpace(env))
                return env;

            // Walk up from cwd looking for main.env
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

