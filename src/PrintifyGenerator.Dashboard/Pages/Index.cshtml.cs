using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace PrintifyGenerator.Dashboard.Pages;

public sealed class IndexModel : PageModel
{
    public IActionResult OnGet()
    {
        return RedirectToPage("/Stats");
    }
}
