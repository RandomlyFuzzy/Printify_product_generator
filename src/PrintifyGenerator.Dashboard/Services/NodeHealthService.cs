using System.Diagnostics;

namespace PrintifyGenerator.Dashboard.Services;

public sealed class NodeHealthService
{
    private readonly HttpClient _httpClient;

    public NodeHealthService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<IReadOnlyDictionary<string, NodeHealthSnapshot>> ProbeAsync(
        OrchestrationSettings settings,
        CancellationToken cancellationToken = default)
    {
        var probes = settings.Ollama
            .Select(node => ProbeNodeAsync(node, "ollama", cancellationToken))
            .Concat(settings.ComfyUi.Select(node => ProbeNodeAsync(node, "comfyui", cancellationToken)));

        var snapshots = await Task.WhenAll(probes);
        return snapshots.ToDictionary(snapshot => snapshot.NodeId, snapshot => snapshot, StringComparer.OrdinalIgnoreCase);
    }

    private async Task<NodeHealthSnapshot> ProbeNodeAsync(
        OrchestrationNode node,
        string nodeType,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(node.BaseUrl))
            return new NodeHealthSnapshot(node.Id, false, "Offline", "No base URL configured", null);

        var endpoint = BuildHealthUrl(nodeType, node.BaseUrl);
        if (!Uri.TryCreate(endpoint, UriKind.Absolute, out var uri))
            return new NodeHealthSnapshot(node.Id, false, "Offline", "Invalid URL", null);

        var timer = Stopwatch.StartNew();
        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, uri);
            using var response = await _httpClient.SendAsync(request, cancellationToken);
            timer.Stop();

            if (!response.IsSuccessStatusCode)
            {
                return new NodeHealthSnapshot(
                    node.Id,
                    false,
                    "Offline",
                    $"HTTP {(int)response.StatusCode}",
                    timer.ElapsedMilliseconds);
            }

            return new NodeHealthSnapshot(node.Id, true, "Online", $"{timer.ElapsedMilliseconds} ms", timer.ElapsedMilliseconds);
        }
        catch (Exception ex)
        {
            timer.Stop();
            return new NodeHealthSnapshot(node.Id, false, "Offline", ex.Message, timer.ElapsedMilliseconds);
        }
    }

    private static string BuildHealthUrl(string nodeType, string baseUrl)
    {
        var normalizedBaseUrl = OrchestrationSettingsStore.NormalizeBaseUrl(baseUrl);
        return string.Equals(nodeType, "comfyui", StringComparison.OrdinalIgnoreCase)
            ? $"{normalizedBaseUrl}/system_stats"
            : $"{normalizedBaseUrl}/api/tags";
    }
}

public sealed record NodeHealthSnapshot(
    string NodeId,
    bool IsOnline,
    string DisplayText,
    string DetailText,
    long? LatencyMs)
{
    public string CssClass => IsOnline ? "health-online" : "health-offline";

    public static NodeHealthSnapshot Unknown(string nodeId)
    {
        return new NodeHealthSnapshot(nodeId, false, "Unknown", "Status has not been checked yet", null);
    }
}