using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using PrintifyGenerator.Dashboard.Services;

namespace PrintifyGenerator.Dashboard.Pages;

public sealed class QcQueueModel : PageModel
{
    private readonly DashboardDataService _dashboardDataService;

    public QcQueueModel(DashboardDataService dashboardDataService)
    {
        _dashboardDataService = dashboardDataService;
    }

    [BindProperty(SupportsGet = true, Name = "status")]
    public string Status { get; set; } = GalleryStatusFilter.All;

    [TempData]
    public string? FlashMessage { get; set; }

    public DashboardSnapshot Snapshot { get; private set; } = DashboardSnapshot.Empty;

    public void OnGet()
    {
        Status = GalleryStatusFilter.Normalize(Status);
        Snapshot = _dashboardDataService.LoadSnapshot(Status, itemLimitOverride: 0);
    }

    public IActionResult OnPostSetOverride(string imagePath, string mode, string? status)
    {
        var normalizedStatus = GalleryStatusFilter.Normalize(status);

        try
        {
            _dashboardDataService.SetPublishingOverride(imagePath, mode);
            FlashMessage = BuildOverrideMessage(mode);
        }
        catch (Exception ex)
        {
            FlashMessage = ex.Message;
        }

        if (WantsJsonResponse())
            return BuildOverrideJsonResult(imagePath, normalizedStatus, FlashMessage ?? string.Empty);

        return RedirectToPage(new { status = normalizedStatus });
    }

    public static string GetQcLabel(GalleryItem item)
    {
        if (!item.HasSuitability)
            return "Pending QC";

        return item.IsEligibleForPublishing ? "Passes QC" : "Fails QC";
    }

    public static string GetQcCssClass(GalleryItem item)
    {
        if (!item.HasSuitability)
            return "qc-pending";

        return item.IsEligibleForPublishing ? "qc-pass" : "qc-fail";
    }

    public static string GetOverrideNote(GalleryItem item)
    {
        return item.PublishOverrideUpdatedAtUtc is DateTime updatedAtUtc
            ? $"{item.PublishOverrideLabel} set {updatedAtUtc.ToLocalTime():dd MMM yyyy HH:mm}"
            : "No manual override set.";
    }

    private static string BuildOverrideMessage(string mode)
    {
        return PublishingOverrideModes.Normalize(mode) switch
        {
            PublishingOverrideModes.ForceAllow => "Allow override saved. This image can continue if safety checks are clear.",
            PublishingOverrideModes.ForceBlock => "Block override saved. This image will not continue.",
            _ => "Manual override cleared. Automatic QC rules apply again."
        };
    }

    private JsonResult BuildOverrideJsonResult(string imagePath, string status, string flashMessage)
    {
        var normalizedImagePath = Path.GetFullPath(imagePath);
        var allSnapshot = _dashboardDataService.LoadSnapshot(GalleryStatusFilter.All, itemLimitOverride: 0);
        var filteredSnapshot = string.Equals(status, GalleryStatusFilter.All, StringComparison.OrdinalIgnoreCase)
            ? allSnapshot
            : _dashboardDataService.LoadSnapshot(status, itemLimitOverride: 0);

        var item = allSnapshot.Images.FirstOrDefault(candidate => string.Equals(candidate.ImagePath, normalizedImagePath, StringComparison.Ordinal));
        var matchesCurrentFilter = filteredSnapshot.Images.Any(candidate => string.Equals(candidate.ImagePath, normalizedImagePath, StringComparison.Ordinal));

        return new JsonResult(new
        {
            success = item is not null,
            flashMessage,
            visibleCount = filteredSnapshot.Images.Count,
            item = item is null
                ? null
                : new
                {
                    statusLabel = item.StatusLabel,
                    statusCssClass = item.StatusCssClass,
                    qcLabel = GetQcLabel(item),
                    qcCssClass = GetQcCssClass(item),
                    eligibilityReason = item.EligibilityReason,
                    publishOverrideMode = item.PublishOverrideMode,
                    publishOverrideLabel = item.PublishOverrideLabel,
                    overrideNote = GetOverrideNote(item),
                    canForceAllow = item.CanForceAllow,
                    matchesCurrentFilter
                }
        });
    }

    private bool WantsJsonResponse()
    {
        var requestedWith = Request.Headers["X-Requested-With"].ToString();
        var accept = Request.Headers.Accept.ToString();

        return string.Equals(requestedWith, "fetch", StringComparison.OrdinalIgnoreCase)
            || accept.Contains("application/json", StringComparison.OrdinalIgnoreCase);
    }
}