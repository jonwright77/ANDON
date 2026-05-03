using AndonApp.Data;
using AndonApp.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace AndonApp.Pages;

public class AdminLoginModel : PageModel
{
    private readonly AndonDbContext _db;
    private readonly LoginAttemptTracker _tracker;
    private readonly ILogger<AdminLoginModel> _logger;

    public AdminLoginModel(AndonDbContext db, LoginAttemptTracker tracker, ILogger<AdminLoginModel> logger)
    {
        _db = db;
        _tracker = tracker;
        _logger = logger;
    }

    public string? Error { get; private set; }
    public string Username { get; private set; } = string.Empty;

    public IActionResult OnGet()
    {
        if (User.Identity?.IsAuthenticated == true)
            return Redirect("/admin/andon-codes");
        return Page();
    }

    public async Task<IActionResult> OnPostAsync(string username, string password)
    {
        Username = username ?? string.Empty;
        var ip = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";

        if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
        {
            Error = "Username and password are required.";
            return Page();
        }

        if (_tracker.IsLockedOut(username))
        {
            var remaining = _tracker.LockoutRemaining(username);
            var minutes = (int)Math.Ceiling(remaining.TotalMinutes);
            _logger.LogWarning("AUDIT: Admin login blocked — account locked. User={Username} IP={IP}", username, ip);
            Error = $"Account locked after too many failed attempts. Try again in {minutes} minute(s).";
            return Page();
        }

        var user = await _db.AdminUsers.FirstOrDefaultAsync(u => u.Username == username);

        if (user == null || !BCrypt.Net.BCrypt.Verify(password, user.PasswordHash))
        {
            _tracker.RecordFailure(username);
            _logger.LogWarning("AUDIT: Admin login failed. User={Username} IP={IP}", username, ip);
            Error = "Invalid username or password.";
            return Page();
        }

        _tracker.RecordSuccess(username);
        _logger.LogInformation("AUDIT: Admin login succeeded. User={Username} IP={IP}", username, ip);

        var claims = new List<Claim>
        {
            new(ClaimTypes.Name, user.Username),
            new(ClaimTypes.Role, "Admin")
        };

        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        var principal = new ClaimsPrincipal(identity);

        await HttpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            principal,
            new AuthenticationProperties
            {
                IsPersistent = true,
                ExpiresUtc = DateTimeOffset.UtcNow.AddHours(8)
            });

        return Redirect("/admin/andon-codes");
    }
}
