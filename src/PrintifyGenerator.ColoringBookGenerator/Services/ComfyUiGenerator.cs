using System;
using System.IO;
using System.Threading.Tasks;
using System.Collections.Generic;
using PrintifyGenerator.ColoringBookGenerator.Utilities;
using PrintifyGenerator.ColoringBookGenerator.Services.PromptProviders;
using PrintifyGenerator.ColoringBookGenerator.Models;

namespace PrintifyGenerator.ColoringBookGenerator.Services
{
    public class ComfyUiGenerator : IImageGenerator
    {
        private readonly string _baseUrl;
        private readonly string _workflowPath;
        private readonly int _timeoutMinutes;
        private readonly IPromptProvider _promptProvider;
        private readonly BookFinisher _finisher;

        public ComfyUiGenerator(IPromptProvider promptProvider, string baseUrl = "http://192.168.0.181:8188", string? workflowPath = null, int timeoutMinutes = 10)
        {
            _baseUrl = baseUrl?.TrimEnd('/') ?? throw new ArgumentNullException(nameof(baseUrl));
            _workflowPath = workflowPath ?? Path.Combine(Directory.GetCurrentDirectory(), "src", "data", "workloads", "better_default.json");
            _timeoutMinutes = timeoutMinutes;
            _promptProvider = promptProvider;
            _finisher = new BookFinisher(promptProvider);
        }

        public async Task<string> GenerateFrontCoverAsync(string outputDirectory, string title, string theme, string styleAddon, string? promptPrefix = null)
        {
            var basePrompt = _promptProvider.BuildFrontCoverPrompt(title, theme, styleAddon);
            var prompt = string.IsNullOrWhiteSpace(promptPrefix) ? basePrompt : (promptPrefix + "\n" + basePrompt);
            PromptRecorder.Record("ComfyUiGenerator", "front_cover_prompt", prompt);
            return await GenerateInternalAsync(prompt, Ratio.ratio_3_4, outputDirectory, "front_cover");
        }

        public async Task<string> GenerateBackCoverAsync(string outputDirectory, string theme, string styleAddon, string? promptPrefix = null)
        {
            var basePrompt = _promptProvider.BuildBackCoverPrompt(theme, styleAddon);
            var prompt = string.IsNullOrWhiteSpace(promptPrefix) ? basePrompt : (promptPrefix + "\n" + basePrompt);
            PromptRecorder.Record("ComfyUiGenerator", "back_cover_prompt", prompt);
            return await GenerateInternalAsync(prompt, Ratio.ratio_3_4, outputDirectory, "back_cover");
        }

        public async Task<string> GeneratePageAsync(string outputDirectory, int pageNumber, string theme, string styleAddon, string? promptPrefix = null)
        {
            var ratio = Ratio.ratio_16_9;
            if (pageNumber == 1 || pageNumber == 24) ratio = Ratio.ratio_3_4;
            var basePrompt = _promptProvider.BuildPagePrompt(pageNumber, theme, styleAddon);
            var prompt = string.IsNullOrWhiteSpace(promptPrefix) ? basePrompt : (promptPrefix + "\n" + basePrompt);
            PromptRecorder.Record("ComfyUiGenerator", $"page_{pageNumber:D2}_prompt", prompt);
            return await GenerateInternalAsync(prompt, ratio, outputDirectory, $"page_{pageNumber:D2}");
        }

        public async Task<string> GenerateImageFromJobAsync(string outputDirectory, GenerationJob job, string? promptPrefix = null)
        {
            var ratio = ParseRatio(job.AspectRatio);
            var basePrompt = job.Prompt ?? string.Empty;
            var prompt = string.IsNullOrWhiteSpace(promptPrefix) ? basePrompt : (promptPrefix + "\n" + basePrompt);
            PromptRecorder.Record("ComfyUiGenerator", $"job_{job.PageLabel}_prompt", prompt);
            return await GenerateInternalAsync(prompt, ratio, outputDirectory, job.PageLabel);
        }

        private static Ratio ParseRatio(string ratio)
        {
            return ratio?.Replace(":", "_") switch
            {
                "4_3" => Ratio.ratio_4_3,
                "3_4" => Ratio.ratio_3_4,
                "1_1" => Ratio.ratio_1_1,
                "16_9" => Ratio.ratio_16_9,
                "9_16" => Ratio.ratio_9_16,
                _ => Ratio.ratio_3_4
            };
        }

        private async Task<string> GenerateInternalAsync(string prompt, Ratio ratioId, string outputDirectory, string baseName)
        {
            if (string.IsNullOrWhiteSpace(prompt)) prompt = ".";
            if (prompt.Length > 2000) prompt = prompt.Substring(0, 2000);

            PromptRecorder.Record("ComfyUiGenerator", $"final_prompt_{baseName}", prompt);

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
            {
                // Apply BookFinisher processing
                var pageNum = ExtractPageNumber(baseName);
                var options = new FinishingOptions
                {
                    ConvertToBlackAndWhite = _finisher.ShouldConvertToBlackAndWhite,
                    ApplyAntialiasing = true,
                    AddPageNumbers = false,
                    AddTitleOverlay = false,
                    SaveIntermediateStages = true,
                    SupersampleFactor = 8,
                    GaussianBlurSigma = 1f
                };
                var finishedPath = await _finisher.FinishImageAsync(downloaded, outputDirectory, pageNum, options);
                Console.WriteLine($"[ComfyUI] finished: {finishedPath}");
                return finishedPath;
            }

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

        private static int? ExtractPageNumber(string baseName)
        {
            var match = System.Text.RegularExpressions.Regex.Match(baseName, @"page_(\d+)", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
            if (match.Success && int.TryParse(match.Groups[1].Value, out var pn))
                return pn;
            return null;
        }
    }
}
