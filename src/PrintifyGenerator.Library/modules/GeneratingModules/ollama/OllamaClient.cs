using System;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.IO;

public class OllamaClient
{
    private readonly HttpClient _http;
    private readonly string _baseUrl;

    public OllamaClient(string baseUrl = "http://localhost:11434")
    {
        _baseUrl = baseUrl;
        _http = new HttpClient()
        {
            Timeout = TimeSpan.FromMinutes(10) // Set a longer timeout for potentially long-running requests
        };
    }

    // 🔹 1. Health check
    public async Task<string> CheckStatusAsync()
    {
        return await _http.GetStringAsync(_baseUrl);
    }

    // 🔹 2. List models
    public async Task<string> ListModelsAsync()
    {
        return await _http.GetStringAsync($"{_baseUrl}/api/tags");
    }

    // 🔹 3. Pull model
    public async Task<string> PullModelAsync(string model)
    {
        var body = JsonSerializer.Serialize(new { name = model });
        return await PostAsync("/api/pull", body);
    }

    // 🔹 4. Generate text
    public async Task<string> GenerateAsync(string model, string prompt)
    {
        var body = JsonSerializer.Serialize(new
        {
            model,
            prompt,
            stream = false,
            options = new
            {
                temperature = 0.7,
                top_p = 0.9,
                num_predict = 200
            }
        });

        return await PostAsync("/api/generate", body);
    }

    // 🔹 5. Chat
    public async Task<string> ChatAsync(string model, string message)
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

        return await PostAsync("/api/chat", body);
    }

    // 🔹 6. Model info
    public async Task<string> ShowModelAsync(string model)
    {
        var body = JsonSerializer.Serialize(new { name = model });
        return await PostAsync("/api/show", body);
    }

    // 🔹 7. Delete model
    public async Task<string> DeleteModelAsync(string model)
    {
        var body = JsonSerializer.Serialize(new { name = model });

        var request = new HttpRequestMessage(HttpMethod.Delete, $"{_baseUrl}/api/delete")
        {
            Content = new StringContent(body, Encoding.UTF8, "application/json")
        };

        var response = await _http.SendAsync(request);
        return await response.Content.ReadAsStringAsync();
    }

    // 🔹 8. Embeddings
    public async Task<string> EmbeddingsAsync(string model, string prompt)
    {
        var body = JsonSerializer.Serialize(new
        {
            model,
            prompt
        });

        return await PostAsync("/api/embeddings", body);
    }

    // 🖼️ 9. Generate with image (multimodal)
    public async Task<string> GenerateWithImageAsync(string model, string prompt, string imagePath)
    {
        var base64Image = Convert.ToBase64String(await File.ReadAllBytesAsync(imagePath));

        var body = JsonSerializer.Serialize(new
        {
            model,
            prompt,
            images = new[] { base64Image },
            stream = false
        });

        return await PostAsync("/api/generate", body);
    }

    //ollama stop model from being active
    public async Task<string> StopModelAsync(string model)
    {
        /*
        Endpoint: http://localhost:11434/api/generateMethod: POSTBody: {"model": "model_name", "keep_alive": "0"}
        */
        var body = JsonSerializer.Serialize(new
        {
            model,
            keep_alive = "0"
        });
        return await PostAsync("/api/generate", body);
       
    }


    public async IAsyncEnumerable<string> GenerateWithImageStreamAsync(
    string model,
    string prompt,
    string imagePath,
    [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
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

        var request = new HttpRequestMessage(HttpMethod.Post, $"{_baseUrl}/api/generate")
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

        while (!reader.EndOfStream && !cancellationToken.IsCancellationRequested)
        {
            var line = await reader.ReadLineAsync();
            if (string.IsNullOrWhiteSpace(line))
                continue;

            using var doc = JsonDocument.Parse(line);

            if (doc.RootElement.TryGetProperty("response", out var token))
                yield return token.GetString();

            if (doc.RootElement.TryGetProperty("done", out var done) &&
                done.GetBoolean())
                yield break;
        }
    }
    public async IAsyncEnumerable<string> ChatWithImageStreamAsync(
    string model,
    string message,
    string imagePath,
    [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
{
    var imageBytes = await File.ReadAllBytesAsync(imagePath, cancellationToken);
    var base64Image = Convert.ToBase64String(imageBytes);

    var requestBody = JsonSerializer.Serialize(new
    {
        model,
        stream = true,
        messages = new[]
        {
            new
            {
                role = "user",
                content = message,
                images = new[] { base64Image }
            }
        }
    });

    var request = new HttpRequestMessage(HttpMethod.Post, $"{_baseUrl}/api/chat")
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

    while (!reader.EndOfStream && !cancellationToken.IsCancellationRequested)
    {
        var line = await reader.ReadLineAsync();
        if (string.IsNullOrWhiteSpace(line))
            continue;

        using var doc = JsonDocument.Parse(line);

        if (doc.RootElement.TryGetProperty("message", out var msg) &&
            msg.TryGetProperty("content", out var content))
        {
            yield return content.GetString();
        }

        if (doc.RootElement.TryGetProperty("done", out var done) &&
            done.GetBoolean())
            yield break;
    }
}
    public async IAsyncEnumerable<string> ChatStreamAsync(
    string model,
    string message,
    [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var requestBody = JsonSerializer.Serialize(new
        {
            model,
            stream = true,
            messages = new[]
            {
                new { role = "user", content = message }
            }
        });

        var request = new HttpRequestMessage(HttpMethod.Post, $"{_baseUrl}/api/chat")
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

        while (!reader.EndOfStream && !cancellationToken.IsCancellationRequested)
        {
            var line = await reader.ReadLineAsync();
            if (string.IsNullOrWhiteSpace(line))
                continue;

            using var doc = JsonDocument.Parse(line);

            if (doc.RootElement.TryGetProperty("message", out var messageObj) &&
                messageObj.TryGetProperty("content", out var content))
            {
                yield return content.GetString();
            }

            if (doc.RootElement.TryGetProperty("done", out var done) &&
                done.GetBoolean())
            {
                yield break;
            }
        }
    }

    public async IAsyncEnumerable<string> GenerateStreamAsync(
        string model,
        string prompt,
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var requestBody = JsonSerializer.Serialize(new
        {
            model,
            prompt,
            stream = true,
            options = new
            {
                temperature = 0.7,
                top_p = 0.9,
                num_predict = 200
            }
        });

        var request = new HttpRequestMessage(HttpMethod.Post, $"{_baseUrl}/api/generate")
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

        while (!reader.EndOfStream && !cancellationToken.IsCancellationRequested)
        {
            var line = await reader.ReadLineAsync();
            if (string.IsNullOrWhiteSpace(line))
                continue;

            using var doc = JsonDocument.Parse(line);

            if (doc.RootElement.TryGetProperty("response", out var token))
            {
                yield return token.GetString();
            }

            // stop condition from Ollama
            if (doc.RootElement.TryGetProperty("done", out var done) &&
                done.GetBoolean())
            {
                yield break;
            }
        }
    }

    // 🔧 Helper POST method
    private async Task<string> PostAsync(string endpoint, string json)
    {
        var content = new StringContent(json, Encoding.UTF8, "application/json");
        var response = await _http.PostAsync($"{_baseUrl}{endpoint}", content);
        return await response.Content.ReadAsStringAsync();
    }
}