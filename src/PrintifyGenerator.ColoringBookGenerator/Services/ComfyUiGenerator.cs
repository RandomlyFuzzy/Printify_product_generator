using System;
using System.IO;
using System.Threading.Tasks;
using System.Collections.Generic;
using PrintifyGenerator.ColoringBookGenerator.Utilities;

namespace PrintifyGenerator.ColoringBookGenerator.Services
{
    public class ComfyUiGenerator : IImageGenerator
    {
        private readonly string _baseUrl;
        private readonly string _workflowPath;
        private readonly int _timeoutMinutes;

        public ComfyUiGenerator(string baseUrl = "http://192.168.0.181:8188", string? workflowPath = null, int timeoutMinutes = 10)
        {
            _baseUrl = baseUrl?.TrimEnd('/') ?? throw new ArgumentNullException(nameof(baseUrl));
            _workflowPath = workflowPath ?? Path.Combine(Directory.GetCurrentDirectory(), "src", "data", "workloads", "better_default.json");
            _timeoutMinutes = timeoutMinutes;
        }

        public async Task<string> GenerateFrontCoverAsync(string outputDirectory, string title, string theme, string styleAddon)
        {
            var prompt = BuildFrontCoverPrompt(title, theme, styleAddon);
            PromptRecorder.Record("ComfyUiGenerator", "front_cover_prompt", prompt);
            return await GenerateInternalAsync(prompt, Ratio.ratio_3_4, outputDirectory, "front_cover");
        }

        public async Task<string> GenerateBackCoverAsync(string outputDirectory, string theme, string styleAddon)
        {
            var prompt = BuildBackCoverPrompt(theme, styleAddon);
            PromptRecorder.Record("ComfyUiGenerator", "back_cover_prompt", prompt);
            return await GenerateInternalAsync(prompt, Ratio.ratio_3_4, outputDirectory, "back_cover");
        }

        public async Task<string> GeneratePageAsync(string outputDirectory, int pageNumber, string theme, string styleAddon)
        {
            var ratio = Ratio.ratio_16_9; // Standard portrait ratio for coloring book pages
            if (pageNumber == 1 || pageNumber == 24) ratio = Ratio.ratio_3_4; // Could customize first and last page if desired
            var prompt = BuildPagePrompt(pageNumber, theme, styleAddon);
            PromptRecorder.Record("ComfyUiGenerator", $"page_{pageNumber:D2}_prompt", prompt);
            return await GenerateInternalAsync(prompt, ratio, outputDirectory, $"page_{pageNumber:D2}");
        }

        private async Task<string> GenerateInternalAsync(string prompt, Ratio ratioId, string outputDirectory, string baseName)
        {
            if (string.IsNullOrWhiteSpace(prompt)) prompt = ".";
            if (prompt.Length > 2000) prompt = prompt.Substring(0, 2000);

            Directory.CreateDirectory(outputDirectory);

            if (!File.Exists(_workflowPath))
                throw new FileNotFoundException("ComfyUI workflow not found", _workflowPath);

            // Load and update workflow JSON directly
            var workflowJson = File.ReadAllText(_workflowPath);
            var workflow = System.Text.Json.Nodes.JsonNode.Parse(workflowJson)!;

            // Set workflow parameters (update these paths as needed for your workflow structure)
            // Example: update positive prompt, width, height, steps, cfg, save_prefix
            workflow = JsonXPath.Set(workflow, "//30:45/inputs/text", System.Text.Json.Nodes.JsonValue.Create(prompt)!);
            workflow = JsonXPath.Set(workflow, "//30:41/inputs/width", System.Text.Json.Nodes.JsonValue.Create(MapRatioToSize(ratioId).w)!);
            workflow = JsonXPath.Set(workflow, "//30:41/inputs/height", System.Text.Json.Nodes.JsonValue.Create(MapRatioToSize(ratioId).h)!);
            workflow = JsonXPath.Set(workflow, "//30:44/inputs/steps", System.Text.Json.Nodes.JsonValue.Create(28)!);
            workflow = JsonXPath.Set(workflow, "//30:44/inputs/cfg", System.Text.Json.Nodes.JsonValue.Create(7.0)!);
            workflow = JsonXPath.Set(workflow, "//30:41/inputs/save_prefix", System.Text.Json.Nodes.JsonValue.Create(baseName)!);

            var emitter = new WebSocketEventEmitter();
            await using var comfyClient = new ComfyUiClient(_baseUrl, emitter);
            try { await comfyClient.StartListener(); } catch { }
            var promptId = comfyClient.QueuePrompt(workflow);
            var timeoutAt = DateTimeOffset.UtcNow.AddMinutes(_timeoutMinutes);

            Console.WriteLine($"[ComfyUI] Queued prompt {promptId} (base: {_baseUrl})");

            JobStatus status;
            string lastStatus = null;
            double lastProgress = -1;
            DateTime lastLog = DateTime.MinValue;

            do
            {
                status = comfyClient.GetJob(promptId);
                var s = status?.Status ?? "unknown";
                var p = status?.Progress ?? 0.0;
                var node = status?.CurrentNode ?? string.Empty;
                var imgCount = status?.ImageUrls?.Count ?? 0;

                // Log when status changes, progress moves noticeably, or periodically every 10s
                if (s != lastStatus || Math.Abs(p - lastProgress) > 0.5 || (DateTime.UtcNow - lastLog).TotalSeconds > 10)
                {
                    Console.WriteLine($"[ComfyUI] prompt={promptId} status={s} progress={Math.Round(p,1)}% node=\"{node}\" images={imgCount}");
                    lastStatus = s;
                    lastProgress = p;
                    lastLog = DateTime.UtcNow;
                }

                if (string.Equals(s, "failed", StringComparison.OrdinalIgnoreCase))
                {
                    Console.WriteLine($"[ComfyUI] prompt {promptId} failed.");
                    throw new Exception("ComfyUI generation failed.");
                }

                if (DateTimeOffset.UtcNow > timeoutAt)
                {
                    Console.WriteLine($"[ComfyUI] prompt {promptId} timed out after {_timeoutMinutes} minutes.");
                    throw new Exception("ComfyUI generation timed out.");
                }

                await Task.Delay(2000);
            }
            while (!string.Equals(status?.Status, "completed", StringComparison.OrdinalIgnoreCase));

            Console.WriteLine($"[ComfyUI] prompt {promptId} completed — downloading outputs (images={status.ImageUrls?.Count ?? 0})...");
            var downloaded = await status.DownloadAllImagesAsync(outputDirectory, baseName);
            Console.WriteLine($"[ComfyUI] downloaded: {downloaded}");
            if (!string.IsNullOrWhiteSpace(downloaded))
                return downloaded;

            throw new Exception("ComfyUI generation completed but no image was downloaded.");
        }

        private static (int w, int h) MapRatioToSize(Ratio r)
        {
            return r switch
            {
                Ratio.ratio_4_3 => (1024, 768),
                Ratio.ratio_3_4 => (768, 1024),
                Ratio.ratio_1_1 => (1024, 1024),
                Ratio.ratio_16_9 => (1280, 720),
                Ratio.ratio_9_16 => (720, 1280),
                _ => (768, 1024),
            };
        }

        private static string BuildFrontCoverPrompt(string title, string theme, string styleAddon)
        {
            return $@"
        Create a high-quality black and white coloring book FRONT COVER illustration.

        Title:
        {title}

        Theme:
        {theme}

        CRITICAL TITLE REQUIREMENT:
        - The title MUST be clearly visible and perfectly legible
        - The title must be well-formed, correctly spelled, and centered
        - Use a decorative but readable lettering style suitable for coloring books
        - The title should be integrated into the cover design (not floating randomly)
        - Ensure strong contrast so the text is readable in black and white line art

        Style Requirements:
        - Pure black and white line art only
        - No grayscale, shading, or color
        - Clean bold outlines suitable for coloring
        - Highly detailed but printable
        - White background
        - Professional coloring book cover illustration style
        - Kid-friendly and visually engaging

        Additional Style:
        {styleAddon}

        Composition:
        - Prominent central focal illustration related to the theme
        - Title positioned prominently (top or center depending on composition balance)
        - Decorative framing elements around the title
        - Balanced, symmetrical, polished cover layout
        - Leave clear separation between title and illustration elements
        - Avoid clutter that reduces title readability
        - No extra text besides the title

        Output Style:
        Intricate ink illustration, vector-style line art, crisp monochrome outlines, professional coloring book cover design.
        ";
        }

        private static string BuildBackCoverPrompt(string theme, string styleAddon)
        {
            return $@"
            Create a high-quality black and white coloring book illustration for the BACK COVER of a coloring book.

            Theme:
            {theme}

            Style Requirements:
            - Pure black and white line art only
            - No grayscale, shading, or color
            - Clean bold outlines suitable for coloring
            - Highly detailed but printable
            - White background
            - Coloring book page aesthetic
            - Kid-friendly and visually engaging
            - Balanced composition with decorative borders and background elements
            - Include whimsical and intricate patterns
            - Professional coloring book illustration style

            Additional Style:
            {styleAddon}

            Composition:
            - Full-page vertical layout
            - Leave some open spaces for coloring
            - Center-focused design
            - Symmetrical and visually appealing
            - Avoid text, logos, or watermarks

            Output Style:
            Intricate ink illustration, vector-style line art, coloring book page, crisp outlines, monochrome drawing.
        ";
        }

        private static string BuildPagePrompt(int pageNumber, string theme, string styleAddon)
        {
            return $@"
        Create a high-quality black and white coloring book illustration for PAGE {pageNumber} of a coloring book.

        Theme:
        {theme}

        Style Requirements:
        - Pure black and white line art only
        - No grayscale, shading, or color
        - Clean bold outlines suitable for coloring
        - Highly detailed but printable
        - White background
        - Coloring book page aesthetic
        - Kid-friendly and visually engaging
        - Intricate patterns and decorative elements
        - Professional coloring book illustration style

        Additional Style:
        {styleAddon}

        Composition:
        - Full-page vertical layout
        - Unique scene composition for this page
        - Center-focused subject with supporting background details
        - Balanced use of open spaces for coloring
        - Include depth and layered elements
        - Avoid text, logos, watermarks, or page numbers inside the image

        Page Design Guidance:
        - Make this page visually distinct from other pages
        - Add thematic objects, scenery, and ornamental details related to the theme
        - Ensure the illustration feels immersive and creative
        - Maintain consistent art style across all pages

        Output Style:
        Intricate ink illustration, vector-style line art, crisp monochrome outlines, detailed coloring book page.
        ";
        }
    }
}
