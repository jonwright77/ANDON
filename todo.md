# AndonApp – Project State

**Last updated:** 2026-05-08

---

## Stack

ASP.NET Core 8, Blazor Server (InteractiveServer), SQL Server / LocalDB, Entity Framework Core 8, SignalR, MailKit 4.9, ClosedXML, BCrypt.Net-Next.

**CSS cache busting:** increment `?v=N` in `App.razor` whenever `app.css` changes. Current: `v=30`.

---

## Admin Routes

| Route | Role | Purpose |
|---|---|---|
| `/admin/login` | — | Sign-in (cookie auth, BCrypt, brute-force lockout) |
| `/admin/logout` | — | Sign-out (POST + antiforgery only) |
| `/admin/overview` | All | **Default landing page.** Cards grouped by type, live Expected + Built, group/grand totals |
| `/admin/andon-codes` | All | Manage ANDON codes |
| `/admin/andon-codes/{id}/recipients` | All | Email recipients per code |
| `/admin/lines` | All | Manage production lines (icon action buttons) |
| `/admin/lines/{id}/schedule` | All | Shift schedule and breaks |
| `/admin/lines/{id}/targets` | All | Monthly target calendar |
| `/admin/lines/{id}/review` | All | Timeline chart + ERP build overlay + incident list |
| `/admin/line-types` | All | Manage line types |
| `/admin/todays-targets` | All | Today's targets grouped by type |
| `/admin/andon-history` | All | Date-range incident search + Excel export |
| `/admin/email-settings` | Admin | SMTP config + test email |
| `/admin/erp-settings` | Admin | ERP live + history query config |
| `/admin/users` | Admin | Create / delete admin and manager accounts |
| `/admin/general-settings` | Admin | Company name branding |

**End-user:** `/line/{slug}?token={token}` — production line status board

---

## Database

**Tables:** `AdminUsers` (with `Role`), `AndonCodes`, `AndonCodeRecipients`, `ProductionLines`, `LineTypes`, `Incidents`, `LineSchedules`, `ScheduleBreaks`, `LineTargets`, `LineOperatorTargets`

**Migrations (in order):**
`InitialCreate` → `AddWorkSchedules` → `AddLineTargets` → `AddLineOperatorTargets` → `AddProductionLinePool` → `AddLineTypes` → `AddUserRole`

---

## Configuration Files

| File | Managed by | Gitignored |
|---|---|---|
| `appsettings.json` | Repo | No |
| `appsettings.Development.json` | Repo | No |
| `email-settings.json` | Admin UI | Yes |
| `erp-settings.json` | Admin UI | Yes |
| `general-settings.json` | Admin UI | No (non-sensitive) |

Environment variables override JSON files (re-added `AddEnvironmentVariables()` after JSON sources).

---

## Implemented Features

### Security
- [x] Brute-force login protection — `LoginAttemptTracker` singleton, 5 failures → 15-min lockout
- [x] Logout via POST + antiforgery token only (`[ValidateAntiForgeryToken]` on page model class)
- [x] Cookie hardened: `HttpOnly`, `SecurePolicy = Always`, `SameSite = Strict`
- [x] Security headers middleware: `X-Content-Type-Options`, `X-Frame-Options`, `Referrer-Policy`, `Permissions-Policy`, `Content-Security-Policy`
- [x] ERP SELECT-only query guard — rejects non-SELECT and semicolons
- [x] Audit logging — admin logins (success/failure/lockout + IP), incident create/close
- [x] `.gitignore` covers `bin/`, `obj/`, `email-settings.json`, `erp-settings.json`
- [x] `AllowedHosts` restricted to `localhost;127.0.0.1` in base config; dev overrides to `*`
- [x] Default seed token replaced with 64-char cryptographically random token; insecure `demo-token-linea-1234` auto-replaced on startup
- [x] Startup warning if default admin password `Admin@123` has not been changed

### Auth / User Management
- [x] Two roles: `Admin` (full access) and `Manager` (no Email/ERP/Users/Settings pages)
- [x] `AdminOnly` policy and `ManagerOrAdmin` policy wired to all admin pages
- [x] Users page (`/admin/users`) — create user (username, password ≥8 chars, role), delete user
- [x] Cannot delete yourself or the last Admin account
- [x] Login stores `user.Role` claim (not hardcoded "Admin")

### Production Line Status Board
- [x] SCHEDULED / ELAPSED / TARGET / EXPECTED / BUILT stat cards
- [x] Dual target system (admin calendar target vs operator line target)
- [x] Work status badge — Working / Break name / Overtime
- [x] Card colour-coded by worst open incident severity (live SignalR)
- [x] New Incident modal / Close Incident / Line Target modal
- [x] HTML emails (with plain-text fallback) on incident open and close; closed email includes open duration
- [x] **Auto-reload at midnight** via `DateOnly` comparison in clock tick — clears ERP built totals and reloads all day-specific data

### Admin — Production Overview
- [x] Card grid grouped by Line Type, untyped lines last
- [x] Multi-select type filter chips — real-time filtering
- [x] Live Expected (clock-driven) and Built (SignalR) per card
- [x] Card colour: green / amber / red from open incidents
- [x] **Group subtotals** — Expected + Built blue cards below each group (hidden when only 1 group visible)
- [x] **Grand total** — Expected + Built cards at bottom of page, always visible
- [x] Live clock + date display (day of week + full date, updates at midnight)

### Admin — Lines
- [x] Create / edit / delete production lines
- [x] Icon-only action buttons (Open, Schedule, Targets, Review, Edit, Delete) with SVG icons + tooltips
- [x] Line Type assignment, ERP Pool mapping, auto-slug generation

### Admin — Today's Targets
- [x] All active lines with working hours, targets, variance
- [x] **Grouped by line type** with table group header rows
- [x] **Type column** added as second field

### Admin — Line Review
- [x] Date picker, schedule bar, SVG timeline chart
- [x] Incident bands (AMBER/RED), break overlays, now-line, hourly axis ticks
- [x] **ERP Build chart** — bar chart + cumulative SVG polyline overlaid on the timeline
- [x] Build data loads automatically on page open and date change
- [x] Incident list table with duration

### Admin — ANDON History (new)
- [x] Date range filter, defaults to last 7 days, auto-loads on init
- [x] Full incident list ordered by datetime
- [x] Columns: Opened, Line, Type, Severity, ANDON Code, Additional Info, Status, Closed, Duration
- [x] **Export to Excel** — styled `.xlsx` via ClosedXML (colour-coded severity, autofilter, frozen header, auto column width)

### Admin — ERP Settings
- [x] Live polling query section (existing)
- [x] **Historical build query section** (new) — separate SQL query, date column name, test button

### Admin — General Settings (new)
- [x] Company Name — shown in admin nav brand, login page heading, and email banner + footer

### Admin — Users (new)
- [x] List users with role badges, "You" indicator
- [x] Create user modal (username, password, role dropdown with descriptions)
- [x] Delete with confirmation; guards: cannot delete self or last Admin

### Admin Nav (improved)
- [x] `NavLink` components — active state highlight (background pill + bold)
- [x] SVG icons on each nav item
- [x] Visual separator between general and Admin-only sections
- [x] Right-side area: theme toggle + logged-in username + Sign Out button
- [x] **Overview is the default landing page** (login redirect updated)

### Email
- [x] HTML emails with colour-coded banner (RED/AMBER for open, GREEN for close)
- [x] Company name in banner and footer (when configured)
- [x] Duration open shown in closure emails
- [x] Plain-text fallback for clients that strip HTML
- [x] `WebUtility.HtmlEncode` on all user-supplied values

### ERP / Build Data
- [x] Live poll service → SignalR `BuiltUpdated` → BUILT card
- [x] On-demand history query → Line Review build chart (bars + cumulative line)
- [x] SELECT-only validation on both queries

### UI / UX
- [x] Dark mode toggle — persists to `localStorage`, scoped to `.admin-shell`
- [x] No flash on load — inline `<head>` script applies theme before CSS
- [x] Dark mode fixes on calendar target cells (background, text, input)
- [x] `download.js` — browser file download helper for Excel export

---

## Known Remaining Items

- [ ] **Access token visible in URL** — `/line/{slug}?token=...` — token appears in browser address bar and server access logs. Mitigation: call `history.replaceState` in `OnAfterRenderAsync` after auth to strip token from URL without breaking SignalR (which has already joined the group).
- [ ] **`MigrateAsync()` on every startup** — acceptable for development; for production deployments consider making DB migrations a separate CI/CD step rather than running on every app start.
- [ ] **No per-line access control for Managers** — a Manager user can see all production lines. If per-line restrictions are needed, a line-to-user mapping table would be required.
- [ ] **Password change for users** — currently users can only be created or deleted. A "Reset Password" action on the Users page would allow Admins to update passwords without deleting/recreating accounts.
