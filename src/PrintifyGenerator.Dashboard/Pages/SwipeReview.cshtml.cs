using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using PrintifyGenerator.Dashboard.Services;

namespace PrintifyGenerator.Dashboard.Pages;

public sealed class SwipeReviewModel : PageModel
{
    private readonly StagingSwipeReviewService _stagingSwipeReviewService;

    public SwipeReviewModel(StagingSwipeReviewService stagingSwipeReviewService)
    {
        _stagingSwipeReviewService = stagingSwipeReviewService;
    }

    public SwipeReviewSnapshot Snapshot { get; private set; } = SwipeReviewSnapshot.Empty;

    public SwipeReviewPageState PageState { get; private set; } = SwipeReviewPageState.Empty;

    public async Task OnGetAsync(CancellationToken cancellationToken)
    {
        ViewData["Title"] = "Swipe Review";
        Snapshot = await _stagingSwipeReviewService.LoadSnapshotAsync(cancellationToken);
        PageState = SwipeReviewPageState.FromSnapshot(Snapshot);
    }

    public async Task<IActionResult> OnPostDeleteAsync([FromForm] string productId, CancellationToken cancellationToken)
    {
        return new JsonResult(await ExecuteActionAsync(
            () => _stagingSwipeReviewService.DeleteAndLoadSnapshotAsync(productId, cancellationToken),
            cancellationToken));
    }

    public async Task<IActionResult> OnPostPromoteAsync([FromForm] string productId, CancellationToken cancellationToken)
    {
        return new JsonResult(await ExecuteActionAsync(
            () => _stagingSwipeReviewService.EnqueuePromoteAndLoadSnapshotAsync(productId, cancellationToken),
            cancellationToken));
    }

    private async Task<SwipeReviewInteractionResponse> ExecuteActionAsync(
        Func<Task<SwipeReviewActionExecution>> action,
        CancellationToken cancellationToken)
    {
        var level = "success";
        string? message;
        SwipeReviewSnapshot snapshot;

        try
        {
            var result = await action();
            snapshot = result.Snapshot;
            message = result.ActionResult.Message;
            level = result.ActionResult.Success ? "success" : "warning";
        }
        catch (Exception ex)
        {
            message = ex.Message;
            level = "error";
            snapshot = await _stagingSwipeReviewService.LoadSnapshotAsync(cancellationToken);
        }

        return new SwipeReviewInteractionResponse(snapshot, message, level);
    }
}

public sealed record SwipeReviewInteractionResponse(
    SwipeReviewSnapshot Snapshot,
    string? Message,
    string Level);

public sealed record SwipeReviewPageState(
    SwipeReviewItem? CurrentItem,
    SwipeReviewImage InitialImage,
    bool HasInitialImage,
    bool HideInitialImage,
    bool HideInitialImageEmptyState,
    string InitialCountLabel,
    string InitialGalleryLabel,
    string InitialImageCounter,
    IReadOnlyList<SwipeReviewImage> InitialSupplementalImages,
    bool HideInitialSupplementalPanel,
    string InitialSupplementalCountLabel,
    string InitialNote,
    string? InitialBackdropStyle,
    string InitialCardClass,
    string InitialImageFrameClass,
    string InitialEmptyStateClass,
    string InitialActionsClass,
    string BlueprintTitle,
    string PrintProviderTitle,
    string CurrentTitle,
    string CurrentDescription,
    string CurrentPriceLabel,
    string SuggestedPriceLabel,
    string MarginLabel,
    IReadOnlyList<string> VisibleTags,
    bool HasVisibleTags,
    bool CanDelete,
    bool CanPromote,
    bool DeleteDisabled,
    bool PromoteDisabled,
    string InitialSnapshotJson)
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public static SwipeReviewPageState Empty { get; } = FromSnapshot(SwipeReviewSnapshot.Empty);

    public static SwipeReviewPageState FromSnapshot(SwipeReviewSnapshot snapshot)
    {
        var current = snapshot.CurrentItem;
        var displayImages = current?.DisplayImages ?? Array.Empty<SwipeReviewImage>();
        var supplementalImages = current?.SupplementalImages ?? Array.Empty<SwipeReviewImage>();
        var gridImages = displayImages.Count > 0 ? displayImages : supplementalImages;
        var initialImage = current?.PrimaryImage ?? SwipeReviewImage.Empty;
        var hasInitialImage = !string.IsNullOrWhiteSpace(initialImage.Url);
        var initialImageCount = displayImages.Count;
        var initialCountLabel = current is null ? "No cards left" : $"{snapshot.QueueCount} left";
        var initialImageCounter = initialImageCount > 0 ? $"1 / {initialImageCount}" : "0 / 0";
        var initialGalleryLabel = current?.ProductImages.Count > 0 ? "Product images" : "Available images";
        var initialSupplementalCountLabel = gridImages.Count == 1
            ? "1 image"
            : $"{gridImages.Count} images";
        var initialNote = current?.PromoteUnavailableReason
            ?? snapshot.StatusMessage
            ?? (initialImageCount > 1
                ? "Tap a thumbnail or swipe up and down for more product images."
                : supplementalImages.Count > 0
                    ? "Reference images stay pinned on the side."
                : "Swipe left to delete or right to move to drafts.");

        return new SwipeReviewPageState(
            CurrentItem: current,
            InitialImage: initialImage,
            HasInitialImage: hasInitialImage,
            HideInitialImage: !hasInitialImage,
            HideInitialImageEmptyState: hasInitialImage,
            InitialCountLabel: initialCountLabel,
            InitialGalleryLabel: initialGalleryLabel,
            InitialImageCounter: initialImageCounter,
            InitialSupplementalImages: gridImages,
            HideInitialSupplementalPanel: gridImages.Count == 0,
            InitialSupplementalCountLabel: initialSupplementalCountLabel,
            InitialNote: initialNote,
            InitialBackdropStyle: hasInitialImage ? $"background-image:url('{initialImage.Url}');" : null,
            InitialCardClass: current is null ? "swipe-card is-hidden" : "swipe-card",
            InitialImageFrameClass: hasInitialImage ? "swipe-image-frame" : "swipe-image-frame is-empty",
            InitialEmptyStateClass: current is null ? "swipe-empty is-visible" : "swipe-empty",
            InitialActionsClass: current is null ? "swipe-actions is-hidden" : "swipe-actions",
            BlueprintTitle: current?.BlueprintTitle ?? string.Empty,
            PrintProviderTitle: current?.PrintProviderTitle ?? string.Empty,
            CurrentTitle: current?.CurrentTitle ?? string.Empty,
            CurrentDescription: current?.CurrentDescription ?? string.Empty,
            CurrentPriceLabel: current?.CurrentPriceLabel ?? string.Empty,
            SuggestedPriceLabel: current?.SuggestedPriceLabel ?? string.Empty,
            MarginLabel: current?.MarginLabel ?? string.Empty,
            VisibleTags: current?.Tags.Take(4).ToArray() ?? Array.Empty<string>(),
            HasVisibleTags: current?.Tags.Count > 0,
            CanDelete: current?.CanDelete == true,
            CanPromote: current?.CanPromote == true,
            DeleteDisabled: current?.CanDelete != true,
            PromoteDisabled: current?.CanPromote != true,
            InitialSnapshotJson: JsonSerializer.Serialize(snapshot, JsonOptions));
    }
}