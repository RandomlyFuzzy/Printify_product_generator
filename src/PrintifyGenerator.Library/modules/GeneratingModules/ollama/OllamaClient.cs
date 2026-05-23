using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using System.IO;
using PrintifyGenerator.Library.Ollama;

public class OllamaClient : IDisposable
{
    private readonly HttpClient _http;
    private readonly string _baseUrl;

    // 🔒 GLOBAL LOCK (only 1 request at a time)
    private SemaphoreSlim _gate
    {
        get
        {
            if (!_modelLocks.TryGetValue(_baseUrl, out var sem))
            {
                sem = new SemaphoreSlim(1, 2);
                _modelLocks[_baseUrl] = sem;
            }
            return sem;
        }
    }
    static Dictionary<string,SemaphoreSlim> _modelLocks = new(StringComparer.OrdinalIgnoreCase);

    public OllamaClient(string baseUrl = "http://localhost:11434")
    {
        _baseUrl = baseUrl.TrimEnd('/');
        _http = new HttpClient()
        {
            Timeout = TimeSpan.FromMinutes(10)
        };
        if(!_modelLocks.ContainsKey(_baseUrl))
        {
            _modelLocks[_baseUrl] = new SemaphoreSlim(1,1);
        }
    }

    /// <summary>For testing — inject a custom HttpClient.</summary>
    public OllamaClient(string baseUrl, HttpMessageHandler handler)
    {
        _baseUrl = baseUrl.TrimEnd('/');
        _http = new HttpClient(handler)
        {
            Timeout = TimeSpan.FromMinutes(10)
        };
        if(!_modelLocks.ContainsKey(_baseUrl))
        {
            _modelLocks[_baseUrl] = new SemaphoreSlim(1,1);
        }
    }


    // =========================
    // 🔹 LOCK HELPER
    // =========================
    private async Task<T> WithLock<T>(Func<Task<T>> action)
    {
        await _gate.WaitAsync();
        try
        {
            return await action();
        }
        finally
        {
            _gate.Release();
        }
    }

    private async Task WithLock(Func<Task> action)
    {
        await _gate.WaitAsync();
        try
        {
            await action();
        }
        finally
        {
            _gate.Release();
        }
    }

    // =========================
    // 🔹 1. Health check
    // =========================
    public Task<string> CheckStatusAsync()
        => WithLock(async () =>
        {
            var response = await _http.GetAsync(BuildUrl("/api/tags"));
            response.EnsureSuccessStatusCode();

            var body = await response.Content.ReadAsStringAsync();
            ParseInstalledModelNames(body);
            return body;
        });

    public Task<HashSet<string>> GetInstalledModelNamesAsync(CancellationToken ct = default)
        => WithLock(async () =>
        {
            var response = await _http.GetAsync(BuildUrl("/api/tags"), ct);
            response.EnsureSuccessStatusCode();

            var body = await response.Content.ReadAsStringAsync(ct);
            return ParseInstalledModelNames(body);
        });

    public Task<string> ListModelsAsync()
        => WithLock(() => _http.GetStringAsync(BuildUrl("/api/tags")));

    // =========================
    // 🔹 2. Pull model
    // =========================
    public Task<string> PullModelAsync(string model)
        => WithLock(() =>
        {
            var body = JsonSerializer.Serialize(new { name = model });
            return PostAsync("/api/pull", body);
        });

    // =========================
    // 🔹 3. Generate
    // =========================
    public Task<string> GenerateAsync(string model, string prompt)
        => WithLock(() =>
        {
            var body = JsonSerializer.Serialize(new
            {
                model,
                prompt,
                stream = false,
                options = new
                {
                    temperature = 0.7,
                    top_p = 0.9
                }
            });

            return PostAsync("/api/generate", body);
        });

    // =========================
    // 🔹 4. Chat
    // =========================
    public Task<string> ChatAsync(string model, string message)
        => WithLock(() =>
        {
            var body = JsonSerializer.Serialize(new
            {
                model,
                stream = false,
                messages = new[]
                {
                    new { role = "user", content = message }
                }
            });

            return PostAsync("/api/chat", body);
        });

    // =========================
    // 🔹 4b. Chat with Tools (multi-turn)
    // =========================
    /// <param name="executeTool">Called for each tool_call. Receives ToolCall, returns string result.</param>
    public async Task<ChatResult> ChatWithToolsAsync(
        string model,
        List<ChatMessage> messages,
        List<ToolDefinition> tools,
        Func<ToolCall, Task<string>> executeTool,
        int maxTurns = 10)
    {
        var allMessages = new List<Dictionary<string, object>>();
        foreach (var msg in messages)
            allMessages.Add(msg.ToDictionary());

        var toolDefs = tools.Select(t => t.ToJson()).ToList();

        for (int turn = 0; turn < maxTurns; turn++)
        {
            var requestBody = new Dictionary<string, object>
            {
                ["model"] = model,
                ["stream"] = false,
                ["messages"] = allMessages,
                ["tools"] = toolDefs
            };

            var json = JsonSerializer.Serialize(requestBody);
            var responseJson = await WithLock(() => PostAsync("/api/chat", json));
            using var doc = JsonDocument.Parse(responseJson);
            var root = doc.RootElement;

            var msgElement = root.GetProperty("message");
            var role = msgElement.GetProperty("role").GetString()!;
            var content = msgElement.TryGetProperty("content", out var c) ? c.GetString() : null;

            List<ToolCall>? toolCalls = null;
            if (msgElement.TryGetProperty("tool_calls", out var tcElement))
                toolCalls = ToolCall.ParseList(tcElement);

            var assistantMsg = new Dictionary<string, object> { ["role"] = role };
            if (content != null)
                assistantMsg["content"] = content;
            if (toolCalls is { Count: > 0 })
            {
                var callsList = new List<Dictionary<string, object>>();
                foreach (var tc in toolCalls)
                {
                    var argsDict = new Dictionary<string, object>();
                    foreach (var kvp in tc.Arguments)
                        argsDict[kvp.Key] = JsonSerializer.Deserialize<object>(kvp.Value.GetRawText()) ?? "";

                    var argsJson = JsonSerializer.Serialize(argsDict);
                    callsList.Add(new Dictionary<string, object>
                    {
                        ["type"] = "function",
                        ["function"] = new Dictionary<string, object>
                        {
                            ["name"] = tc.Name,
                            ["arguments"] = argsJson
                        }
                    });
                }
                assistantMsg["tool_calls"] = callsList;
            }
            allMessages.Add(assistantMsg);

            if (toolCalls == null || toolCalls.Count == 0)
            {
                return new ChatResult(content ?? "", allMessages);
            }

            foreach (var toolCall in toolCalls)
            {
                string result;
                try
                {
                    result = await executeTool(toolCall);
                }
                catch (Exception ex)
                {
                    result = $"Error: {ex.Message}";
                }

                allMessages.Add(new Dictionary<string, object>
                {
                    ["role"] = "tool",
                    ["tool_call_id"] = $"{toolCall.Name}_{turn}",
                    ["content"] = result
                });
            }
        }

        return new ChatResult("Max turns reached", allMessages);
    }

    public class ChatResult
    {
        public string Content { get; }
        public List<Dictionary<string, object>> MessageHistory { get; }

        public ChatResult(string content, List<Dictionary<string, object>> messageHistory)
        {
            Content = content;
            MessageHistory = messageHistory;
        }
    }

    // =========================
    // 🔹 5. Show model
    // =========================
    public Task<string> ShowModelAsync(string model)
        => WithLock(() =>
        {
            var body = JsonSerializer.Serialize(new { name = model });
            return PostAsync("/api/show", body);
        });

    // =========================
    // 🔹 6. Delete model
    // =========================
    public Task<string> DeleteModelAsync(string model)
        => WithLock(async () =>
        {
            var body = JsonSerializer.Serialize(new { name = model });

            var request = new HttpRequestMessage(HttpMethod.Delete, BuildUrl("/api/delete"))
            {
                Content = new StringContent(body, Encoding.UTF8, "application/json")
            };

            var response = await _http.SendAsync(request);
            return await response.Content.ReadAsStringAsync();
        });

    // =========================
    // 🔹 7. Embeddings
    // =========================
    public Task<string> EmbeddingsAsync(string model, string prompt)
        => WithLock(() =>
        {
            var body = JsonSerializer.Serialize(new
            {
                model,
                prompt
            });

            return PostAsync("/api/embeddings", body);
        });

    // =========================
    // 🔹 8. Stop model
    // =========================
    public Task<string> StopModelAsync(string model)
        => WithLock(() =>
        {
            var body = JsonSerializer.Serialize(new
            {
                model,
                keep_alive = "0"
            });

            return PostAsync("/api/generate", body);
        });

    // =========================
    // 🔹 9. Generate with images
    // =========================
    public Task<string> GenerateWithImageAsync(string model, string prompt, params string[] imagePaths)
        => WithLock(async () =>
        {
            var base64Images = new List<string>();

            foreach (var path in imagePaths)
            {
                var bytes = await File.ReadAllBytesAsync(path);
                base64Images.Add(Convert.ToBase64String(bytes));
            }

            var body = JsonSerializer.Serialize(new
            {
                model,
                prompt,
                images = base64Images,
                stream = false
            });

            return await PostAsync("/api/generate", body);
        });

    // =========================
    // 🔥 STREAMING (LOCKED)
    // =========================

    public async IAsyncEnumerable<string> GenerateStreamAsync(
        string model,
        string prompt,
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken ct = default)
    {
        await _gate.WaitAsync(ct);
        try
        {
            var body = JsonSerializer.Serialize(new
            {
                model,
                prompt,
                stream = true
            });

            var request = new HttpRequestMessage(HttpMethod.Post, BuildUrl("/api/generate"))
            {
                Content = new StringContent(body, Encoding.UTF8, "application/json")
            };

            using var response = await _http.SendAsync(
                request,
                HttpCompletionOption.ResponseHeadersRead,
                ct);

            response.EnsureSuccessStatusCode();

            await using var stream = await response.Content.ReadAsStreamAsync(ct);
            using var reader = new StreamReader(stream);

            while (!ct.IsCancellationRequested)
            {
                var line = await reader.ReadLineAsync();
                if (line == null) yield break;
                if (string.IsNullOrWhiteSpace(line)) continue;

                using var doc = JsonDocument.Parse(line);

                if (doc.RootElement.TryGetProperty("response", out var token))
                {
                    var text = token.GetString();
                    if (!string.IsNullOrEmpty(text))
                        yield return text;
                }

                if (doc.RootElement.TryGetProperty("done", out var done) &&
                    done.GetBoolean())
                    yield break;
            }
        }
        finally
        {
            _gate.Release();
        }
    }
    public async IAsyncEnumerable<string> GenerateWithImageStreamAsync(
        string model,
        string prompt,
        string imagePath,
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        await _gate.WaitAsync(cancellationToken);
        try
        {
            var imageBytes = await File.ReadAllBytesAsync(imagePath, cancellationToken);
            var base64Image = Convert.ToBase64String(imageBytes);

            var requestBody = JsonSerializer.Serialize(new
            {
                model,
                prompt,
                images = new[] { base64Image },
                stream = true
            });

            var request = new HttpRequestMessage(HttpMethod.Post, BuildUrl("/api/generate"))
            {
                Content = new StringContent(requestBody, Encoding.UTF8, "application/json")
            };

            using var response = await _http.SendAsync(
                request,
                HttpCompletionOption.ResponseHeadersRead,
                cancellationToken);

            response.EnsureSuccessStatusCode();

            await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
            using var reader = new StreamReader(stream);

            while (!cancellationToken.IsCancellationRequested)
            {
                var line = await reader.ReadLineAsync();
                if (line is null)
                    yield break;

                if (string.IsNullOrWhiteSpace(line))
                    continue;

                using var doc = JsonDocument.Parse(line);

                if (doc.RootElement.TryGetProperty("response", out var token))
                {
                    var text = token.GetString();
                    if (!string.IsNullOrEmpty(text))
                        yield return text;
                }

                if (doc.RootElement.TryGetProperty("done", out var done) &&
                    done.GetBoolean())
                {
                    yield break;
                }
            }
        }
        finally
        {
            _gate.Release();
        }
    }
    public async IAsyncEnumerable<string> GenerateWithImagesStreamAsync(
        string model,
        string prompt,
        string[] imagePaths,
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        await _gate.WaitAsync(cancellationToken);
        try
        {
            var base64Images = new List<string>();

            foreach (var imagePath in imagePaths)
            {
                var imageBytes = await File.ReadAllBytesAsync(imagePath, cancellationToken);
                base64Images.Add(Convert.ToBase64String(imageBytes));
            }

            var requestBody = JsonSerializer.Serialize(new
            {
                model,
                prompt,
                images = base64Images,
                stream = true
            });

            var request = new HttpRequestMessage(HttpMethod.Post, BuildUrl("/api/generate"))
            {
                Content = new StringContent(requestBody, Encoding.UTF8, "application/json")
            };

            using var response = await _http.SendAsync(
                request,
                HttpCompletionOption.ResponseHeadersRead,
                cancellationToken);

            response.EnsureSuccessStatusCode();

            await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
            using var reader = new StreamReader(stream);

            while (!cancellationToken.IsCancellationRequested)
            {
                var line = await reader.ReadLineAsync();
                if (line is null)
                    yield break;

                if (string.IsNullOrWhiteSpace(line))
                    continue;

                using var doc = JsonDocument.Parse(line);

                if (doc.RootElement.TryGetProperty("response", out var token))
                {
                    var text = token.GetString();
                    if (!string.IsNullOrEmpty(text))
                        yield return text;
                }

                if (doc.RootElement.TryGetProperty("done", out var done) &&
                    done.GetBoolean())
                {
                    yield break;
                }
            }
        }
        finally
        {
            _gate.Release();
        }
    }

    public Task<float[]> GetEmbeddingVectorAsync(string model, string prompt)
    => WithLock(async () =>
    {
        var body = JsonSerializer.Serialize(new
        {
            model,
            prompt
        });

        var json = await PostAsync("/api/embeddings", body);

        using var doc = JsonDocument.Parse(json);

        if (!doc.RootElement.TryGetProperty("embedding", out var emb))
            throw new InvalidOperationException("No embedding returned");

        var vector = new float[emb.GetArrayLength()];
        for (int i = 0; i < vector.Length; i++)
            vector[i] = emb[i].GetSingle();

        return vector;
    });

    public void Dispose()
    {
        _http.Dispose();
    }

    // =========================
    // 🔧 POST helper (NOT locked directly)
    // =========================
    private async Task<string> PostAsync(string endpoint, string json)
    {
        var content = new StringContent(json, Encoding.UTF8, "application/json");
        var response = await _http.PostAsync(BuildUrl(endpoint), content);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadAsStringAsync();
    }

    // =========================
    // 🔧 helpers unchanged
    // =========================
    private static HashSet<string> ParseInstalledModelNames(string body)
    {
        using var document = JsonDocument.Parse(body);

        if (!document.RootElement.TryGetProperty("models", out var models))
            throw new InvalidOperationException("Invalid response");

        var set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var model in models.EnumerateArray())
        {
            if (model.TryGetProperty("name", out var name))
            {
                var value = name.GetString();
                if (!string.IsNullOrWhiteSpace(value))
                    set.Add(value);
            }
        }

        return set;
    }



    private string BuildUrl(string endpoint)
        => endpoint.StartsWith('/')
            ? $"{_baseUrl}{endpoint}"
            : $"{_baseUrl}/{endpoint}";
}