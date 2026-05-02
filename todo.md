# AndonApp – Project State

## Overview

**Stack:** ASP.NET Core 8, Blazor Server (InteractiveServer), SQL Server / LocalDB, Entity Framework Core 8, SignalR, MailKit.

**CSS cache busting:** `App.razor` links `css/app.css?v=17` — increment `v` whenever `app.css` changes.

---

## Admin Routes

| Route | Purpose |
|---|---|
| `/admin/login` | Admin sign-in (cookie auth, BCrypt) |
| `/admin/logout` | Admin sign-out |
| `/admin/overview` | Production overview board — card per line, grouped by type |
| `/admin/andon-codes` | Manage ANDON codes and email recipients |
| `/admin/andon-codes/{id}/recipients` | Email recipients for a code |
| `/admin/lines` | Manage production lines |
| `/admin/lines/{id}/schedule` | Per-line shift schedule and breaks |
| `/admin/lines/{id}/targets` | Per-line monthly target calendar |
| `/admin/lines/{id}/review` | Day-by-day incident timeline and list |
| `/admin/line-types` | Manage production line types |
| `/admin/todays-targets` | All lines: today's working hours, targets, and variance |
| `/admin/email-settings` | SMTP configuration + send test email |
| `/admin/erp-settings` | ERP database integration settings + connection test |

**End-user:** `/line/{slug}?token={token}` — production line status board

---

## Database

**Tables:** `AdminUsers`, `AndonCodes`, `AndonCodeRecipients`, `ProductionLines`, `LineTypes`, `Incidents`, `LineSchedules`, `ScheduleBreaks`, `LineTargets`, `LineOperatorTargets`

**Migrations (in order):**
`InitialCreate` → `AddWorkSchedules` → `AddLineTargets` → `AddLineOperatorTargets` → `AddProductionLinePool` → `AddLineTypes`

---

## Configuration

### Database connection string
Set in `appsettings.json` (or override via environment variable `ConnectionStrings__DefaultConnection`):
```json
"ConnectionStrings": {
  "DefaultConnection": "Server=(localdb)\\MSSQLLocalDB;Database=AndonDb;Trusted_Connection=True;MultipleActiveResultSets=true"
}
```

### Email settings
Managed via **Admin → Email Settings** (`/admin/email-settings`).
Saved to `email-settings.json` (hot-reloads, no restart needed).
Defaults to `LogOnly` mode — switch to `Smtp` to send real emails.
Fields: Mode, From Address, SMTP Host, Port, Username, Password. Includes a **Send Test Email** function.

### ERP integration settings
Managed via **Admin → ERP Settings** (`/admin/erp-settings`).
Saved to `erp-settings.json` (hot-reloads, no restart needed).
Disabled by default (`Enabled: false`) — app runs normally without it.
Fields: Enabled toggle, Connection String, SQL Query, Pool Column, Quantity Column, Refresh Interval. Includes a **Test Connection** function that previews query results.

---

## Completed Features

### Code quality
- [x] `AddDbContextFactory` — short-lived `await using var db` per method, no circuit-lifetime context leak
- [x] SignalR hub validates slug + token + IsActive before joining groups
- [x] `CloseIncident()` disables button in-flight, shows per-card inline error on failure
- [x] Email address validation uses `MailAddress.Parse` (not `Contains('@')`)
- [x] Admin API controller removed — Blazor pages are the sole admin data path
- [x] SignalR broadcasts full `IncidentSummaryDto` (including `ProductionLineId`) on create — no DB round-trip on SignalR events

### Production line status board (`/line/{slug}`)
- [x] **SCHEDULED** card — total working minutes (shift minus breaks), shift times sub-line
- [x] **ELAPSED** card — working minutes elapsed, clamped to shift bounds, deducts completed/in-progress breaks
- [x] **TARGET** card — admin-set daily target (from Target Calendar); operator line target shown as sub-line
- [x] **EXPECTED** card — `round((Elapsed ÷ Scheduled) × Target)`; shows both admin and operator expected values
- [x] **BUILT** card — live ERP quantity via SignalR; fades when data is stale (> 2× refresh interval); seeds from last poll on page load
- [x] Work status badge — Working / Break name / Overtime, updates every second
- [x] Dual target system — Admin Target (calendar) vs Operator Line Target (button on status board)
- [x] All cards colour-coded by worst open incident severity (green / amber / red)

### Admin — Production Lines (`/admin/lines`)
- [x] Create / edit / delete production lines
- [x] **Line Type** — optional type assignment, shown as blue badge in table
- [x] **Pool** — optional ERP pool identifier for BUILT card mapping
- [x] Auto-generated access token; end-user URL shown in table

### Admin — Line Types (`/admin/line-types`)
- [x] Create / edit / delete line types (Name, Description)
- [x] Shows count of lines using each type; deletion clears the type from affected lines (SET NULL)

### Admin — Schedule (`/admin/lines/{id}/schedule`)
- [x] Per-line, per-day shift schedule (start/end time, workday toggle)
- [x] Named break periods per day

### Admin — Targets (`/admin/lines/{id}/targets`)
- [x] Monthly calendar grid — click any day to set/edit/clear admin target
- [x] Shows both admin target (editable) and operator target (read-only `Line: X` label) per cell

### Admin — Line Review (`/admin/lines/{id}/review`)
- [x] Date picker (capped at today); defaults to today
- [x] Schedule summary bar (shift times + named breaks)
- [x] SVG timeline chart — incident bands (AMBER/RED), break overlays, now-line, hourly ticks
- [x] Incident list table — severity, code, opened/closed times, duration, status, additional info

### Admin — Today's Targets (`/admin/todays-targets`)
- [x] All active lines: working hours, breaks, scheduled minutes, admin target, operator target, variance (colour-coded)

### Admin — Production Overview (`/admin/overview`)
- [x] Card grid, one card per active production line, grouped by Line Type with section headers
- [x] Multi-select type filter (checkbox chips) — filters by type in real-time; untyped lines shown separately
- [x] Each card shows: line name, **Expected** (live clock-driven), **Built** (live ERP via SignalR)
- [x] Card colour: green / amber / red based on open incidents — updates in real-time via SignalR

### Admin — Email Settings (`/admin/email-settings`)
- [x] Mode selector: Log Only / SMTP
- [x] SMTP fields: Host, Port, Username, Password (show/hide toggle), From Address
- [x] Saves to `email-settings.json` — hot-reloads without restart
- [x] **Send Test Email** button — tests current form values against live SMTP before saving

### Admin — ERP Settings (`/admin/erp-settings`)
- [x] Enable/disable ERP polling
- [x] Connection string, SQL query (textarea), Pool column, Quantity column, refresh interval
- [x] Saves to `erp-settings.json` — hot-reloads without restart
- [x] **Test Connection** button — runs query, previews first 5 rows in table
- [x] Last poll status bar: timestamp, row count or error message

### ERP integration (BUILT card feed)
- [x] `ErpDataService` — raw `Microsoft.Data.SqlClient` (supports SQL Server 2008 R2+), no EF Core
- [x] `ErpPollStatus` — thread-safe singleton holding last result, timestamp, error
- [x] `ErpPollingService` — `BackgroundService`, polls on configured interval, sleeps 30 s when disabled, never crashes
- [x] SignalR `BuiltUpdated(slug, qty)` broadcast to each matching line group on every successful poll
- [x] Staleness indicator — BUILT card fades if last update is older than 2× refresh interval

### UI
- [x] Dark mode toggle in admin nav bar — persists to `localStorage`, applies instantly via CSS `html.dark` class
- [x] No flash on load — inline `<head>` script applies saved theme before CSS renders
- [x] Dark mode scoped to `.admin-shell` — production line status board unaffected

---

## Deferred / Known Issues

- [ ] **Access token in URL** — token is visible in the browser URL bar and server logs. Fix: call `history.replaceState(null, "", "/line/{slug}")` in `LineStatus.razor` `OnAfterRenderAsync` (first render, authorised) to strip it without breaking the session.
- [ ] **No brute-force protection on admin login** — no rate limiting or lockout on `AdminLogin.cshtml.cs`. Consider ASP.NET Core IP rate limiting middleware for production.
- [ ] **`MigrateAsync()` on every startup** — `DbSeeder.SeedAsync` always calls `MigrateAsync()`. Acceptable for development; should be a separate deployment step in production.
- [ ] **`AndonCode.CreatedAt` default** — migration creates a non-nullable `datetime2` column; verify `AndonCode.cs` has `public DateTime CreatedAt { get; set; } = DateTime.UtcNow;` so the seeder doesn't fail.
