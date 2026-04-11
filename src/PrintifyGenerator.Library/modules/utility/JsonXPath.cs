using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json.Nodes;

public static class JsonXPath
{
    // =========================
    // LOAD / SAVE
    // =========================

    public static JsonNode Load(string path)
    {
        var json = File.ReadAllText(path);
        return JsonNode.Parse(json)
               ?? throw new Exception("Invalid JSON file");
    }

    public static void Save(JsonNode node, string path)
    {
        File.WriteAllText(path, node.ToJsonString(new System.Text.Json.JsonSerializerOptions
        {
            WriteIndented = true
        }));
    }

    // =========================
    // PUBLIC API
    // =========================

public static JsonNode Set(JsonNode root, string path, JsonNode value)
{
    var targets = SelectTargets(root, path);

    foreach (var t in targets)
    {
        t.Parent[t.Key] = value.DeepClone();
    }

    return root;
}

    public static List<JsonNode?> Select(JsonNode root, string path)
    {
        var results = new List<JsonNode?>();
        var tokens = Tokenize(path);

        Walk(root, null, null, tokens, 0, results);
        return results;
    }

    // =========================
    // INTERNAL TARGET MODEL
    // =========================

    private record Target(JsonObject Parent, string Key);

    private static List<Target> SelectTargets(JsonNode root, string path)
    {
        var results = new List<Target>();
        var tokens = Tokenize(path);

        WalkTargets(root, null, null, tokens, 0, results);

        return results;
    }

    // =========================
    // CORE WALK (READ)
    // =========================

    private static void Walk(
        JsonNode node,
        JsonObject? parent,
        string? key,
        List<string> tokens,
        int index,
        List<JsonNode?> results)
    {
        if (node is null || index >= tokens.Count)
            return;

        var token = tokens[index];

        if (index == tokens.Count - 1)
        {
            if (node is JsonObject obj)
            {
                foreach (var kv in obj)
                {
                    if (Match(kv.Key, token))
                        results.Add(kv.Value);
                }
            }
            return;
        }

        if (node is JsonObject obj2)
        {
            foreach (var kv in obj2)
            {
                if (Match(kv.Key, token) || token == "*")
                {
                    Walk(kv.Value!, obj2, kv.Key, tokens, index + 1, results);
                }
            }

            // recursive descent //
            if (token == "//")
            {
                // match next token against this node's own keys
                Walk(node, parent, key, tokens, index + 1, results);
                // then recurse into children still carrying //
                foreach (var kv in obj2)
                {
                    Walk(kv.Value!, obj2, kv.Key, tokens, index, results);
                }
            }
        }
    }

    // =========================
    // CORE WALK (WRITE TARGETS)
    // =========================

    private static void WalkTargets(
        JsonNode node,
        JsonObject? parent,
        string? key,
        List<string> tokens,
        int index,
        List<Target> results)
    {
        if (node is null || index >= tokens.Count)
            return;

        var token = tokens[index];

        if (node is JsonObject obj)
        {
            foreach (var kv in obj)
            {
                bool match = Match(kv.Key, token) || token == "*";

                if (index == tokens.Count - 1 && match)
                {
                    results.Add(new Target(obj, kv.Key));
                }
                else if (match)
                {
                    WalkTargets(kv.Value!, obj, kv.Key, tokens, index + 1, results);
                }
            }

            // recursive descent //
            if (token == "//")
            {
                // match next token against this node's own keys
                WalkTargets(node, parent, key, tokens, index + 1, results);
                // then recurse into children still carrying //
                foreach (var kv in obj)
                {
                    WalkTargets(kv.Value!, obj, kv.Key, tokens, index, results);
                }
            }
        }
    }

    // =========================
    // HELPERS
    // =========================

    private static bool Match(string key, string token)
        => token == "*" || key == token;

    private static List<string> Tokenize(string path)
    {
        var result = new List<string>();
        var buffer = "";

        for (int i = 0; i < path.Length; i++)
        {
            if (path[i] == '/')
            {
                if (buffer.Length > 0)
                {
                    result.Add(buffer);
                    buffer = "";
                }

                if (i + 1 < path.Length && path[i + 1] == '/')
                {
                    result.Add("//");
                    i++;
                }
            }
            else
            {
                buffer += path[i];
            }
        }

        if (buffer.Length > 0)
            result.Add(buffer);

        return result;
    }
}