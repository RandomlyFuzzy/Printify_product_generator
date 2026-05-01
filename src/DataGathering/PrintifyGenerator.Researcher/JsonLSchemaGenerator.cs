using System;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace FlatSchemaCompiler
{
    // ================= CONFIG =================

    public class SchemaOptions
    {
        public bool UseFuzzyGrouping { get; set; } = true;
        public double FuzzyThreshold { get; set; } = 0.85;
    }

    // ================= COMPILER =================

    public class SchemaCompiler
    {
        private readonly SchemaOptions _options;
        private readonly Dictionary<string, SchemaField> _fields = new();

        public SchemaCompiler(SchemaOptions options)
        {
            _options = options;
        }

        public void AddJson(string json)
        {
            using var doc = JsonDocument.Parse(json);
            ProcessElement(doc.RootElement, "");
        }

        // ================= STREAM PROCESSING =================

        private void ProcessElement(JsonElement element, string path)
        {
            switch (element.ValueKind)
            {
                case JsonValueKind.Object:
                    foreach (var prop in element.EnumerateObject())
                    {
                        string key = SchemaKey.Normalize(prop.Name);
                        string fullKey = path + "/" + key;

                        ref var field = ref CollectionsMarshal.GetValueRefOrAddDefault(
                            _fields,
                            fullKey,
                            out bool exists);

                        if (!exists || field == null)
                            field = new SchemaField();

                        field.Count++;
                        field.Aliases.Add(prop.Name);

                        ProcessElement(prop.Value, fullKey);
                    }
                    break;

                case JsonValueKind.Array:
                    foreach (var item in element.EnumerateArray())
                        ProcessElement(item, path + "[]");
                    break;

                case JsonValueKind.String:
                    AddType(path, SchemaType.String);
                    break;

                case JsonValueKind.Number:
                    AddType(path, SchemaType.Number);
                    break;

                case JsonValueKind.True:
                case JsonValueKind.False:
                    AddType(path, SchemaType.Bool);
                    break;
            }
        }

        private void AddType(string path, SchemaType type)
        {
            if (string.IsNullOrEmpty(path))
                return;

            ref var field = ref CollectionsMarshal.GetValueRefOrAddDefault(
                _fields,
                path,
                out bool exists);

            if (!exists || field == null)
                field = new SchemaField();

            field.Types |= type;
        }

        // ================= GENERATION =================

        public string Generate(string rootName)
        {
            var sb = new StringBuilder(64 * 1024);

            sb.AppendLine("using System;");
            sb.AppendLine("using System.Collections.Generic;");
            sb.AppendLine("using System.Text.Json.Serialization;");
            sb.AppendLine();

            var grouped = new Dictionary<string, List<KeyValuePair<string, SchemaField>>>();

            foreach (var kv in _fields)
            {
                string parent = GetParentPath(kv.Key);

                if (!grouped.TryGetValue(parent, out var list))
                {
                    list = new List<KeyValuePair<string, SchemaField>>();
                    grouped[parent] = list;
                }

                list.Add(kv);
            }

            foreach (var group in grouped)
            {
                string className = SchemaKey.ToClassName(group.Key);

                sb.AppendLine($"public class {className}");
                sb.AppendLine("{");

                var fields = group.Value;
                fields.Sort((a, b) => b.Value.Count.CompareTo(a.Value.Count));

                foreach (var f in fields)
                {
                    string propName = SchemaKey.ToPropertyName(GetLastSegment(f.Key));
                    var field = f.Value;

                    sb.AppendLine($"    // Occurs {field.Count}");

                    sb.Append("    [JsonPropertyName(\"");
                    sb.Append(string.Join(",", field.Aliases));
                    sb.AppendLine("\")]");

                    sb.AppendLine($"    public string {propName} {{ get; set; }}");
                    sb.AppendLine();
                }

                sb.AppendLine("}");
            }

            return sb.ToString();
        }

        private static string GetParentPath(string path)
        {
            int i = path.LastIndexOf('/');
            return i <= 0 ? "" : path[..i];
        }

        private static string GetLastSegment(string path)
        {
            int i = path.LastIndexOf('/');
            return i < 0 ? path : path[(i + 1)..];
        }
    }

    // ================= FIELD MODEL =================

    public class SchemaField
    {
        public int Count;
        public SchemaType Types;
        public HashSet<string> Aliases = new();
    }

    [Flags]
    public enum SchemaType
    {
        None = 0,
        String = 1,
        Number = 2,
        Bool = 4
    }

    // ================= SAFE C# IDENTIFIER HELPERS =================

    public static class SchemaKey
    {
        public static string Normalize(string s)
        {
            Span<char> buffer = stackalloc char[s.Length];
            int j = 0;

            for (int i = 0; i < s.Length; i++)
            {
                char c = s[i];
                if (char.IsLetterOrDigit(c))
                    buffer[j++] = char.ToLowerInvariant(c);
            }

            return new string(buffer[..j]);
        }

        public static string ToPropertyName(string s)
        {
            if (string.IsNullOrWhiteSpace(s))
                return "Property";

            Span<char> buffer = stackalloc char[s.Length];
            int j = 0;

            for (int i = 0; i < s.Length; i++)
            {
                char c = s[i];
                if (char.IsLetterOrDigit(c))
                    buffer[j++] = c;
            }

            if (j == 0)
                return "Property";

            string result = new string(buffer[..j]);

            // 🔥 FIX: cannot start with digit
            if (char.IsDigit(result[0]))
                result = "_" + result;

            return result;
        }

        public static string ToClassName(string path)
        {
            if (string.IsNullOrEmpty(path))
                return "Root";

            Span<char> buffer = stackalloc char[path.Length];
            int j = 0;
            bool upper = true;

            for (int i = 0; i < path.Length; i++)
            {
                char c = path[i];

                if (!char.IsLetterOrDigit(c))
                {
                    upper = true;
                    continue;
                }

                buffer[j++] = upper ? char.ToUpperInvariant(c) : c;
                upper = false;
            }

            return new string(buffer[..j]);
        }
    }
}