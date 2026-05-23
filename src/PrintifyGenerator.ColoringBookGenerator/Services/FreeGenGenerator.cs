using System;
using System.Net.WebSockets;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading;
using System.Threading.Tasks;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using PrintifyGenerator.ColoringBookGenerator.Utilities;
using PrintifyGenerator.ColoringBookGenerator.Services.PromptProviders;
using PrintifyGenerator.ColoringBookGenerator.Models;

namespace PrintifyGenerator.ColoringBookGenerator.Services
{
    public class FreeGenGenerator : IImageGenerator
    {
        private readonly HttpClient _http;
        private readonly IPromptProvider _promptProvider;
        private readonly BookFinisher _finisher;

        private readonly string _signerUrl = "https://prompt-signer.freegen.app";
        private readonly string _generatorUrl = "https://image-generator.freegen.app";
        private readonly string _websocketUrl = "wss://websocket-bridge.freegen.app/ws";
        private readonly string _statsApiUrl = "https://stats.freegen.app";

        public FreeGenGenerator(HttpClient http, IPromptProvider promptProvider)
        {
            _http = http;
            _promptProvider = promptProvider;
            _finisher = new BookFinisher(promptProvider);
        }

        // When the remote FreeGen reports a rate-limit, we mark it unavailable
        // until the reported retry time. While unavailable, generation will
        // attempt ComfyUI fallback.
        private static readonly object _freeGenStateLock = new();
        private static DateTime _freeGenUnavailableUntilUtc = DateTime.MinValue;
        // Expose current FreeGen unavailable-until for external checks (fallback routing)
        public static DateTime FreeGenUnavailableUntilUtc
        {
            get
            {
                lock (_freeGenStateLock)
                {
                    return _freeGenUnavailableUntilUtc;
                }
            }
        }

        // ── Public API ─────────────────────────────────────────────────────────

        /// <summary>Front (right-side) cover — full color, portrait 3:4.</summary>
        public async Task<string> GenerateFrontCoverAsync(string outputDirectory, string title, string theme, string styleAddon, string? promptPrefix = null)
        {
            var basePrompt = _promptProvider.BuildFrontCoverPrompt(title, theme, styleAddon);
            var prompt = string.IsNullOrWhiteSpace(promptPrefix) ? basePrompt : (promptPrefix + "\n" + basePrompt);
            PromptRecorder.Record("FreeGenGenerator", "front_cover_prompt", prompt);
            var result = await GenerateInternalAsync(prompt, Ratio.ratio_3_4, "front_cover");
            return await SaveImageResultAsync(result, outputDirectory, "front_cover");
        }

        public async Task<string> GenerateBackCoverAsync(string outputDirectory, string theme, string styleAddon, string? promptPrefix = null)
        {
            var basePrompt = _promptProvider.BuildBackCoverPrompt(theme, styleAddon);
            var prompt = string.IsNullOrWhiteSpace(promptPrefix) ? basePrompt : (promptPrefix + "\n" + basePrompt);
            PromptRecorder.Record("FreeGenGenerator", "back_cover_prompt", prompt);
            var result = await GenerateInternalAsync(prompt, Ratio.ratio_3_4, "back_cover");
            return await SaveImageResultAsync(result, outputDirectory, "back_cover");
        }

        public async Task<string> GeneratePageAsync(string outputDirectory, int pageNumber, string theme, string styleAddon, string? promptPrefix = null)
        {
            var ratio = Ratio.ratio_16_9;
            if (pageNumber == 1 || pageNumber == 24) ratio = Ratio.ratio_3_4;
            var basePrompt = _promptProvider.BuildPagePrompt(pageNumber, theme, styleAddon);
            var prompt = string.IsNullOrWhiteSpace(promptPrefix) ? basePrompt : (promptPrefix + "\n" + basePrompt);
            PromptRecorder.Record("FreeGenGenerator", $"page_{pageNumber:D2}_prompt", prompt);
            var result = await GenerateInternalAsync(prompt, ratio, $"page_{pageNumber:D2}");
            return await SaveImageResultAsync(result, outputDirectory, $"page_{pageNumber:D2}");
        }

        public async Task<string> GenerateImageFromJobAsync(string outputDirectory, GenerationJob job, string? promptPrefix = null)
        {
            var ratio = ParseRatio(job.AspectRatio);
            var basePrompt = job.Prompt ?? string.Empty;
            var prompt = string.IsNullOrWhiteSpace(promptPrefix) ? basePrompt : (promptPrefix + "\n" + basePrompt);
            PromptRecorder.Record("FreeGenGenerator", $"job_{job.PageLabel}_prompt", prompt);
            var result = await GenerateInternalAsync(prompt, ratio, job.PageLabel);
            var rawPath = await SaveImageResultAsync(result, outputDirectory, job.PageLabel);
            job.OutputPath = rawPath;
            return rawPath;
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

        private async Task<string> GenerateInternalAsync(string prompt, Ratio ratioId, string src = "unknown")
        {
            if (string.IsNullOrWhiteSpace(prompt))
                prompt = ".";

            if (prompt.Length > 2000)
            {
                prompt = prompt.Substring(0, 2000);
                Console.WriteLine("Warning: Prompt exceeded 2000 characters and was truncated. This may affect image quality.");
            }

            PromptRecorder.Record("FreeGenGenerator", $"final_prompt_{src}", prompt);

            // Rate-limit acquisitions: ensure we only generate up to 100 images per hour
            await Utilities.ImageRateLimiter.Instance.AcquireAsync();

            // If FreeGen is currently marked unavailable (rate-limited), try ComfyUI
            // fallback during the remaining window instead of contacting FreeGen.
            DateTime now = DateTime.UtcNow;
            DateTime unavailableUntil;
            lock (_freeGenStateLock)
            {
                unavailableUntil = _freeGenUnavailableUntilUtc;
            }

            if (now < unavailableUntil)
            {
                var remaining = (unavailableUntil - now).TotalSeconds;
                try
                {
                    var comfy = await TryComfyUiFallbackAsync(prompt, ratioId, remaining);
                    if (!string.IsNullOrWhiteSpace(comfy))
                        return comfy;
                }
                catch
                {
                    // best-effort fallback; if comfy fails, we'll attempt FreeGen below
                }
            }

            var signerRes = await _http.PostAsync(
                _signerUrl,
                new StringContent(
                    JsonSerializer.Serialize(new { prompt }),
                    Encoding.UTF8,
                    "application/json"));

            if (!signerRes.IsSuccessStatusCode)
            {
                if (signerRes.StatusCode == System.Net.HttpStatusCode.TooManyRequests)
                {
                    // Mark FreeGen unavailable for the reported retry window (try Retry-After header first)
                    var retrySeconds = 60;
                    try
                    {
                        if (signerRes.Headers.TryGetValues("Retry-After", out var vals))
                        {
                            var v = vals.FirstOrDefault();
                            if (int.TryParse(v, out var parsed))
                                retrySeconds = Math.Max(1, parsed);
                            else if (DateTimeOffset.TryParse(v, out var dt))
                                retrySeconds = (int)Math.Max(1, (dt.ToUniversalTime() - DateTime.UtcNow).TotalSeconds);
                        }
                    }
                    catch { }

                    lock (_freeGenStateLock)
                    {
                        _freeGenUnavailableUntilUtc = DateTime.UtcNow.AddSeconds(retrySeconds);
                    }

                    // Best-effort: try ComfyUI fallback during the retry window
                    try
                    {
                        var comfy = await TryComfyUiFallbackAsync(prompt, ratioId, retrySeconds);
                        if (!string.IsNullOrWhiteSpace(comfy))
                            return comfy;
                    }
                    catch { }

                    throw new Exception("Too many requests. Please try again later.");
                }

                throw new Exception($"Signer Error ({(int)signerRes.StatusCode})");
            }

            var signer = JsonSerializer.Deserialize<SignerResponse>(
                await signerRes.Content.ReadAsStringAsync());

            var body = new Dictionary<string, object>
            {
                ["prompt"] = prompt,
                ["ts"] = signer.ts,
                ["sig"] = signer.sig,
                ["ratio_id"] = RatioToString(ratioId)
            };


            var genRes = await _http.PostAsync(
                _generatorUrl,
                new StringContent(JsonSerializer.Serialize(body), Encoding.UTF8, "application/json"));

            var genJson = await genRes.Content.ReadAsStringAsync();

            // Detect provider-side rate-limit payload (may be 429 or 200 with an error body)
            try
            {
                using var doc = JsonDocument.Parse(genJson ?? "{}");
                if (doc.RootElement.ValueKind == JsonValueKind.Object &&
                    doc.RootElement.TryGetProperty("retry_after_seconds", out var retryProp))
                {
                    var retrySeconds = retryProp.GetInt32();
                    lock (_freeGenStateLock)
                    {
                        _freeGenUnavailableUntilUtc = DateTime.UtcNow.AddSeconds(retrySeconds);
                    }

                    // Try ComfyUI fallback for the duration of retrySeconds
                    var comfy = await TryComfyUiFallbackAsync(prompt, ratioId, retrySeconds);
                    if (!string.IsNullOrWhiteSpace(comfy))
                        return comfy;

                    throw new Exception($"FreeGen rate-limited. Retry after {retrySeconds}s and ComfyUI fallback failed.");
                }
            }
            catch
            {
                // ignore parse errors and continue handling status codes below
            }

            if (!genRes.IsSuccessStatusCode)
            {
                if (genRes.StatusCode == System.Net.HttpStatusCode.TooManyRequests)
                {
                    // If the generator returned 429 without a JSON retry payload, mark unavailable and try ComfyUI
                    var retrySeconds = 60;
                    try
                    {
                        if (genRes.Headers.TryGetValues("Retry-After", out var vals))
                        {
                            var v = vals.FirstOrDefault();
                            if (int.TryParse(v, out var parsed))
                                retrySeconds = Math.Max(1, parsed);
                            else if (DateTimeOffset.TryParse(v, out var dt))
                                retrySeconds = (int)Math.Max(1, (dt.ToUniversalTime() - DateTime.UtcNow).TotalSeconds);
                        }
                    }
                    catch { }

                    lock (_freeGenStateLock)
                    {
                        _freeGenUnavailableUntilUtc = DateTime.UtcNow.AddSeconds(retrySeconds);
                    }

                    try
                    {
                        var comfy = await TryComfyUiFallbackAsync(prompt, ratioId, retrySeconds);
                        if (!string.IsNullOrWhiteSpace(comfy))
                            return comfy;
                    }
                    catch { }

                    throw new Exception("Too many requests. Please try again later.");
                }

                throw new Exception($"Generator Error ({(int)genRes.StatusCode})");
            }

            var data = JsonSerializer.Deserialize<GeneratorResponse>(genJson);

            var startTime = DateTime.UtcNow;

            if (!string.IsNullOrWhiteSpace(data?.job_id))
            {
                return await WaitForJobAsync(data.job_id, startTime);
            }

            if (!string.IsNullOrWhiteSpace(data?.image_data_url))
            {
                await RecordStatsAsync("fallback_" + DateTime.UtcNow.Ticks, startTime);
                return data.image_data_url;
            }

            throw new Exception("Failed to generate image.");
        }

        private async Task<string> WaitForJobAsync(string jobId, DateTime startTime)
        {
            using var ws = new ClientWebSocket();
            await ws.ConnectAsync(new Uri(_websocketUrl), CancellationToken.None);

            var auth = await CreateAuthAsync(jobId);

            var subscribe = new
            {
                type = "subscribe",
                job_id = jobId,
                auth = auth
            };

            await ws.SendAsync(
                Encoding.UTF8.GetBytes(JsonSerializer.Serialize(subscribe)),
                WebSocketMessageType.Text,
                true,
                CancellationToken.None);

            var buffer = new byte[65536];

            while (ws.State == WebSocketState.Open)
            {
                using var ms = new MemoryStream();
                WebSocketReceiveResult result;
                do
                {
                    result = await ws.ReceiveAsync(buffer, CancellationToken.None);
                    if (result.MessageType == WebSocketMessageType.Close)
                        break;
                    ms.Write(buffer, 0, result.Count);
                } while (!result.EndOfMessage);

                if (result.MessageType == WebSocketMessageType.Close)
                    break;

                var raw = Encoding.UTF8.GetString(ms.ToArray()).Trim();

                // Console.WriteLine($"  [WS raw] {raw[..Math.Min(160, raw.Length)]}");

                // If the message is not JSON it is the raw base64 JPEG image
                if (!raw.StartsWith('{'))
                {
                    Console.WriteLine("  [WS] received raw base64 image data");
                    await RecordStatsAsync(jobId, startTime);
                    return raw;
                }

                var msg = JsonSerializer.Deserialize<WsMessage>(raw);
                if (msg == null) continue;

                // Console.WriteLine($"  [WS] type={msg.type ?? "?"} message={msg.message ?? "-"}");

                switch (msg.type)
                {
                    case "status":
                        break;

                    case "result":
                        await RecordStatsAsync(jobId, startTime);
                        return msg.image_data
                            ?? throw new Exception("Result message had no image_data field.");

                    case "error":

                        throw new Exception(msg.message ?? "Generation failed");
                }
            }

            throw new Exception("WebSocket closed without delivering image data.");
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

        private async Task<string?> TryComfyUiFallbackAsync(string prompt, Ratio ratioId, double maxWaitSeconds)
        {
            // Try to run ComfyUI workflow at the provided IP while FreeGen is waiting.
            // This is best-effort: return the first image URL on success, or null on failure.
            try
            {
                var workflowPath = Path.Combine(Directory.GetCurrentDirectory(), "src", "data", "workloads", "better_default.json");
                if (!File.Exists(workflowPath))
                    return null;

                var workflow = JsonXPath.Load(workflowPath);
                workflow = JsonXPath.Set(workflow, "//30:45/inputs/text", JsonValue.Create(prompt)!);
                workflow = JsonXPath.Set(workflow, "//30:85/inputs/text", JsonValue.Create(string.Empty)!);

                var (w, h) = MapRatioToSize(ratioId);
                workflow = JsonXPath.Set(workflow, "//30:41/inputs/width", JsonValue.Create(w)!);
                workflow = JsonXPath.Set(workflow, "//30:41/inputs/height", JsonValue.Create(h)!);

                // sensible defaults
                workflow = JsonXPath.Set(workflow, "//30:44/inputs/steps", JsonValue.Create(28)!);
                workflow = JsonXPath.Set(workflow, "//30:44/inputs/cfg", JsonValue.Create(7.0)!);

                var emitter = new WebSocketEventEmitter();
                await using var comfyClient = new ComfyUiClient("http://192.168.0.181:8188", emitter);
                try
                {
                    await comfyClient.StartListener();
                }
                catch
                {
                    // if websocket can't start, we still try the HTTP prompt API below
                }

                var promptId = comfyClient.QueuePrompt(workflow);
                var timeoutAt = DateTimeOffset.UtcNow.AddSeconds(Math.Max(5, Math.Min(maxWaitSeconds, 600)));

                JobStatus status;
                do
                {
                    status = comfyClient.GetJob(promptId);
                    if (string.Equals(status.Status, "completed", StringComparison.OrdinalIgnoreCase) && status.ImageUrls?.Count > 0)
                    {
                        return status.ImageUrls[0];
                    }

                    if (string.Equals(status.Status, "failed", StringComparison.OrdinalIgnoreCase))
                        break;

                    if (DateTimeOffset.UtcNow > timeoutAt)
                        break;

                    await Task.Delay(2000);
                }
                while (true);
            }
            catch
            {
                // swallow — fallback is best-effort
            }

            return null;
        }

        private Task<string> CreateAuthAsync(string jobId)
        {
            return Task.FromResult(Convert.ToBase64String(Encoding.UTF8.GetBytes(jobId)));
        }

        private async Task RecordStatsAsync(string jobId, DateTime startTime)
        {
            var payload = new
            {
                job_id = jobId,
                total_time_ms = (DateTime.UtcNow - startTime).TotalMilliseconds,
                timestamp = DateTime.UtcNow
            };

            await _http.PostAsync(
                $"{_statsApiUrl}/record-completion",
                new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json"));
        }

        private async Task<string> SaveImageResultAsync(string imageDataOrUrl, string outputDirectory, string baseName)
        {
            if (string.IsNullOrWhiteSpace(imageDataOrUrl))
                throw new Exception("No image data returned from generator.");

            Directory.CreateDirectory(outputDirectory);

            string outPath;

            // If it's a URL, download it
            if (Uri.TryCreate(imageDataOrUrl, UriKind.Absolute, out var uri) && (uri.Scheme == "http" || uri.Scheme == "https"))
            {
                var bytes = await _http.GetByteArrayAsync(imageDataOrUrl);
                // Final output should always be JPG named page_#.jpg
                outPath = Path.Combine(outputDirectory, baseName + ".jpg");
                await ProcessImageBytesAndSaveAsync(bytes, outPath);
            }
            // If it's a data URI: data:[mime];base64,DATA
            else if (imageDataOrUrl.StartsWith("data:", StringComparison.OrdinalIgnoreCase))
            {
                var comma = imageDataOrUrl.IndexOf(',');
                var meta = imageDataOrUrl.Substring(5, comma - 5); // after 'data:'
                var base64 = imageDataOrUrl.Substring(comma + 1);
                var bytes = Convert.FromBase64String(base64);
                // Final output should always be JPG named page_#.jpg
                outPath = Path.Combine(outputDirectory, baseName + ".jpg");
                await ProcessImageBytesAndSaveAsync(bytes, outPath);
            }
            else
            {
                // Otherwise assume raw base64 JPEG
                try
                {
                    var bytes = Convert.FromBase64String(imageDataOrUrl);
                    outPath = Path.Combine(outputDirectory, baseName + ".jpg");
                    await ProcessImageBytesAndSaveAsync(bytes, outPath);
                }
                catch (FormatException)
                {
                    // Not base64; write the string as text for diagnostics
                    outPath = Path.Combine(outputDirectory, baseName + ".txt");
                    await File.WriteAllTextAsync(outPath, imageDataOrUrl);
                    return outPath; // not an image — skip greyscale
                }
            }

            return outPath;
        }

        private async Task ProcessImageBytesAndSaveAsync(byte[] bytes, string outPath)
        {
            var dir = Path.GetDirectoryName(outPath);
            if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);

            var baseName = Path.GetFileNameWithoutExtension(outPath);
            var ext = Path.GetExtension(outPath);
            if (string.IsNullOrEmpty(ext)) ext = ".jpg";

            // Save raw original bytes first (no processing)
            var rawPath = Path.Combine(dir, baseName + "_00_raw" + ext);
            await File.WriteAllBytesAsync(rawPath, bytes);

            // Use BookFinisher for all finishing stages
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

            var finalPath = await _finisher.FinishImageAsync(rawPath, dir, pageNum, options);

            // Overwrite the caller's expected path with the BookFinisher's output
            if (finalPath != outPath)
            {
                if (File.Exists(outPath)) File.Delete(outPath);
                File.Copy(finalPath, outPath);
            }
        }

        private static int? ExtractPageNumber(string baseName)
        {
            var match = System.Text.RegularExpressions.Regex.Match(baseName, @"page_(\d+)", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
            if (match.Success && int.TryParse(match.Groups[1].Value, out var pn))
                return pn;
            return null;
        }

        private string RatioToString(Ratio r)
        {
            return r switch
            {
                Ratio.ratio_4_3 => "4:3",
                Ratio.ratio_3_4 => "3:4",
                Ratio.ratio_1_1 => "1:1",
                Ratio.ratio_16_9 => "16:9",
                Ratio.ratio_9_16 => "9:16",
                _ => "3:4",
            };
        }
    }

       

    public class SignerResponse
    {
        public long ts { get; set; }
        public string sig { get; set; }
    }

    public class GeneratorResponse
    {
        public string job_id { get; set; }
        public string image_data_url { get; set; }
    }

    public class WsMessage
    {
        public string type { get; set; }
        public string message { get; set; }
        public string image_data { get; set; }
    }
}