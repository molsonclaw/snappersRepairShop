using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace SnappersRepairShop.Areas.Identity.Pages.Account;

[AllowAnonymous]
public class RegisterModel : PageModel
{
    public IActionResult OnGet() => Page();

    public IActionResult OnPost() => RedirectToPage("Login");
}
