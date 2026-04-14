using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using PrintifyGenerator.Dashboard.Services;

namespace PrintifyGenerator.Dashboard.Pages;

public sealed class GeneratedImagesModel : PageModel
{
    private readonly DashboardDataService _dashboardDataService;

    public GeneratedImagesModel(DashboardDataService dashboardDataService)
    {
        _dashboardDataService = dashboardDataService;
    }

    [BindProperty(SupportsGet = true, Name = "status")]
    public string Status { get; set; } = GalleryStatusFilter.All;

    [BindProperty(SupportsGet = true, Name = "focusImagePath")]
    public string? FocusImagePath { get; set; }

    [TempData]
    public string? FlashMessage { get; set; }

    public DashboardSnapshot Snapshot { get; private set; } = DashboardSnapshot.Empty;

    public bool HasFocusedImage => !string.IsNullOrWhiteSpace(FocusImagePath);

    public void OnGet()
    {
        Status = GalleryStatusFilter.Normalize(Status);
        Snapshot = _dashboardDataService.LoadSnapshot(Status, focusedImagePath: FocusImagePath);
    }

    public IActionResult OnPostSetOverride(string imagePath, string mode, string? status, string? focusImagePath)
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

        return RedirectToPage(new
        {
            status = GalleryStatusFilter.Normalize(status),
            focusImagePath = NormalizeOptionalRouteValue(focusImagePath)
        });
    }

    private static string BuildOverrideMessage(string mode)
    {
        return PublishingOverrideModes.Normalize(mode) switch
        {
            PublishingOverrideModes.ForceAllow => "Manual allow saved. This image can pass the minimum score gate if safety checks are clear.",
            PublishingOverrideModes.ForceBlock => "Manual block saved. This image is now excluded from the publish queue.",
            _ => "Publish override cleared. Automatic scoring rules apply again."
        };
    }

    private static string? NormalizeOptionalRouteValue(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }
}