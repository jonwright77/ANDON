# ANDON App – Security Review

**Date:** 2026-05-03  
**Reviewer:** Claude Code  
**Scope:** Full codebase review of AndonApp (ASP.NET Core 8 / Blazor Server)

---

## Summary

| Severity | Count |
|----------|-------|
| CRITICAL | 2 |
| HIGH     | 4 |
| MEDIUM   | 5 |
| LOW      | 4 |

---

## CRITICAL

### C1 – Plaintext SMTP credentials on disk with no `.gitignore`

**File:** `AndonApp/AndonApp/email-settings.json`

The file contains a real Gmail account username and app password in plaintext:

```json
{
  "SMTP_USER": "jonnywwright@googlemail.com",
  "SMTP_PASS": "uowt hhuu imic koxc"
}
```

The file is currently untracked (`??` in git status), but there is **no `.gitignore`** anywhere in the repository. A single `git add .` would commit these credentials to history permanently. Build artifacts (`bin/`, `obj/`) are also being tracked.

**Recommendations:**
- Immediately revoke and regenerate the Gmail app password.
- Add a `.gitignore` at the repo root (see template below).
- Never store credentials in JSON files in the working tree. Use environment variables, Windows Credential Manager, or `dotnet user-secrets` for local development.
- For production, inject secrets via environment variables or a secrets vault (Azure Key Vault, etc.).

**Suggested `.gitignore` additions:**
```
bin/
obj/
*.user
email-settings.json
erp-settings.json
appsettings.*.json
!appsettings.json
!appsettings.Development.json
```

---

### C2 – Predictable seed access token

**File:** `AndonApp/AndonApp/Data/DbSeeder.cs`, line 43

```csharp
AccessToken = "demo-token-linea-1234",
```

This hardcoded, guessable token is seeded into every fresh database. Anyone who knows the slug `line-a` and tries `demo-token-linea-1234` gains full access to that production line's incident controls — with no login required.

**Recommendations:**
- Remove the hardcoded token entirely; generate one with `Guid.NewGuid().ToString("N") + Guid.NewGuid().ToString("N")` (as the Lines.razor UI already does for new lines).
- Add a check in `DbSeeder` that replaces any token matching the insecure default at startup.

---

## HIGH

### H1 – No brute-force protection on admin login

**File:** `AndonApp/AndonApp/Pages/AdminLogin.cshtml.cs`

The login handler performs a direct credential check with no rate limiting, no account lockout, no CAPTCHA, and no delay on failure. An attacker can make unlimited login attempts as fast as the server responds.

**Recommendations:**
- Add a failed-attempt counter per username (in-memory `ConcurrentDictionary` or database).
- Lock the account for 15 minutes after 5 consecutive failures.
- Consider `Microsoft.AspNetCore.RateLimiting` middleware on the `/admin/login` POST endpoint.

---

### H2 – Logout triggered by HTTP GET (CSRF-vulnerable)

**File:** `AndonApp/AndonApp/Pages/AdminLogout.cshtml.cs`

```csharp
public async Task<IActionResult> OnGetAsync()
{
    await HttpContext.SignOutAsync(...);
    return Redirect("/admin/login");
}
```

Logout via GET means any page or email the admin visits that contains `<img src="/admin/logout">` or a link to that URL will silently sign them out. This is a classic CSRF logout attack.

**Recommendations:**
- Change to `OnPostAsync()` and require a POST from a form with antiforgery token.
- The admin layout's logout button should submit a `<form method="post">` rather than an `<a href>`.

---

### H3 – ERP query executed without SELECT-only enforcement

**File:** `AndonApp/AndonApp/Services/ErpDataService.cs`, lines 25 and 44

```csharp
await using var cmd = new SqlCommand(settings.Query, conn);
```

The SQL query comes directly from `erp-settings.json` with no parsing or validation. The UI says "use SELECT-only" but does not enforce this. If the ERP connection string has write permissions, a compromised admin account could run `DROP TABLE`, `DELETE`, `INSERT`, or `EXEC xp_cmdshell` against the ERP database.

**Recommendations:**
- Connect to ERP using a read-only SQL account (enforce at the database level — this is the most robust mitigation).
- Optionally add a server-side check that rejects queries not starting with `SELECT` (simple guard, not foolproof against `; DROP`).
- Log every query execution with timestamp and current admin user.

---

### H4 – ~~All production line access tokens broadcast over SignalR from Overview~~ — FALSE POSITIVE

**File:** `AndonApp/AndonApp/Components/Pages/Admin/Overview.razor`

**Status: Closed — not a vulnerability in this architecture.**

This finding was initially raised because `Overview.razor` calls `JoinLine(slug, token)` for every production line. However, investigation during remediation revealed that in **Blazor Server**, `HubConnectionBuilder.WithUrl().Build()` creates a **.NET SignalR client that runs inside the ASP.NET Core server process**, not in the browser.

The consequence is:
- The `JoinLine` calls travel **server-to-server** over a loopback/internal connection.
- The access tokens **never transit the browser** and are **never visible in browser DevTools**.
- There is no WebSocket frame the user's browser can inspect.

An attempt to replace the pattern with an `AdminJoinAllLines` hub method (checking `Context.User.IsInRole("Admin")`) was reverted because the server-side hub connection carries no browser authentication cookies, causing the check to always fail and the page to break.

**If the app is ever ported to Blazor WebAssembly** the concern would become real, as hub connections would then originate from the browser. At that point, implement an `AdminJoinAllLines` hub method that validates a short-lived signed token issued server-side (e.g., via `IDataProtectionProvider`).

---

## MEDIUM

### M1 – `AllowedHosts: "*"` in production config

**File:** `AndonApp/AndonApp/appsettings.json`, line 8

```json
"AllowedHosts": "*"
```

Accepting any `Host` header allows host header injection attacks, which can poison password-reset links, cache poisoning, and SSRF scenarios.

**Recommendations:**
- Set `AllowedHosts` to the actual domain(s) used in production (e.g., `"andon.yourdomain.com"`).

---

### M2 – Credentials stored as plain JSON files on disk

**Files:** `email-settings.json`, `erp-settings.json`

Both files are written to the application's content root directory by the admin UI and are world-readable by anything with filesystem access to the server (other processes, IIS app pools sharing a server, etc.). They contain SMTP passwords and ERP connection strings.

**Recommendations:**
- For production, move credentials to environment variables or a secrets manager.
- If file-based config must be used, restrict file permissions at the OS level so only the app pool identity can read them.
- Consider encrypting the sensitive fields at rest using `DataProtectionProvider`.

---

### M3 – Missing HTTP security headers

No security headers are added anywhere in `Program.cs` or middleware.

**Missing headers:**
| Header | Risk without it |
|--------|----------------|
| `Content-Security-Policy` | XSS, clickjacking |
| `X-Frame-Options: DENY` | Clickjacking |
| `X-Content-Type-Options: nosniff` | MIME sniffing attacks |
| `Referrer-Policy: strict-origin` | Token leakage in Referrer header |
| `Permissions-Policy` | Feature abuse |

**Recommendation:** Add a security headers middleware in `Program.cs`:
```csharp
app.Use(async (ctx, next) =>
{
    ctx.Response.Headers.Append("X-Frame-Options", "DENY");
    ctx.Response.Headers.Append("X-Content-Type-Options", "nosniff");
    ctx.Response.Headers.Append("Referrer-Policy", "strict-origin");
    ctx.Response.Headers.Append("Content-Security-Policy",
        "default-src 'self'; script-src 'self' 'unsafe-inline'; style-src 'self' 'unsafe-inline'");
    await next();
});
```

---

### M4 – Access token exposed in URL query string

**Design:** `/line/{slug}?token={AccessToken}`

Tokens in query strings appear in:
- Web server access logs
- Browser history
- `Referrer` header when navigating to external links from the status page
- Network proxies and load balancer logs

**Recommendations:**
- Move token to a short-lived session cookie set after a one-time token validation step.
- Alternatively, POST the token to a validation endpoint that sets a cookie, then redirect to the clean URL.
- At minimum, add `Referrer-Policy: no-referrer` to the line status page responses.

---

### M5 – `AdditionalInfo` not validated at the API/DTO boundary

**File:** `AndonApp/AndonApp/Controllers/LineApiController.cs`, line 87

```csharp
public record CreateIncidentDto(int AndonCodeId, string Severity, string? AdditionalInfo);
```

`AdditionalInfo` has `[MaxLength(1000)]` on the model, but the DTO has no validation attribute. EF Core enforces the length at the database level, but the API will accept an oversized payload and only fail deep in the stack, producing a 500 rather than a clean 400.

**Recommendations:**
- Add `[MaxLength(1000)]` to the DTO property.
- Enable model validation globally so bad inputs return 400 immediately.

---

## LOW

### L1 – Default admin credentials seeded with no forced change

**File:** `AndonApp/AndonApp/Data/DbSeeder.cs`, line 15–18

The seed creates `admin / Admin@123`. There is no mechanism to detect or require changing this before going to production.

**Recommendations:**
- Add a startup warning log if the admin password hash matches the default.
- Consider adding a `PasswordChangedAt` column and blocking access until the password has been changed from the seed default.

---

### L2 – Cookie `Secure` policy not explicitly set

**File:** `AndonApp/AndonApp/Program.cs`, lines 22–29

The `AddCookie` call does not explicitly set `SecurePolicy`. By default this is `SameAsRequest`, meaning the authentication cookie will be served over HTTP in development. If the app is accidentally deployed without HTTPS, the session cookie is sent in cleartext.

**Recommendation:**
```csharp
options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
options.Cookie.HttpOnly = true;
options.Cookie.SameSite = SameSiteMode.Strict;
```

---

### L3 – No audit logging for admin and incident actions

There is no audit trail for:
- Admin logins (success and failure)
- Incident creation and closure (which admin or which line token performed the action)
- Admin configuration changes (ERP settings, email settings, line edits)

**Recommendations:**
- Log admin login attempts (success/failure, IP address, timestamp) to a structured log.
- Store `CreatedByToken` or similar on incidents so the line that raised an incident is traceable.

---

### L4 – No `.gitignore` — build artifacts tracked in git

The repository has no `.gitignore`. The `bin/` and `obj/` directories are being modified and tracked, bloating the repository and creating noise in `git status` / diffs.

**Recommendation:** Add a standard `.gitignore` for .NET projects (Visual Studio template is a good starting point) and run `git rm -r --cached bin/ obj/` to untrack them.

---

## Implementation Status

| # | Finding | Severity | Status |
|---|---------|----------|--------|
| C1 | Plaintext SMTP credentials / no `.gitignore` | CRITICAL | ✅ Fixed — `.gitignore` added, credential files excluded, build artifacts untracked. **Action required: revoke & regenerate Gmail app password.** |
| C2 | Predictable seed access token | CRITICAL | ✅ Fixed — token now generated with two GUIDs; existing insecure tokens replaced on startup. |
| H1 | No brute-force protection on login | HIGH | ✅ Fixed — `LoginAttemptTracker` locks account for 15 min after 5 failures. |
| H2 | Logout via HTTP GET (CSRF) | HIGH | ✅ Fixed — logout requires POST with antiforgery token; GET redirects to login without signing out. |
| H3 | ERP query without SELECT-only enforcement | HIGH | ✅ Fixed — `ValidateSelectQuery` rejects non-SELECT queries and semicolons. |
| H4 | SignalR tokens visible in browser DevTools | HIGH | ⬜ Closed as false positive — Blazor Server hub connections are server-to-server; tokens never transit the browser. |
| M1 | `AllowedHosts: "*"` | MEDIUM | ✅ Fixed — production default tightened to `localhost;127.0.0.1`; `appsettings.Development.json` overrides to `*` for dev. **Set to your real hostname before deploying.** |
| M2 | Credentials stored as plain JSON on disk | MEDIUM | ✅ Partially fixed — `AddEnvironmentVariables()` re-added after JSON files so env vars always override disk files in production. Files remain gitignored. OS-level file permissions are still recommended. |
| M3 | Missing HTTP security headers | MEDIUM | ✅ Fixed — `X-Content-Type-Options`, `X-Frame-Options`, `Referrer-Policy`, `Permissions-Policy`, and `Content-Security-Policy` added via middleware in `Program.cs`. |
| M4 | Access token in URL query string | MEDIUM | ✅ Mitigated — `Referrer-Policy: strict-origin` (from M3) prevents the query-string token leaking in Referrer headers. Token still appears in server access logs; use a secrets manager for access logs in production. |
| M5 | `AdditionalInfo` not validated at DTO boundary | MEDIUM | ✅ Fixed — `[MaxLength(1000)]` added to `CreateIncidentDto`; `[ApiController]` returns 400 on violation. |
| L1 | Default admin credentials with no forced change | LOW | ✅ Fixed — `DbSeeder` checks the default password hash at every startup and logs a `LogWarning` if it has not been changed. |
| L2 | Cookie `Secure` policy not explicitly set | LOW | ✅ Fixed — `HttpOnly = true`, `SecurePolicy = Always`, `SameSite = Strict` added to cookie options. |
| L3 | No audit logging | LOW | ✅ Fixed — structured `AUDIT:` log entries added for admin login (success/failure/lockout + IP), incident creation, and incident closure. |
| L4 | No `.gitignore` | LOW | ✅ Fixed (resolved as part of C1). |
