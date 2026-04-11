using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;

public static class WorkflowManager
{
    // 📂 Load workflow
    public static Dictionary<string, object> LoadWorkflow(string path)
    {
        var json = File.ReadAllText(path);

        return JsonSerializer.Deserialize<Dictionary<string, object>>(
            json,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
        ) ?? new Dictionary<string, object>();
    }

    // 🔁 Deep clone (like Python copy.deepcopy)
    private static Dictionary<string, object> Clone(Dictionary<string, object> workflow)
    {
        var json = JsonSerializer.Serialize(workflow);
        return JsonSerializer.Deserialize<Dictionary<string, object>>(json)!;
    }

    // 🏷 Label helper (equivalent to Python _label)
    private static string Label(Dictionary<string, object> node)
    {
        var meta = node.ContainsKey("_meta") && node["_meta"] is JsonElement m
            ? JsonSerializer.Deserialize<Dictionary<string, object>>(m.GetRawText())
            : node.ContainsKey("_meta") && node["_meta"] is Dictionary<string, object> md
                ? md
                : new Dictionary<string, object>();

        var title = meta.TryGetValue("title", out var t) ? t?.ToString() : "";
        var classType = node.TryGetValue("class_type", out var ct) ? ct?.ToString() : "";

        return $"{classType} {title}".Trim().ToLower();
    }

    // 🔍 Enumerate all nodes recursively
    private static IEnumerable<(string id, Dictionary<string, object> node)> EnumerateNodes(
        Dictionary<string, object> workflow)
    {
        foreach (var (key, value) in workflow)
        {
            if (value is Dictionary<string, object> node)
            {
                yield return (key, node);

                foreach (var child in EnumerateNodes(node))
                    yield return child;
            }
        }
    }

    // 🧠 Main override function (C# version of apply_overrides)
    public static (Dictionary<string, object> workflow, List<string> changes) ApplyOverrides(
        Dictionary<string, object> workflow,
        string? prompt = null,
        string? negativePrompt = null,
        int? seed = null,
        int? steps = null,
        double? cfg = null,
        int? width = null,
        int? height = null,
        int? batchSize = null,
        string? checkpointName = null,
        string? loraName = null,
        double? loraStrength = null,
        string? savePrefix = null)
    {
        var updated = Clone(workflow);
        var changes = new List<string>();

        var nodes = EnumerateNodes(updated).ToList();

        var textNodes = new List<(string id, Dictionary<string, object> node)>();
        var positiveCandidates = new List<(string id, Dictionary<string, object> node)>();
        var negativeCandidates = new List<(string id, Dictionary<string, object> node)>();

        foreach (var (id, node) in nodes)
        {
            if (!node.TryGetValue("inputs", out var inputsObj) ||
                inputsObj is not Dictionary<string, object> inputs)
                continue;

            if (inputs.ContainsKey("text"))
            {
                textNodes.Add((id, node));

                var label = Label(node);
                if (label.Contains("negative"))
                    negativeCandidates.Add((id, node));
                else
                    positiveCandidates.Add((id, node));
            }

            // Checkpoint swap
            if (checkpointName != null)
            {
                foreach (var key in new[] { "ckpt_name", "checkpoint", "model_name", "unet_name" })
                {
                    if (inputs.ContainsKey(key) && inputs[key] is string)
                    {
                        inputs[key] = checkpointName;
                        changes.Add($"{id}.{key}={checkpointName}");
                        break;
                    }
                }
            }

            // LoRA name
            if (loraName != null)
            {
                foreach (var key in new[] { "lora_name", "adapter_name" })
                {
                    if (inputs.ContainsKey(key) && inputs[key] is string)
                    {
                        inputs[key] = loraName;
                        changes.Add($"{id}.{key}={loraName}");
                    }
                }
            }

            // LoRA strength
            if (loraStrength != null)
            {
                foreach (var key in new[] { "strength_model", "strength_clip", "lora_strength", "weight" })
                {
                    if (inputs.ContainsKey(key) && inputs[key] is JsonElement je &&
                        je.ValueKind == JsonValueKind.Number)
                    {
                        inputs[key] = loraStrength.Value;
                        changes.Add($"{id}.{key}={loraStrength}");
                    }
                }
            }

            // Numeric overrides
            void SetNum(string key, object value, string name)
            {
                if (inputs.ContainsKey(key))
                {
                    inputs[key] = value;
                    changes.Add($"{id}.{key}={value}");
                }
            }

            if (batchSize.HasValue) SetNum("batch_size", batchSize.Value, "batch_size");
            if (width.HasValue) SetNum("width", width.Value, "width");
            if (height.HasValue) SetNum("height", height.Value, "height");
            if (seed.HasValue) SetNum("seed", seed.Value, "seed");
            if (steps.HasValue) SetNum("steps", steps.Value, "steps");
            if (cfg.HasValue) SetNum("cfg", cfg.Value, "cfg");

            if (savePrefix != null && inputs.ContainsKey("filename_prefix"))
            {
                inputs["filename_prefix"] = savePrefix;
                changes.Add($"{id}.filename_prefix={savePrefix}");
            }
        }

        // 🎯 Prompt injection (positive)
        if (prompt != null)
        {
            var target = positiveCandidates.FirstOrDefault();
            if (target.node == null)
                target = textNodes.FirstOrDefault();

            if (target.node != null &&
                target.node["inputs"] is Dictionary<string, object> tInputs)
            {
                tInputs["text"] = prompt;
                changes.Add($"{target.id}.text=<prompt>");
            }
        }

        // 🚫 Negative prompt injection
        if (negativePrompt != null)
        {
            var target = negativeCandidates.FirstOrDefault();
            if (target.node == null && textNodes.Count > 1)
                target = textNodes[1];

            if (target.node != null &&
                target.node["inputs"] is Dictionary<string, object> nInputs)
            {
                nInputs["text"] = negativePrompt;
                changes.Add($"{target.id}.text=<negative_prompt>");
            }
        }

        return (updated, changes);
    }
}