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
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using PrintifyGenerator.ColoringBookGenerator.Utilities;

namespace PrintifyGenerator.ColoringBookGenerator.Services
{
    public class FreeGenGenerator : IImageGenerator
    {
        private readonly HttpClient _http;

        private readonly string _signerUrl = "https://prompt-signer.freegen.app";
        private readonly string _generatorUrl = "https://image-generator.freegen.app";
        private readonly string _websocketUrl = "wss://websocket-bridge.freegen.app/ws";
        private readonly string _statsApiUrl = "https://stats.freegen.app";

        public FreeGenGenerator(HttpClient http)
        {
            _http = http;
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
        public async Task<string> GenerateFrontCoverAsync(string outputDirectory, string title, string theme, string styleAddon)
        {
            var prompt = BuildFrontCoverPrompt(title, theme, styleAddon);
            PromptRecorder.Record("FreeGenGenerator", "front_cover_prompt", prompt);
            var result = await GenerateInternalAsync(prompt, Ratio.ratio_3_4);
            return await SaveImageResultAsync(result, outputDirectory, "front_cover");
        }

        /// <summary>Back (left-side) cover — full color, portrait 3:4.</summary>
        public async Task<string> GenerateBackCoverAsync(string outputDirectory, string theme, string styleAddon)
        {
            var prompt = BuildBackCoverPrompt(theme, styleAddon);
            PromptRecorder.Record("FreeGenGenerator", "back_cover_prompt", prompt);
            var result = await GenerateInternalAsync(prompt, Ratio.ratio_3_4);
            return await SaveImageResultAsync(result, outputDirectory, "back_cover");
        }

        /// <summary>Interior coloring page — black and white line art, portrait 3:4.</summary>
        public async Task<string> GeneratePageAsync(string outputDirectory, int pageNumber, string theme, string styleAddon)
        {
            var ratio = Ratio.ratio_16_9; // Standard portrait ratio for coloring book pages
            if (pageNumber == 1 || pageNumber == 24) ratio = Ratio.ratio_3_4; // Could customize first and last page if desired
            var prompt = BuildPagePrompt(pageNumber, theme, styleAddon);
            PromptRecorder.Record("FreeGenGenerator", $"page_{pageNumber:D2}_prompt", prompt);
            var result = await GenerateInternalAsync(prompt, ratio);
            return await SaveImageResultAsync(result, outputDirectory, $"page_{pageNumber:D2}");
        }

        // ── Prompt builders ────────────────────────────────────────────────────
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

        private async Task<string> GenerateInternalAsync(string prompt, Ratio ratioId)
        {
            if (string.IsNullOrWhiteSpace(prompt))
                prompt = ".";

            if (prompt.Length > 2000)
            {
                prompt = prompt.Substring(0, 2000);
                Console.WriteLine("Warning: Prompt exceeded 2000 characters and was truncated. This may affect image quality.");
            }

            // record the final prompt that will be sent to the signer/generator (post-truncation)
            try
            {
                PromptRecorder.Record("FreeGenGenerator", "final_prompt", prompt);
            }
            catch
            {
                // best-effort only
            }

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

        private static async Task ProcessImageBytesAndSaveAsync(byte[] bytes, string outPath)
        {
            var dir = Path.GetDirectoryName(outPath);
            if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);
            using var image = Image.Load<Rgba32>(bytes);
            var origW = image.Width;
            var origH = image.Height;

            var baseName = Path.GetFileNameWithoutExtension(outPath);
            var ext = Path.GetExtension(outPath);
            if (string.IsNullOrEmpty(ext)) ext = ".png";

            // Save original
            var origPath = Path.Combine(dir, baseName + "_orig" + ext);
            await image.SaveAsync(origPath);

            // Convert to grayscale
            image.Mutate(ctx => ctx.Grayscale());
            var grayPath = Path.Combine(dir, baseName + "_01_grayscale" + ext);
            await image.SaveAsync(grayPath);

            // Supersample, blur, then downsample to smooth edges (antialiasing)
            const int supersample = 8;
            if (supersample > 1 && origW > 1 && origH > 1)
            {
                image.Mutate(ctx => ctx.Resize(origW * supersample, origH * supersample, KnownResamplers.Lanczos3));
                image.Mutate(ctx => ctx.GaussianBlur(1f));
                image.Mutate(ctx => ctx.Resize(origW, origH, KnownResamplers.Lanczos3));
            }
            var aaPath = Path.Combine(dir, baseName + "_02_antialiased" + ext);
            await image.SaveAsync(aaPath);

            // Final output (same name caller expects)
            await image.SaveAsync(outPath);
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