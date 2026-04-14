using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using PrintifyGenerator.Dashboard.Services;

namespace PrintifyGenerator.Dashboard.Pages;

public sealed class StatsModel : PageModel
{
    private readonly DashboardDataService _dashboardDataService;
    private readonly GenerationRuntimeService _generationRuntimeService;
    private readonly NodeHealthService _nodeHealthService;

    public StatsModel(
        DashboardDataService dashboardDataService,
        GenerationRuntimeService generationRuntimeService,
        NodeHealthService nodeHealthService)
    {
        _dashboardDataService = dashboardDataService;
        _generationRuntimeService = generationRuntimeService;
        _nodeHealthService = nodeHealthService;
    }

    [TempData]
    public string? FlashMessage { get; set; }

    public DashboardSnapshot Snapshot { get; private set; } = DashboardSnapshot.Empty;
    public GenerationRuntimeSnapshot Runtime { get; private set; } = GenerationRuntimeSnapshot.Empty;
    public IReadOnlyDictionary<string, NodeHealthSnapshot> NodeHealth { get; private set; } = new Dictionary<string, NodeHealthSnapshot>();

    public int MaxDailyVolume => Snapshot.DailyVolumes.Count == 0 ? 1 : Snapshot.DailyVolumes.Max(entry => entry.Count);
    public int MaxScoreBandCount => Snapshot.ScoreBands.Count == 0 ? 1 : Snapshot.ScoreBands.Max(entry => entry.Count);

    public async Task OnGetAsync(CancellationToken cancellationToken)
    {
        await LoadPageAsync(cancellationToken);
    }

    public IActionResult OnGetRuntimeState()
    {
        return new JsonResult(_generationRuntimeService.GetSnapshot());
    }

    public IActionResult OnPostStartRunner()
    {
        _generationRuntimeService.Start();
        FlashMessage = "Live generation runner resumed.";
        return RedirectToPage();
    }

    public IActionResult OnPostStopRunner()
    {
        _generationRuntimeService.Stop();
        FlashMessage = "Live generation runner will pause after the current work item.";
        return RedirectToPage();
    }

    public IActionResult OnPostSaveModels(string promptModel, string suitabilityModel, string mockupVisionModel, float minimumPublishScore)
    {
        var settings = _dashboardDataService.LoadSettings();
        settings.PromptModel = promptModel;
        settings.SuitabilityModel = suitabilityModel;
        settings.MockupVisionModel = mockupVisionModel;
        settings.MinimumPublishScore = Math.Clamp(minimumPublishScore, 0.0f, 10.0f);

        _dashboardDataService.SaveSettings(settings);
        FlashMessage = "Shared generation settings saved.";
        return RedirectToPage();
    }

    public IActionResult OnPostAddNode(string nodeType, string name, string baseUrl)
    {
        if (!TryNormalizeNodeInput(name, baseUrl, out var normalizedName, out var normalizedBaseUrl, out var errorMessage))
        {
            FlashMessage = errorMessage;
            return RedirectToPage();
        }

        var settings = _dashboardDataService.LoadSettings();
        var nodes = GetNodes(settings, nodeType);
        nodes.Add(new OrchestrationNode
        {
            Name = normalizedName,
            BaseUrl = normalizedBaseUrl,
            Enabled = true
        });

        _dashboardDataService.SaveSettings(settings);
        FlashMessage = $"{PrettyNodeType(nodeType)} node added.";
        return RedirectToPage();
    }

    public IActionResult OnPostSaveNode(string nodeType, string id, string name, string baseUrl, bool enabled)
    {
        if (!TryNormalizeNodeInput(name, baseUrl, out var normalizedName, out var normalizedBaseUrl, out var errorMessage))
        {
            FlashMessage = errorMessage;
            return RedirectToPage();
        }

        var settings = _dashboardDataService.LoadSettings();
        var nodes = GetNodes(settings, nodeType);
        var node = nodes.FirstOrDefault(candidate => string.Equals(candidate.Id, id, StringComparison.OrdinalIgnoreCase));
        if (node is null)
        {
            FlashMessage = $"{PrettyNodeType(nodeType)} node not found.";
            return RedirectToPage();
        }

        node.Name = normalizedName;
        node.BaseUrl = normalizedBaseUrl;
        node.Enabled = enabled;

        _dashboardDataService.SaveSettings(settings);
        FlashMessage = $"{PrettyNodeType(nodeType)} node updated.";
        return RedirectToPage();
    }

    public IActionResult OnPostDeleteNode(string nodeType, string id)
    {
        var settings = _dashboardDataService.LoadSettings();
        var nodes = GetNodes(settings, nodeType);
        var removed = nodes.RemoveAll(candidate => string.Equals(candidate.Id, id, StringComparison.OrdinalIgnoreCase));

        if (removed > 0)
        {
            _dashboardDataService.SaveSettings(settings);
            FlashMessage = $"{PrettyNodeType(nodeType)} node removed.";
        }
        else
        {
            FlashMessage = $"{PrettyNodeType(nodeType)} node not found.";
        }

        return RedirectToPage();
    }

    public NodeHealthSnapshot GetNodeHealth(string nodeId)
    {
        return NodeHealth.TryGetValue(nodeId, out var snapshot)
            ? snapshot
            : NodeHealthSnapshot.Unknown(nodeId);
    }

    private async Task LoadPageAsync(CancellationToken cancellationToken)
    {
        Snapshot = _dashboardDataService.LoadSnapshot(GalleryStatusFilter.All);
        Runtime = _generationRuntimeService.GetSnapshot();
        NodeHealth = await _nodeHealthService.ProbeAsync(Snapshot.Settings, cancellationToken);
    }

    private static List<OrchestrationNode> GetNodes(OrchestrationSettings settings, string nodeType)
    {
        return string.Equals(nodeType, "comfyui", StringComparison.OrdinalIgnoreCase)
            ? settings.ComfyUi
            : settings.Ollama;
    }

    private static string PrettyNodeType(string nodeType)
    {
        return string.Equals(nodeType, "comfyui", StringComparison.OrdinalIgnoreCase)
            ? "ComfyUI"
            : "Ollama";
    }

    private static bool TryNormalizeNodeInput(
        string name,
        string baseUrl,
        out string normalizedName,
        out string normalizedBaseUrl,
        out string errorMessage)
    {
        normalizedName = (name ?? string.Empty).Trim();
        normalizedBaseUrl = OrchestrationSettingsStore.NormalizeBaseUrl(baseUrl);
        errorMessage = string.Empty;

        if (string.IsNullOrWhiteSpace(normalizedName))
        {
            errorMessage = "Node name is required.";
            return false;
        }

        if (!Uri.TryCreate(normalizedBaseUrl, UriKind.Absolute, out var uri)
            || (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
        {
            errorMessage = "A valid http or https base URL is required.";
            return false;
        }

        return true;
    }
}