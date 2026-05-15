using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.Extensions.Options;
using PrintifyGenerator.Dashboard.Models;

namespace PrintifyGenerator.Dashboard.Services;

public sealed class GenerationRuntimeService : BackgroundService
{
    private const int MaxRetainedTerminalItems = 18;
    private const int MaxRetainedLogEntries = 90;

    private static readonly TimeSpan IdleDelay = TimeSpan.FromSeconds(2);
    private static readonly TimeSpan ConnectionRetryDelay = TimeSpan.FromSeconds(5);
    private static readonly TimeSpan JobPollDelay = TimeSpan.FromMilliseconds(750);
    private static readonly TimeSpan JobTimeout = TimeSpan.FromMinutes(20);
    private static readonly JsonSerializerOptions PrettyJsonOptions = new() { WriteIndented = true };

        private const string PromptTemplate = """
You are a prompt engineer for AI image generation. Output ONLY a valid JSON array with no markdown, no explanation, no code fences, and no extra text.
Generate exactly 2 creative Stable Diffusion prompts for print-on-demand products such as t-shirts, posters, and mugs.
Each prompt should be visually striking, commercially appealing, and distinct in style. Avoid text in the image.
Each item must use this exact JSON shape:
[
    {
        "positive": "detailed positive prompt here, comma separated tags, style, lighting, quality boosters",
        "negative": "ugly, deformed, blurry, low quality, watermark, text, bad anatomy, extra limbs",
        "width": 512,
        "height": 512,
        "steps": 25,
        "cfg": 7
    }
]
""";

    private static readonly string SuitabilityPrompt =
        "You are reviewing artwork for public print-on-demand sales. " +
        "Return only JSON describing whether the image is safe, commercially suitable, and free of IP or legal risk. " +
        "Use this exact schema: " + new ImageSuitability().PrettyJsonString();

    private readonly object _sync = new();
    private readonly IWebHostEnvironment _environment;
    private readonly IOptionsMonitor<DashboardOptions> _options;
    private readonly ILogger<GenerationRuntimeService> _logger;
    private readonly List<RuntimeQueueItem> _items = new();
    private readonly Queue<RuntimeQueueItem> _pendingWorkItems = new();
    private readonly List<GenerationLogEntry> _logs = new();

    private RoundRobinSelector<OllamaRunnerNode>? _ollamaPool;
    private RoundRobinSelector<ComfyRunnerNode>? _comfyPool;
    private string _settingsSignature = string.Empty;
    private string _dataRoot = string.Empty;
    private string _checkingRoot = string.Empty;
    private OrchestrationSettings _settings = new();
    private bool _isRunning = true;
    private string _stateLabel = "Running";
    private string _stateCssClass = "runner-state-running";
    private string _statusMessage = "Preparing the first prompt batch.";
    private DateTime _lastUpdatedUtc = DateTime.UtcNow;
    private int _sessionPromptCount;
    private int _sessionImageCount;
    private int _sessionSuitabilityCount;
    private int _sessionFailureCount;
    private string? _lastConnectionIssue;
    private FileStream? _runnerLockHandle;
    private string? _runnerLockPath;

    public GenerationRuntimeService(
        IWebHostEnvironment environment,
        IOptionsMonitor<DashboardOptions> options,
        ILogger<GenerationRuntimeService> logger)
    {
        _environment = environment;
        _options = options;
        _logger = logger;

        lock (_sync)
        {
            AppendLogLocked("info", "Live generation runner initialized.");
        }
    }

    public GenerationRuntimeSnapshot GetSnapshot()
    {
        lock (_sync)
        {
            return new GenerationRuntimeSnapshot(
                IsRunning: _isRunning,
                StateLabel: _stateLabel,
                StateCssClass: _stateCssClass,
                StatusMessage: _statusMessage,
                PendingQueueCount: _pendingWorkItems.Count,
                ActiveItemCount: _items.Count(item => !item.IsTerminal && item.StageKey != "queued"),
                SessionPromptCount: _sessionPromptCount,
                SessionImageCount: _sessionImageCount,
                SessionSuitabilityCount: _sessionSuitabilityCount,
                SessionFailureCount: _sessionFailureCount,
                LastUpdatedUtc: _lastUpdatedUtc,
                Items: _items
                    .OrderByDescending(item => item.UpdatedAtUtc)
                    .Select(item => item.ToSnapshot())
                    .ToList(),
                Logs: _logs
                    .OrderByDescending(entry => entry.TimestampUtc)
                    .ToList());
        }
    }

    public void Start()
    {
        lock (_sync)
        {
            if (_isRunning)
                return;

            _isRunning = true;
            UpdateStateLocked("Running", "runner-state-running", "Resuming prompt generation.");
            AppendLogLocked("info", "Live runner resumed.");
        }
    }

    public void Stop()
    {
        lock (_sync)
        {
            if (!_isRunning)
                return;

            _isRunning = false;
            UpdateStateLocked("Pausing", "runner-state-paused", "The runner will pause after the current work item finishes.");
            AppendLogLocked("info", "Live runner will pause after the current work item.");
        }
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            if (!IsRunning())
            {
                ReleaseRunnerLock();
                await DisposeConnectionsAsync();
                lock (_sync)
                {
                    if (!_isRunning)
                    {
                        UpdateStateLocked("Paused", "runner-state-paused", "Runner paused. Resume to request more prompts.");
                    }
                }

                await Task.Delay(IdleDelay, stoppingToken);
                continue;
            }

            if (!TryAcquireRunnerLock())
            {
                await DisposeConnectionsAsync();
                await Task.Delay(ConnectionRetryDelay, stoppingToken);
                continue;
            }

            try
            {
                if (!await EnsureConnectionsAsync(stoppingToken))
                {
                    await Task.Delay(ConnectionRetryDelay, stoppingToken);
                    continue;
                }

                if (TryDequeuePendingItem(out var queuedItem))
                {
                    await ProcessQueueItemAsync(queuedItem, stoppingToken);
                    continue;
                }

                var promptBatch = await GeneratePromptBatchAsync(stoppingToken);
                if (promptBatch.Prompts.Count == 0)
                {
                    LogWarning("Prompt generation returned no valid prompts.");
                    await Task.Delay(ConnectionRetryDelay, stoppingToken);
                    continue;
                }

                EnqueuePromptBatch(promptBatch);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                MarkConnectionsDirty();
                lock (_sync)
                {
                    UpdateStateLocked("Retrying", "runner-state-error", $"Generation failed and will retry shortly: {ex.Message}");
                }
                LogError($"Generation loop error: {ex.Message}");
                await Task.Delay(ConnectionRetryDelay, stoppingToken);
            }
        }

        ReleaseRunnerLock();
        await DisposeConnectionsAsync();
    }

    private bool TryAcquireRunnerLock()
    {
        var dataRoot = ResolveDataRoot();
        var lockPath = Path.Combine(dataRoot, "staging", "dashboard-runtime.lock");

        lock (_sync)
        {
            if (_runnerLockHandle is not null && string.Equals(_runnerLockPath, lockPath, StringComparison.Ordinal))
                return true;
        }

        Directory.CreateDirectory(Path.GetDirectoryName(lockPath) ?? dataRoot);

        try
        {
            var lockHandle = new FileStream(lockPath, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);
            var lockContents = Encoding.UTF8.GetBytes($"pid={Environment.ProcessId}\nstartedUtc={DateTime.UtcNow:O}\n");
            lockHandle.Position = 0;
            lockHandle.SetLength(0);
            lockHandle.Write(lockContents, 0, lockContents.Length);
            lockHandle.Flush(true);

            lock (_sync)
            {
                _runnerLockHandle = lockHandle;
                _runnerLockPath = lockPath;
                _lastConnectionIssue = null;
            }

            return true;
        }
        catch (IOException)
        {
            const string message = "This dashboard is monitor-only because another dashboard server process already owns live generation. Browser tabs do not start extra runners.";

            lock (_sync)
            {
                UpdateStateLocked("Waiting", "runner-state-waiting", message);
                if (!string.Equals(_lastConnectionIssue, message, StringComparison.Ordinal))
                {
                    AppendLogLocked("warning", message);
                    _lastConnectionIssue = message;
                }
            }

            return false;
        }
    }

    private void ReleaseRunnerLock()
    {
        FileStream? lockHandle;

        lock (_sync)
        {
            lockHandle = _runnerLockHandle;
            _runnerLockHandle = null;
            _runnerLockPath = null;
        }

        if (lockHandle is null)
            return;

        try
        {
            lockHandle.Dispose();
        }
        catch
        {
            // Ignore lock cleanup failures during shutdown.
        }
    }

    private async Task<bool> EnsureConnectionsAsync(CancellationToken cancellationToken)
    {
        var dataRoot = ResolveDataRoot();
        var settings = OrchestrationSettingsStore.Load(dataRoot);
        var settingsSignature = BuildSettingsSignature(dataRoot, settings);

        if (_ollamaPool is not null && _comfyPool is not null && string.Equals(settingsSignature, _settingsSignature, StringComparison.Ordinal))
            return true;

        await DisposeConnectionsAsync();

        var activeOllamaNodes = await LoadActiveOllamaNodesAsync(settings, cancellationToken);
        if (activeOllamaNodes.Count == 0)
        {
            const string message = "No enabled Ollama nodes are currently usable.";

            lock (_sync)
            {
                UpdateStateLocked("Waiting", "runner-state-waiting", message);
                if (!string.Equals(_lastConnectionIssue, message, StringComparison.Ordinal))
                {
                    AppendLogLocked("warning", message);
                    _lastConnectionIssue = message;
                }
            }

            return false;
        }

        var activeComfyNodes = await LoadActiveComfyUiNodesAsync(settings, cancellationToken);
        if (activeComfyNodes.Count == 0)
        {
            foreach (var node in activeOllamaNodes)
            {
                node.Dispose();
            }

            const string message = "No enabled ComfyUI nodes are currently usable.";

            lock (_sync)
            {
                UpdateStateLocked("Waiting", "runner-state-waiting", message);
                if (!string.Equals(_lastConnectionIssue, message, StringComparison.Ordinal))
                {
                    AppendLogLocked("warning", message);
                    _lastConnectionIssue = message;
                }
            }

            return false;
        }

        _ollamaPool = new RoundRobinSelector<OllamaRunnerNode>(activeOllamaNodes);
        _comfyPool = new RoundRobinSelector<ComfyRunnerNode>(activeComfyNodes);
        _settings = settings;
        _settingsSignature = settingsSignature;
        _dataRoot = dataRoot;
        _checkingRoot = Path.Combine(dataRoot, "Checking");
        Directory.CreateDirectory(_checkingRoot);

        lock (_sync)
        {
            _lastConnectionIssue = null;
            UpdateStateLocked("Running", "runner-state-running", $"Connected to {activeOllamaNodes.Count} Ollama node(s) and {activeComfyNodes.Count} ComfyUI node(s).");
            AppendLogLocked("info", $"Live runner ready with {activeOllamaNodes.Count} Ollama node(s) and {activeComfyNodes.Count} ComfyUI node(s).");
        }

        return true;
    }

    private async Task<PromptBatchResult> GeneratePromptBatchAsync(CancellationToken cancellationToken)
    {
        if (_ollamaPool is null)
            throw new InvalidOperationException("No Ollama pool is available.");

        for (var attempt = 1; attempt <= 3; attempt++)
        {
            var node = _ollamaPool.Next();
            SetRunnerStatus($"Generating prompts on {node.Node.Name}.");

            try
            {
                var responseBuilder = new StringBuilder();
                await foreach (var chunk in node.Client.GenerateStreamAsync(_settings.PromptModel, PromptTemplate, cancellationToken))
                {
                    responseBuilder.Append(chunk);
                }

                var prompts = TryParsePromptBatch(responseBuilder.ToString());
                if (prompts.Count > 0)
                    return new PromptBatchResult(prompts, node.Node.Name);

                LogWarning($"Prompt batch attempt {attempt} on {node.Node.Name} returned invalid JSON.");
            }
            catch (Exception ex)
            {
                MarkConnectionsDirty();
                LogWarning($"Prompt batch attempt {attempt} on {node.Node.Name} failed: {ex.Message}");
            }
        }

        throw new InvalidOperationException("Prompt generation failed after 3 attempts.");
    }

    private void EnqueuePromptBatch(PromptBatchResult batch)
    {
        lock (_sync)
        {
            foreach (var prompt in batch.Prompts)
            {
                var item = new RuntimeQueueItem(prompt)
                {
                    PromptNodeName = batch.NodeName,
                    StageKey = "queued",
                    StageLabel = "Queued",
                    StageCssClass = "runtime-stage-queued",
                    StatusMessage = "Prompt generated and waiting for image generation."
                };

                _pendingWorkItems.Enqueue(item);
                _items.Insert(0, item);
                AppendLogLocked("info", $"Prompt generated for queue item {item.RuntimeId}: {item.PromptPreview}");
            }

            _sessionPromptCount += batch.Prompts.Count;
            UpdateStateLocked(
                "Running",
                "runner-state-running",
                $"{_pendingWorkItems.Count} queued item(s) ready for image generation.");
            TrimTerminalItemsLocked();
        }
    }

    private bool TryDequeuePendingItem(out RuntimeQueueItem item)
    {
        lock (_sync)
        {
            if (_pendingWorkItems.Count == 0)
            {
                item = null!;
                return false;
            }

            item = _pendingWorkItems.Dequeue();
            return true;
        }
    }

    private async Task ProcessQueueItemAsync(RuntimeQueueItem item, CancellationToken cancellationToken)
    {
        try
        {
            var imagePath = await GenerateImageAsync(item, cancellationToken);
            await GenerateSuitabilityAsync(item, imagePath, cancellationToken);

            lock (_sync)
            {
                item.StageKey = "completed";
                item.StageLabel = "Completed";
                item.StageCssClass = "runtime-stage-completed";
                item.StatusMessage = "Image saved and suitability recorded.";
                item.ProgressPercent = 100;
                item.UpdatedAtUtc = DateTime.UtcNow;
                _lastUpdatedUtc = item.UpdatedAtUtc;
                TrimTerminalItemsLocked();
            }

            LogInfo($"Queue item {item.DisplayId} completed.");
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            MarkConnectionsDirty();

            lock (_sync)
            {
                item.StageKey = "failed";
                item.StageLabel = "Failed";
                item.StageCssClass = "runtime-stage-failed";
                item.StatusMessage = "Generation failed.";
                item.ErrorMessage = ex.Message;
                item.ProgressPercent ??= 0;
                item.UpdatedAtUtc = DateTime.UtcNow;
                _sessionFailureCount++;
                _lastUpdatedUtc = item.UpdatedAtUtc;
                TrimTerminalItemsLocked();
            }

            LogError($"Queue item {item.DisplayId} failed: {ex.Message}");
        }
    }

    private async Task<string> GenerateImageAsync(RuntimeQueueItem item, CancellationToken cancellationToken)
    {
        if (_comfyPool is null)
            throw new InvalidOperationException("No ComfyUI pool is available.");

        var comfyNode = _comfyPool.Next();
        var workflowPath = Path.Combine(_dataRoot, "workloads", "illustration_lora_base.json");
        if (!File.Exists(workflowPath))
            throw new FileNotFoundException("The ComfyUI workflow file could not be found.", workflowPath);

        var workflow = BuildWorkflow(workflowPath, item.Prompt);
        var jobId = comfyNode.Client.QueuePrompt(workflow);
        var jobDirectory = Path.Combine(_checkingRoot, DateTime.UtcNow.ToString("yyyy-MM"), DateTime.UtcNow.ToString("dd"), jobId);
        Directory.CreateDirectory(jobDirectory);
        await File.WriteAllTextAsync(Path.Combine(jobDirectory, "phase_1.json"), item.Prompt.ToPrettyJsonString(), cancellationToken);

        lock (_sync)
        {
            item.ComfyJobId = jobId;
            item.ImageNodeName = comfyNode.Node.Name;
            item.CurrentNodeName = comfyNode.Node.Name;
            item.StageKey = "generating-image";
            item.StageLabel = "Generating image";
            item.StageCssClass = "runtime-stage-generating";
            item.StatusMessage = "Image generation started.";
            item.ProgressPercent = 0;
            item.UpdatedAtUtc = DateTime.UtcNow;
            _lastUpdatedUtc = item.UpdatedAtUtc;
            UpdateStateLocked("Running", "runner-state-running", $"Creating image {jobId} on {comfyNode.Node.Name}.");
        }

        LogInfo($"Image job {jobId} queued on {comfyNode.Node.Name}.");

        var timeoutAtUtc = DateTime.UtcNow.Add(JobTimeout);
        while (DateTime.UtcNow < timeoutAtUtc)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var job = comfyNode.Client.GetJob(jobId);
            var progress = Math.Clamp(job.Progress, 0, 100);
            var currentNodeName = string.IsNullOrWhiteSpace(job.CurrentNode) ? comfyNode.Node.Name : job.CurrentNode;

            lock (_sync)
            {
                item.CurrentNodeName = currentNodeName;
                item.ProgressPercent = progress;
                item.StatusMessage = progress <= 0
                    ? "Waiting for ComfyUI to start rendering."
                    : $"Image being created {progress:0}%";
                item.UpdatedAtUtc = DateTime.UtcNow;
                _lastUpdatedUtc = item.UpdatedAtUtc;
            }

            var progressBucket = (int)Math.Floor(progress / 10.0);
            if (progressBucket > item.LastLoggedProgressBucket && progress > 0)
            {
                item.LastLoggedProgressBucket = progressBucket;
                LogInfo($"Image job {jobId} on {comfyNode.Node.Name}: {progress:0}%");
            }

            if (string.Equals(job.Status, "completed", StringComparison.OrdinalIgnoreCase))
            {
                var imagePath = await job.DownloadAllImagesAsync(jobDirectory);
                if (string.IsNullOrWhiteSpace(imagePath))
                    throw new InvalidOperationException($"Image job {jobId} completed but no image was downloaded.");

                lock (_sync)
                {
                    item.ImagePath = Path.GetFullPath(imagePath);
                    item.ImageUrl = BuildGeneratedUrl(_checkingRoot, item.ImagePath);
                    item.StageKey = "image-saved";
                    item.StageLabel = "Image saved";
                    item.StageCssClass = "runtime-stage-saved";
                    item.StatusMessage = "Image saved to the checking queue.";
                    item.ProgressPercent = 100;
                    item.UpdatedAtUtc = DateTime.UtcNow;
                    _sessionImageCount++;
                    _lastUpdatedUtc = item.UpdatedAtUtc;
                }

                LogInfo($"Image saved for queue item {item.DisplayId}: {item.ImagePath}");
                return item.ImagePath;
            }

            if (string.Equals(job.Status, "failed", StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException($"ComfyUI job {jobId} failed on {comfyNode.Node.Name}.");

            await Task.Delay(JobPollDelay, cancellationToken);
        }

        throw new TimeoutException($"ComfyUI job {jobId} did not finish within {JobTimeout.TotalMinutes:0} minutes.");
    }

    private async Task GenerateSuitabilityAsync(RuntimeQueueItem item, string imagePath, CancellationToken cancellationToken)
    {
        if (_ollamaPool is null)
            throw new InvalidOperationException("No Ollama pool is available.");

        ImageSuitability? suitability = null;
        string? nodeName = null;

        for (var attempt = 1; attempt <= 3; attempt++)
        {
            var node = _ollamaPool.Next();
            nodeName = node.Node.Name;

            lock (_sync)
            {
                item.SuitabilityNodeName = node.Node.Name;
                item.CurrentNodeName = node.Node.Name;
                item.StageKey = "suitability";
                item.StageLabel = "Calculating suitability";
                item.StageCssClass = "runtime-stage-suitability";
                item.StatusMessage = "Suitability being calculated.";
                item.ProgressPercent = null;
                item.UpdatedAtUtc = DateTime.UtcNow;
                _lastUpdatedUtc = item.UpdatedAtUtc;
                UpdateStateLocked("Running", "runner-state-running", $"Calculating suitability for {item.DisplayId} on {node.Node.Name}.");
            }

            LogInfo($"Calculating suitability for {item.DisplayId} on {node.Node.Name}.");

            try
            {
                var responseBuilder = new StringBuilder();
                await foreach (var chunk in node.Client.GenerateWithImageStreamAsync(_settings.SuitabilityModel, SuitabilityPrompt, imagePath, cancellationToken))
                {
                    responseBuilder.Append(chunk);
                }

                suitability = TryParseSuitability(responseBuilder.ToString());
                if (suitability is not null)
                    break;

                LogWarning($"Suitability attempt {attempt} on {node.Node.Name} returned invalid JSON.");
            }
            catch (Exception ex)
            {
                MarkConnectionsDirty();
                LogWarning($"Suitability attempt {attempt} on {node.Node.Name} failed: {ex.Message}");
            }
        }

        if (suitability is null)
            throw new InvalidOperationException("Suitability generation failed after 3 attempts.");

        suitability.imageURL = new Uri(Path.GetFullPath(imagePath)).AbsoluteUri;
        var outputPath = Path.Combine(Path.GetDirectoryName(Path.GetFullPath(imagePath)) ?? _checkingRoot, "phase_3.json");
        await File.WriteAllTextAsync(outputPath, JsonSerializer.Serialize(suitability, PrettyJsonOptions), cancellationToken);

        lock (_sync)
        {
            item.StatusMessage = "Suitability saved.";
            item.UpdatedAtUtc = DateTime.UtcNow;
            item.ProgressPercent = 100;
            _sessionSuitabilityCount++;
            _lastUpdatedUtc = item.UpdatedAtUtc;
        }

        LogInfo($"Suitability saved for queue item {item.DisplayId}: {outputPath}");
    }

    private static JsonNode BuildWorkflow(string workflowPath, Prompt prompt)
    {
        var workflow = JsonXPath.Load(workflowPath);
        workflow = JsonXPath.Set(workflow, "//76:6/inputs/text", JsonValue.Create(prompt.positive)!);
        workflow = JsonXPath.Set(workflow, "//76:7/inputs/text", JsonValue.Create(prompt.negative)!);
        workflow = JsonXPath.Set(workflow, "//76:58/inputs/width", JsonValue.Create(prompt.width)!);
        workflow = JsonXPath.Set(workflow, "//76:58/inputs/height", JsonValue.Create(prompt.height)!);
        workflow = JsonXPath.Set(workflow, "//76:3/inputs/steps", JsonValue.Create(prompt.steps)!);
        workflow = JsonXPath.Set(workflow, "//76:3/inputs/cfg", JsonValue.Create(prompt.cfg)!);
        return workflow;
    }

    private async Task<List<OllamaRunnerNode>> LoadActiveOllamaNodesAsync(OrchestrationSettings settings, CancellationToken cancellationToken)
    {
        var activeNodes = new List<OllamaRunnerNode>();
        var requiredModels = new[] { settings.PromptModel, settings.SuitabilityModel }
            .Where(model => !string.IsNullOrWhiteSpace(model))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        foreach (var node in settings.Ollama.Where(candidate => candidate.Enabled && !string.IsNullOrWhiteSpace(candidate.BaseUrl)))
        {
            try
            {
                var client = new OllamaClient(node.BaseUrl);
                await client.CheckStatusAsync();
                var installedModels = await client.GetInstalledModelNamesAsync(cancellationToken);
                cancellationToken.ThrowIfCancellationRequested();

                var missingModels = requiredModels
                    .Where(model => !installedModels.Contains(model))
                    .ToArray();

                if (missingModels.Length > 0)
                {
                    client.Dispose();
                    LogWarning($"Skipping Ollama node {node.Name} ({node.BaseUrl}): missing model(s): {string.Join(", ", missingModels)}");
                    continue;
                }

                activeNodes.Add(new OllamaRunnerNode(node, client));
            }
            catch (Exception ex)
            {
                LogWarning($"Skipping Ollama node {node.Name} ({node.BaseUrl}): {ex.Message}");
            }
        }

        return activeNodes;
    }

    private async Task<List<ComfyRunnerNode>> LoadActiveComfyUiNodesAsync(OrchestrationSettings settings, CancellationToken cancellationToken)
    {
        var activeNodes = new List<ComfyRunnerNode>();
        foreach (var node in settings.ComfyUi.Where(candidate => candidate.Enabled && !string.IsNullOrWhiteSpace(candidate.BaseUrl)))
        {
            ComfyUiClient? client = null;
            try
            {
                client = new ComfyUiClient(node.BaseUrl, new WebSocketEventEmitter());
                await client.StartListener();
                cancellationToken.ThrowIfCancellationRequested();
                activeNodes.Add(new ComfyRunnerNode(node, client));
            }
            catch (Exception ex)
            {
                if (client is not null)
                    await client.DisposeAsync();

                LogWarning($"Skipping ComfyUI node {node.Name} ({node.BaseUrl}): {ex.Message}");
            }
        }

        return activeNodes;
    }

    private async Task DisposeConnectionsAsync()
    {
        RoundRobinSelector<OllamaRunnerNode>? ollamaPool;
        RoundRobinSelector<ComfyRunnerNode>? comfyPool;

        lock (_sync)
        {
            ollamaPool = _ollamaPool;
            comfyPool = _comfyPool;
            _ollamaPool = null;
            _comfyPool = null;
            _settingsSignature = string.Empty;
        }

        if (comfyPool is not null)
        {
            foreach (var node in comfyPool.Items)
            {
                await node.DisposeAsync();
            }
        }

        if (ollamaPool is not null)
        {
            foreach (var node in ollamaPool.Items)
            {
                node.Dispose();
            }
        }
    }

    private string ResolveDataRoot()
    {
        return DashboardOptions.ResolveDataRoot(_options.CurrentValue.DataRoot, _environment.ContentRootPath);
    }

    private static string BuildSettingsSignature(string dataRoot, OrchestrationSettings settings)
    {
        var enabledOllama = settings.Ollama
            .Where(node => node.Enabled)
            .Select(node => $"{node.Id}:{node.BaseUrl}");
        var enabledComfy = settings.ComfyUi
            .Where(node => node.Enabled)
            .Select(node => $"{node.Id}:{node.BaseUrl}");

        return string.Join('|', new[]
        {
            dataRoot,
            settings.PromptModel,
            settings.SuitabilityModel,
            string.Join(';', enabledOllama),
            string.Join(';', enabledComfy)
        });
    }

    private void MarkConnectionsDirty()
    {
        lock (_sync)
        {
            _settingsSignature = string.Empty;
            _lastConnectionIssue = null;
        }
    }

    private bool IsRunning()
    {
        lock (_sync)
        {
            return _isRunning;
        }
    }

    private void SetRunnerStatus(string message)
    {
        lock (_sync)
        {
            UpdateStateLocked("Running", "runner-state-running", message);
        }
    }

    private void LogInfo(string message)
    {
        _logger.LogInformation(message);
        lock (_sync)
        {
            AppendLogLocked("info", message);
        }
    }

    private void LogWarning(string message)
    {
        _logger.LogWarning(message);
        lock (_sync)
        {
            AppendLogLocked("warning", message);
        }
    }

    private void LogError(string message)
    {
        _logger.LogError(message);
        lock (_sync)
        {
            AppendLogLocked("error", message);
        }
    }

    private void UpdateStateLocked(string stateLabel, string stateCssClass, string message)
    {
        _stateLabel = stateLabel;
        _stateCssClass = stateCssClass;
        _statusMessage = message;
        _lastUpdatedUtc = DateTime.UtcNow;
    }

    private void AppendLogLocked(string level, string message)
    {
        _logs.Add(new GenerationLogEntry(
            TimestampUtc: DateTime.UtcNow,
            Level: level,
            CssClass: level switch
            {
                "error" => "runtime-log-error",
                "warning" => "runtime-log-warning",
                _ => "runtime-log-info"
            },
            Message: message));

        while (_logs.Count > MaxRetainedLogEntries)
        {
            _logs.RemoveAt(0);
        }

        _lastUpdatedUtc = DateTime.UtcNow;
    }

    private void TrimTerminalItemsLocked()
    {
        while (_items.Count(item => item.IsTerminal) > MaxRetainedTerminalItems)
        {
            var oldestTerminal = _items
                .Where(item => item.IsTerminal)
                .OrderBy(item => item.UpdatedAtUtc)
                .FirstOrDefault();

            if (oldestTerminal is null)
                break;

            _items.Remove(oldestTerminal);
        }
    }

    private static IReadOnlyList<Prompt> TryParsePromptBatch(string rawResponse)
    {
        var json = ExtractJsonArray(rawResponse);
        if (string.IsNullOrWhiteSpace(json))
            return Array.Empty<Prompt>();

        try
        {
            return (JsonSerializer.Deserialize<List<Prompt>>(json) ?? new List<Prompt>())
                .Where(prompt => prompt.isValid())
                .Take(2)
                .ToList();
        }
        catch
        {
            return Array.Empty<Prompt>();
        }
    }

    private static ImageSuitability? TryParseSuitability(string rawResponse)
    {
        var json = ExtractJsonObject(rawResponse);
        if (string.IsNullOrWhiteSpace(json))
            return null;

        try
        {
            var suitability = JsonSerializer.Deserialize<ImageSuitability>(json);
            return suitability?.isValid() == true ? suitability : null;
        }
        catch
        {
            return null;
        }
    }

    private static string? ExtractJsonArray(string rawResponse)
    {
        if (string.IsNullOrWhiteSpace(rawResponse))
            return null;

        var start = rawResponse.IndexOf('[');
        if (start < 0)
            return null;

        var end = rawResponse.LastIndexOf(']');
        return end >= start
            ? rawResponse[start..(end + 1)]
            : rawResponse[start..] + "]";
    }

    private static string? ExtractJsonObject(string rawResponse)
    {
        if (string.IsNullOrWhiteSpace(rawResponse))
            return null;

        var start = rawResponse.IndexOf('{');
        var end = rawResponse.LastIndexOf('}');
        if (start < 0 || end < start)
            return null;

        return rawResponse[start..(end + 1)];
    }

    private static string PreviewPrompt(string? prompt)
    {
        if (string.IsNullOrWhiteSpace(prompt))
            return "No prompt text captured yet.";

        var compact = prompt.Trim();
        return compact.Length <= 180 ? compact : compact[..177] + "...";
    }

    private static string BuildPromptSettingsLabel(Prompt prompt)
    {
        return $"{prompt.width} x {prompt.height} · {prompt.steps} steps · cfg {prompt.cfg:0.#}";
    }

    private static string? BuildGeneratedUrl(string checkingRoot, string imagePath)
    {
        var normalizedCheckingRoot = Path.GetFullPath(checkingRoot);
        var normalizedImagePath = Path.GetFullPath(imagePath);
        var relativePath = Path.GetRelativePath(normalizedCheckingRoot, normalizedImagePath);
        if (relativePath.StartsWith("..", StringComparison.OrdinalIgnoreCase))
            return null;

        var encodedSegments = relativePath
            .Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
            .Where(segment => !string.IsNullOrWhiteSpace(segment))
            .Select(Uri.EscapeDataString);

        return "/generated/" + string.Join('/', encodedSegments);
    }

    private sealed class RuntimeQueueItem
    {
        public RuntimeQueueItem(Prompt prompt)
        {
            Prompt = prompt;
            PromptPreview = PreviewPrompt(prompt.positive);
            PromptSettingsLabel = BuildPromptSettingsLabel(prompt);
        }

        public Prompt Prompt { get; }
        public string RuntimeId { get; } = Guid.NewGuid().ToString("N")[..8];
        public string PromptPreview { get; }
        public string PromptSettingsLabel { get; }
        public string StageKey { get; set; } = "queued";
        public string StageLabel { get; set; } = "Queued";
        public string StageCssClass { get; set; } = "runtime-stage-queued";
        public string StatusMessage { get; set; } = "Prompt generated and waiting for image generation.";
        public double? ProgressPercent { get; set; }
        public string? PromptNodeName { get; set; }
        public string? ImageNodeName { get; set; }
        public string? SuitabilityNodeName { get; set; }
        public string? CurrentNodeName { get; set; }
        public string? ComfyJobId { get; set; }
        public string? ImagePath { get; set; }
        public string? ImageUrl { get; set; }
        public string? ErrorMessage { get; set; }
        public DateTime CreatedAtUtc { get; } = DateTime.UtcNow;
        public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;
        public int LastLoggedProgressBucket { get; set; } = -1;
        public bool IsTerminal => StageKey is "completed" or "failed";
        public string DisplayId => ComfyJobId ?? RuntimeId;

        public GenerationQueueItemSnapshot ToSnapshot()
        {
            return new GenerationQueueItemSnapshot(
                RuntimeId: RuntimeId,
                ComfyJobId: ComfyJobId,
                StageKey: StageKey,
                StageLabel: StageLabel,
                StageCssClass: StageCssClass,
                StatusMessage: StatusMessage,
                ProgressPercent: ProgressPercent,
                PromptPreview: PromptPreview,
                PromptSettingsLabel: PromptSettingsLabel,
                PromptNodeName: PromptNodeName,
                ImageNodeName: ImageNodeName,
                SuitabilityNodeName: SuitabilityNodeName,
                CurrentNodeName: CurrentNodeName,
                ImageUrl: ImageUrl,
                ImagePath: ImagePath,
                ErrorMessage: ErrorMessage,
                CreatedAtUtc: CreatedAtUtc,
                UpdatedAtUtc: UpdatedAtUtc);
        }
    }

    private sealed record PromptBatchResult(IReadOnlyList<Prompt> Prompts, string NodeName);

    private sealed class OllamaRunnerNode : IDisposable
    {
        public OllamaRunnerNode(OrchestrationNode node, OllamaClient client)
        {
            Node = node;
            Client = client;
        }

        public OrchestrationNode Node { get; }
        public OllamaClient Client { get; }

        public void Dispose()
        {
            Client.Dispose();
        }
    }

    private sealed class ComfyRunnerNode : IAsyncDisposable
    {
        public ComfyRunnerNode(OrchestrationNode node, ComfyUiClient client)
        {
            Node = node;
            Client = client;
        }

        public OrchestrationNode Node { get; }
        public ComfyUiClient Client { get; }

        public async ValueTask DisposeAsync()
        {
            await Client.DisposeAsync();
        }
    }
}

public sealed record GenerationRuntimeSnapshot(
    bool IsRunning,
    string StateLabel,
    string StateCssClass,
    string StatusMessage,
    int PendingQueueCount,
    int ActiveItemCount,
    int SessionPromptCount,
    int SessionImageCount,
    int SessionSuitabilityCount,
    int SessionFailureCount,
    DateTime LastUpdatedUtc,
    IReadOnlyList<GenerationQueueItemSnapshot> Items,
    IReadOnlyList<GenerationLogEntry> Logs)
{
    public static GenerationRuntimeSnapshot Empty { get; } = new(
        IsRunning: true,
        StateLabel: "Running",
        StateCssClass: "runner-state-running",
        StatusMessage: "Preparing the first prompt batch.",
        PendingQueueCount: 0,
        ActiveItemCount: 0,
        SessionPromptCount: 0,
        SessionImageCount: 0,
        SessionSuitabilityCount: 0,
        SessionFailureCount: 0,
        LastUpdatedUtc: DateTime.UtcNow,
        Items: Array.Empty<GenerationQueueItemSnapshot>(),
        Logs: Array.Empty<GenerationLogEntry>());
}

public sealed record GenerationQueueItemSnapshot(
    string RuntimeId,
    string? ComfyJobId,
    string StageKey,
    string StageLabel,
    string StageCssClass,
    string StatusMessage,
    double? ProgressPercent,
    string PromptPreview,
    string PromptSettingsLabel,
    string? PromptNodeName,
    string? ImageNodeName,
    string? SuitabilityNodeName,
    string? CurrentNodeName,
    string? ImageUrl,
    string? ImagePath,
    string? ErrorMessage,
    DateTime CreatedAtUtc,
    DateTime UpdatedAtUtc)
{
    public string DisplayId => ComfyJobId ?? RuntimeId;
}

public sealed record GenerationLogEntry(
    DateTime TimestampUtc,
    string Level,
    string CssClass,
    string Message);