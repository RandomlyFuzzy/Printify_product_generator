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
        try
        {
            _dashboardDataService.SetPublishingOverride(imagePath, mode);
            FlashMessage = BuildOverrideMessage(mode);
        }
        catch (Exception ex)
        {
            FlashMessage = ex.Message;
        }

        return RedirectToPage(new { status = GalleryStatusFilter.Normalize(status) });
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

    private static string BuildOverrideMessage(string mode)
    {
        return PublishingOverrideModes.Normalize(mode) switch
        {
            PublishingOverrideModes.ForceAllow => "Allow override saved. This image can continue if safety checks are clear.",
            PublishingOverrideModes.ForceBlock => "Block override saved. This image will not continue.",
            _ => "Manual override cleared. Automatic QC rules apply again."
        };
    }
}