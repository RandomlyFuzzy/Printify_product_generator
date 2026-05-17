using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;

namespace PrintifyGenerator.ColoringBookGenerator.Utilities
{
    public record PromptEntry
    {
        public string Source { get; init; } = "";
        public string Key { get; init; } = "";
        public DateTime Timestamp { get; init; }
        public string Prompt { get; init; } = "";
    }

    public static class PromptRecorder
    {
        private static readonly object _lock = new();
        private static readonly List<PromptEntry> _entries = new();

        public static void Record(string source, string key, string prompt)
        {
            if (prompt is null) prompt = string.Empty;
            lock (_lock)
            {
                _entries.Add(new PromptEntry
                {
                    Source = source ?? string.Empty,
                    Key = key ?? string.Empty,
                    Timestamp = DateTime.UtcNow,
                    Prompt = prompt
                });
            }
        }

        public static async Task SaveToDirectoryAsync(string outputDir)
        {
            if (string.IsNullOrWhiteSpace(outputDir)) return;
            try
            {
                List<PromptEntry> snapshot;
                lock (_lock)
                {
                    snapshot = new List<PromptEntry>(_entries);
                }

                Directory.CreateDirectory(outputDir);
                var filePath = Path.Combine(outputDir, "prompts.json");
                var json = JsonSerializer.Serialize(snapshot, new JsonSerializerOptions { WriteIndented = true });
                await File.WriteAllTextAsync(filePath, json);
            }
            catch
            {
                // best-effort; do not throw during generation
            }
        }
    }
}
