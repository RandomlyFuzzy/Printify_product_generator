using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;

public static class OrchestrationSettingsStore
{
    private static readonly JsonSerializerOptions JsonOptions = new JsonSerializerOptions
    {
        PropertyNameCaseInsensitive = true,
        WriteIndented = true
    };

    public static string GetSettingsPath(string dataBasePath)
    {
        var normalizedDataRoot = Path.GetFullPath(dataBasePath);
        return Path.Combine(normalizedDataRoot, "staging", "orchestration-settings.json");
    }

    public static OrchestrationSettings Load(string dataBasePath)
    {
        var settingsPath = GetSettingsPath(dataBasePath);
        if (!File.Exists(settingsPath))
        {
            var defaults = CreateDefault();
            Save(dataBasePath, defaults);
            return defaults;
        }

        try
        {
            var json = File.ReadAllText(settingsPath);
            var settings = JsonSerializer.Deserialize<OrchestrationSettings>(json, JsonOptions);
            return Sanitize(settings ?? CreateDefault());
        }
        catch
        {
            var defaults = CreateDefault();
            Save(dataBasePath, defaults);
            return defaults;
        }
    }

    public static void Save(string dataBasePath, OrchestrationSettings settings)
    {
        var sanitized = Sanitize(settings);
        var settingsPath = GetSettingsPath(dataBasePath);
        var directory = Path.GetDirectoryName(settingsPath);
        if (!string.IsNullOrWhiteSpace(directory))
            Directory.CreateDirectory(directory);

        File.WriteAllText(settingsPath, JsonSerializer.Serialize(sanitized, JsonOptions));
    }

    public static string NormalizeBaseUrl(string? baseUrl)
    {
        return (baseUrl ?? string.Empty).Trim().TrimEnd('/');
    }

    public static OrchestrationSettings CreateDefault()
    {
        return new OrchestrationSettings
        {
            PromptModel = "llama3.2:1b",
            SuitabilityModel = "gemma4:e2b",
            MockupVisionModel = "gemma4:e4b",
            MinimumPublishScore = 6.0f,
            Ollama = new List<OrchestrationNode>
            {
                new OrchestrationNode
                {
                    Name = "Local Ollama",
                    BaseUrl = "http://localhost:11434",
                    Enabled = true
                },
                new OrchestrationNode
                {
                    Name = "Remote Ollama 131",
                    BaseUrl = "http://192.168.0.131:11434",
                    Enabled = true
                },
                new OrchestrationNode
                {
                    Name = "Remote Ollama 151",
                    BaseUrl = "http://192.168.0.151:11434",
                    Enabled = true
                }
            },
            ComfyUi = new List<OrchestrationNode>
            {
                new OrchestrationNode
                {
                    Name = "ComfyUI 151",
                    BaseUrl = "http://192.168.0.151:8188",
                    Enabled = true
                }
            }
        };
    }

    private static OrchestrationSettings Sanitize(OrchestrationSettings settings)
    {
        settings.PromptModel = SanitizeModel(settings.PromptModel, "llama3.2:1b");
        settings.SuitabilityModel = SanitizeModel(settings.SuitabilityModel, "gemma4:e2b");
        settings.MockupVisionModel = SanitizeModel(settings.MockupVisionModel, "gemma4:e4b");
        settings.MinimumPublishScore = SanitizeMinimumPublishScore(settings.MinimumPublishScore);
        settings.Ollama = SanitizeNodes(settings.Ollama, "Ollama");
        settings.ComfyUi = SanitizeNodes(settings.ComfyUi, "ComfyUI");
        return settings;
    }

    private static string SanitizeModel(string? modelName, string fallback)
    {
        return string.IsNullOrWhiteSpace(modelName)
            ? fallback
            : modelName.Trim();
    }

    private static float SanitizeMinimumPublishScore(float minimumPublishScore)
    {
        if (float.IsNaN(minimumPublishScore) || float.IsInfinity(minimumPublishScore))
            return 6.0f;

        return Math.Clamp(minimumPublishScore, 0.0f, 10.0f);
    }

    private static List<OrchestrationNode> SanitizeNodes(List<OrchestrationNode>? nodes, string defaultPrefix)
    {
        var sanitized = new List<OrchestrationNode>();
        foreach (var node in nodes ?? new List<OrchestrationNode>())
        {
            var baseUrl = NormalizeBaseUrl(node.BaseUrl);
            if (string.IsNullOrWhiteSpace(baseUrl))
                continue;

            sanitized.Add(new OrchestrationNode
            {
                Id = string.IsNullOrWhiteSpace(node.Id) ? Guid.NewGuid().ToString("N") : node.Id.Trim(),
                Name = string.IsNullOrWhiteSpace(node.Name) ? $"{defaultPrefix} {sanitized.Count + 1}" : node.Name.Trim(),
                BaseUrl = baseUrl,
                Enabled = node.Enabled
            });
        }

        return sanitized;
    }
}