using System;
using System.Collections.Concurrent;
using System.IO;
using System.Net;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

public class ComfyUiClient
{
    public static string BaseUrl { get; private set; }
    private readonly string _baseUrl;
    public readonly string _wsUrl;
    private readonly string _clientId;

    private ClientWebSocket _socket;
    private readonly WebSocketEventEmitter _emitter;

    // 🧠 Job tracking
    private readonly ConcurrentDictionary<string, JobStatus> _jobs
        = new ConcurrentDictionary<string, JobStatus>();


    public ComfyUiClient(string baseUrl, WebSocketEventEmitter emitter)
    {
        _baseUrl = baseUrl.TrimEnd('/');
        _clientId = Guid.NewGuid().ToString();
        _wsUrl = $"{_baseUrl.Replace("http", "ws")}/ws?clientId={_clientId}";
        BaseUrl = _baseUrl;

        _emitter = emitter;

        // 🔗 Hook internal handlers to emitter
        RegisterInternalHandlers();
    }

    public WebSocketEventEmitter Events => _emitter;

    // -----------------------------
    // 🔗 Internal Event Wiring
    // -----------------------------
    private void RegisterInternalHandlers()
    {
        _emitter.On("execution_start", data =>
        {
            var id = data.GetProperty("prompt_id").GetString();
            UpdateJob(id, j => j.Status = "running");
        });

        _emitter.On("executing", data =>
        {
            var id = data.GetProperty("prompt_id").GetString();
            var node = data.GetProperty("node").GetString();

            UpdateJob(id, j =>
            {
                j.CurrentNode = node;
            });
        });

        _emitter.On("executed", data =>
        {
            //output.images[].{filename, subfolder, type}
            List<string> urls = new List<string>();
            foreach (var node in data.GetProperty("outputs").EnumerateObject())
            {
                if (!node.Value.TryGetProperty("images", out var images))
                    continue;

                foreach (var img in images.EnumerateArray())
                {
                    var filename = img.GetProperty("filename").GetString();
                    var subfolder = img.GetProperty("subfolder").GetString();
                    var type = img.GetProperty("type").GetString();

                    string url =
                        $"{_baseUrl}/view?filename={filename}" +
                        $"&subfolder={subfolder}" +
                        $"&type={type}";

                    urls.Add(url);
                }
            }
            var id = data.GetProperty("prompt_id").GetString();

            UpdateJob(id, j =>
            {
                j.ImageUrls = urls;
            });
            UpdateJob(id, j =>
            {
                j.Status = "completed";
            });
        });

        _emitter.On("progress", data =>
        {
            var id = data.GetProperty("prompt_id").GetString();
            var value = data.GetProperty("value").GetDouble();
            var max = data.GetProperty("max").GetDouble();

            UpdateJob(id, j => j.Progress = (value / max)*100);
        });

        _emitter.On("execution_success", async data =>
        {
            var id = data.GetProperty("prompt_id").GetString();

            UpdateJob(id, j => j.Status = "completed");

            UpdateJob(id, j =>
            {
                j.Progress = 1.0;
                j.Status = "completed";
            });
            // // 🔥 Fetch outputs after completion
            // await PopulateJobOutputs(id);
        });

        _emitter.On("execution_error", data =>
        {
            var id = data.GetProperty("prompt_id").GetString();
            UpdateJob(id, j => j.Status = "failed");
        });
    }


    private async Task PopulateJobOutputs(string promptId)
    {
        try
        {
            string json = GetHistory(promptId);

            using var doc = JsonDocument.Parse(json);

            if (!doc.RootElement.TryGetProperty(promptId, out var jobData))
                return;

            if (!jobData.TryGetProperty("outputs", out var outputs))
                return;

            var urls = new List<string>();

            foreach (var node in outputs.EnumerateObject())
            {
                if (!node.Value.TryGetProperty("images", out var images))
                    continue;

                foreach (var img in images.EnumerateArray())
                {
                    var filename = img.GetProperty("filename").GetString();
                    var subfolder = img.GetProperty("subfolder").GetString();
                    var type = img.GetProperty("type").GetString();

                    string url =
                        $"{_baseUrl}/view?filename={Uri.EscapeDataString(filename)}" +
                        $"&subfolder={Uri.EscapeDataString(subfolder)}" +
                        $"&type={Uri.EscapeDataString(type)}";

                    urls.Add(url);
                }
            }

            UpdateJob(promptId, j => j.ImageUrls = urls);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to fetch outputs: {ex.Message}");
        }
    }

    // -----------------------------
    // 🚀 Send Prompt
    // -----------------------------
    public string QueuePrompt(object workflow)
    {
        var request = (HttpWebRequest)WebRequest.Create($"{_baseUrl}/prompt");
        request.Method = "POST";
        request.ContentType = "application/json";

        string json = JsonSerializer.Serialize(new
        {
            prompt = workflow,
            client_id = _clientId
        });

        using (var stream = new StreamWriter(request.GetRequestStream()))
            stream.Write(json);

        using var response = (HttpWebResponse)request.GetResponse();
        using var reader = new StreamReader(response.GetResponseStream());

        var responseText = reader.ReadToEnd();

        using var doc = JsonDocument.Parse(responseText);
        var promptId = doc.RootElement.GetProperty("prompt_id").GetString();

        _jobs[promptId] = new JobStatus
        {
            PromptId = promptId,
            Status = "queued"
        };

        return promptId;
    }

    // -----------------------------
    // 📊 Query Jobs
    // -----------------------------
    public JobStatus GetJob(string promptId)
    {
        _jobs.TryGetValue(promptId, out var job);
        return job;
    }

    public ConcurrentDictionary<string, JobStatus> GetAllJobs()
    {
        return _jobs;
    }

    // -----------------------------
    // 📦 Get Result
    // -----------------------------
    public string GetHistory(string promptId)
    {
        var request = (HttpWebRequest)WebRequest.Create($"{_baseUrl}/history/{promptId}");
        request.Method = "GET";

        using var response = (HttpWebResponse)request.GetResponse();
        using var reader = new StreamReader(response.GetResponseStream());

        return reader.ReadToEnd();
    }

    // -----------------------------
    // 🔌 Start WebSocket
    // -----------------------------
    public async Task StartListener()
    {
        _socket = new ClientWebSocket();
        await _socket.ConnectAsync(new Uri(_wsUrl), CancellationToken.None);
        Console.WriteLine($"[WS] Connected to {_wsUrl}");

        var featureFlags = "{\"type\":\"feature_flags\",\"data\":{\"supports_preview_metadata\":true,\"supports_manager_v4_ui\":true,\"supports_progress_text_metadata\":true}}";
        var flagBytes = Encoding.UTF8.GetBytes(featureFlags);
        await _socket.SendAsync(new ArraySegment<byte>(flagBytes), WebSocketMessageType.Text, true, CancellationToken.None);
        Console.WriteLine($"[WS] Sent: {featureFlags}");

        _ = Task.Run(ListenLoop);
    }

    private async Task ListenLoop()
    {
        var buffer = new byte[8192];

        while (_socket.State == WebSocketState.Open)
        {
            using var ms = new MemoryStream();
            WebSocketReceiveResult result;

            do
            {
                result = await _socket.ReceiveAsync(
                    new ArraySegment<byte>(buffer),
                    CancellationToken.None
                );

                if (result.MessageType == WebSocketMessageType.Close)
                    return;

                // Only accumulate text frames; binary preview frames are discarded
                if (result.MessageType == WebSocketMessageType.Text)
                    ms.Write(buffer, 0, result.Count);

            } while (!result.EndOfMessage);

            if (result.MessageType != WebSocketMessageType.Text)
            {
                Console.WriteLine($"[WS] Binary frame received ({ms.Length} bytes, skipped)");
                continue;
            }

            var json = Encoding.UTF8.GetString(ms.ToArray());
            // Console.WriteLine($"[WS] {json}");
            // Console.WriteLine($"");
            Dispatch(json);
        }
    }

    // -----------------------------
    // 📡 Dispatch → EventEmitter
    // -----------------------------
    private void Dispatch(string json)
    {
        try
        {
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            if (!root.TryGetProperty("type", out var typeProp))
                return;

            if (!root.TryGetProperty("data", out var data))
                return;

            string type = typeProp.GetString();

            // 🔥 Emit event
            _emitter.Emit(type, data);
        }
        catch
        {
            // ignore bad JSON
        }
    }

    private void UpdateJob(string id, Action<JobStatus> update)
    {
        if (_jobs.TryGetValue(id, out var job))
        {
            update(job);
        }
    }
}

// -----------------------------
// 📦 Job Model
// -----------------------------
public class JobStatus
{
    public string PromptId { get; set; }
    public string Status { get; set; }
    public string CurrentNode { get; set; }
    public double Progress { get; set; }

    // 🆕 Outputs
    public List<string> ImageUrls { get; set; } = new List<string>();

    public async Task<string> DownloadAllImagesAsync(string saveDirectory)
    {
        Directory.CreateDirectory(saveDirectory);

        var stamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
        var baseUrl = ComfyUiClient.BaseUrl;

        // Fetch image URLs from history API
        var historyRequest = (HttpWebRequest)WebRequest.Create($"{baseUrl}/history/{PromptId}");
        historyRequest.Method = "GET";
        using var historyResponse = (HttpWebResponse)await historyRequest.GetResponseAsync();
        using var historyReader = new StreamReader(historyResponse.GetResponseStream());
        var historyJson = await historyReader.ReadToEndAsync();

        using var doc = JsonDocument.Parse(historyJson);
        if (!doc.RootElement.TryGetProperty(PromptId, out var jobData) ||
            !jobData.TryGetProperty("outputs", out var outputs))
        {
            Console.WriteLine($"No outputs found in history for prompt {PromptId}.");
            return "";
        }

        var urls = new List<string>();
        foreach (var node in outputs.EnumerateObject())
        {
            if (!node.Value.TryGetProperty("images", out var images))
                continue;

            foreach (var img in images.EnumerateArray())
            {
                var filename = img.GetProperty("filename").GetString();
                var subfolder = img.GetProperty("subfolder").GetString();
                var type = img.GetProperty("type").GetString();

                urls.Add(
                    $"{baseUrl}/view?filename={Uri.EscapeDataString(filename)}" +
                    $"&subfolder={Uri.EscapeDataString(subfolder)}" +
                    $"&type={Uri.EscapeDataString(type)}"
                );
            }
        }

        Console.WriteLine($"Downloading {urls.Count} images for prompt {PromptId}...");
        foreach (var url in urls)
        {
            var uri = new Uri(url);
            var query = uri.Query.TrimStart('?')
                .Split('&')
                .Select(p => p.Split('=', 2))
                .Where(p => p.Length == 2)
                .ToDictionary(p => Uri.UnescapeDataString(p[0]), p => Uri.UnescapeDataString(p[1]));

            var originalFilename = query.TryGetValue("filename", out var fn) ? fn : Path.GetFileName(uri.LocalPath);
            var savePath = Path.Combine(saveDirectory, $"{PromptId}.png");
            Console.WriteLine($"Downloading {url} to {savePath}...");
            return await DownloadImageAsync(url, savePath);
        }
        return "";
    }

    private async Task<string> DownloadImageAsync(string url, string savePath)
    {
        var request = (HttpWebRequest)WebRequest.Create(url);
        request.Method = "GET";

        using var response = (HttpWebResponse)await request.GetResponseAsync();
        using var stream = response.GetResponseStream();
        using var file = File.Create(savePath);

        await stream.CopyToAsync(file);
        return savePath;
    }
}