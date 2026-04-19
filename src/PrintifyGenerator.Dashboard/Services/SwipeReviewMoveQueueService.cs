using System.Text.Json;
using Microsoft.Extensions.Options;
using PrintifyGenerator.Dashboard.Models;

namespace PrintifyGenerator.Dashboard.Services;

public sealed class SwipeReviewMoveQueueService : BackgroundService
{
    private static readonly JsonSerializerOptions LogJsonOptions = new(JsonSerializerDefaults.Web);

    private readonly object _sync = new();
    private readonly Queue<QueuedSwipeReviewMove> _pendingMoves = new();
    private readonly HashSet<string> _queuedProductIds = new(StringComparer.OrdinalIgnoreCase);
    private readonly SemaphoreSlim _signal = new(0);
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IWebHostEnvironment _environment;
    private readonly IOptionsMonitor<DashboardOptions> _options;
    private readonly ILogger<SwipeReviewMoveQueueService> _logger;

    public SwipeReviewMoveQueueService(
        IServiceScopeFactory scopeFactory,
        IWebHostEnvironment environment,
        IOptionsMonitor<DashboardOptions> options,
        ILogger<SwipeReviewMoveQueueService> logger)
    {
        _scopeFactory = scopeFactory;
        _environment = environment;
        _options = options;
        _logger = logger;
    }

    public bool TryEnqueue(string productId)
    {
        var normalizedProductId = NormalizeProductId(productId);
        if (normalizedProductId is null)
        {
            return false;
        }

        lock (_sync)
        {
            if (_queuedProductIds.Contains(normalizedProductId))
            {
                return false;
            }

            _queuedProductIds.Add(normalizedProductId);
            _pendingMoves.Enqueue(new QueuedSwipeReviewMove(normalizedProductId, DateTime.UtcNow));
        }

        _signal.Release();
        return true;
    }

    public bool IsQueued(string productId)
    {
        var normalizedProductId = NormalizeProductId(productId);
        if (normalizedProductId is null)
        {
            return false;
        }

        lock (_sync)
        {
            return _queuedProductIds.Contains(normalizedProductId);
        }
    }

    public HashSet<string> CreateQueuedProductIdSnapshot()
    {
        lock (_sync)
        {
            return new HashSet<string>(_queuedProductIds, StringComparer.OrdinalIgnoreCase);
        }
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await _signal.WaitAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }

            while (TryDequeue(out var queuedMove))
            {
                try
                {
                    using var scope = _scopeFactory.CreateScope();
                    var swipeReviewService = scope.ServiceProvider.GetRequiredService<StagingSwipeReviewService>();
                    var result = await swipeReviewService.ProcessQueuedPromoteAsync(queuedMove.ProductId, stoppingToken);

                    if (!result.Success)
                    {
                        _logger.LogWarning(
                            "Queued swipe review move for {ProductId} finished with warning: {Message}",
                            queuedMove.ProductId,
                            result.Message);
                        await AppendLogEntryAsync("warning", queuedMove, result.Message, result.TargetProductId, exception: null);
                    }
                    else
                    {
                        _logger.LogInformation(
                            "Queued swipe review move for {ProductId} completed as target {TargetProductId}.",
                            queuedMove.ProductId,
                            result.TargetProductId);
                    }
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Queued swipe review move failed for {ProductId}.", queuedMove.ProductId);
                    await AppendLogEntryAsync("error", queuedMove, ex.Message, targetProductId: null, ex);
                }
                finally
                {
                    Complete(queuedMove.ProductId);
                }
            }
        }
    }

    private bool TryDequeue(out QueuedSwipeReviewMove queuedMove)
    {
        lock (_sync)
        {
            if (_pendingMoves.Count == 0)
            {
                queuedMove = default;
                return false;
            }

            queuedMove = _pendingMoves.Dequeue();
            return true;
        }
    }

    private void Complete(string productId)
    {
        var normalizedProductId = NormalizeProductId(productId);
        if (normalizedProductId is null)
        {
            return;
        }

        lock (_sync)
        {
            _queuedProductIds.Remove(normalizedProductId);
        }
    }

    private async Task AppendLogEntryAsync(
        string level,
        QueuedSwipeReviewMove queuedMove,
        string message,
        string? targetProductId,
        Exception? exception)
    {
        var dataRoot = DashboardOptions.ResolveDataRoot(_options.CurrentValue.DataRoot, _environment.ContentRootPath);
        var logsRoot = Path.Combine(dataRoot, "staging", "logs");
        Directory.CreateDirectory(logsRoot);

        var logPath = Path.Combine(logsRoot, "swipe-review-move-errors.jsonl");
        var entry = new SwipeReviewMoveLogEntry(
            TimestampUtc: DateTime.UtcNow,
            Level: level,
            ProductId: queuedMove.ProductId,
            QueuedAtUtc: queuedMove.QueuedAtUtc,
            Message: message,
            TargetProductId: targetProductId,
            Exception: exception?.ToString());

        var payload = JsonSerializer.Serialize(entry, LogJsonOptions) + Environment.NewLine;
        await File.AppendAllTextAsync(logPath, payload, CancellationToken.None);
    }

    private static string? NormalizeProductId(string productId)
    {
        return string.IsNullOrWhiteSpace(productId) ? null : productId.Trim();
    }

    private readonly record struct QueuedSwipeReviewMove(
        string ProductId,
        DateTime QueuedAtUtc);

    private sealed record SwipeReviewMoveLogEntry(
        DateTime TimestampUtc,
        string Level,
        string ProductId,
        DateTime QueuedAtUtc,
        string Message,
        string? TargetProductId,
        string? Exception);
}