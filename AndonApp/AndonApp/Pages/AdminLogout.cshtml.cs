using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace AndonApp.Pages;

public class AdminLogoutModel : PageModel
{
    // GET: visiting the URL directly just redirects to login without signing out.
    // Actual logout requires a POST with a valid antiforgery token.
    public IActionResult OnGet() => Redirect("/admin/login");

    [ValidateAntiForgeryToken]
    public async Task<IActionResult> OnPostAsync()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return Redirect("/admin/login");
    }
}
