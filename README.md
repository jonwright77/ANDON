# ANDON System

A manufacturing ANDON incident management and production monitoring system built with ASP.NET Core 8 Blazor Server.

---

## Tech Stack

| Layer | Technology |
|---|---|
| UI | Blazor Server (ASP.NET Core 8) |
| Database | SQL Server / LocalDB (dev) |
| ORM | Entity Framework Core 8 (code-first) |
| Realtime | SignalR |
| Email | MailKit 4.9+ (SMTP, HTML emails) |
| ERP integration | Microsoft.Data.SqlClient (direct SQL, supports SQL Server 2008 R2+) |
| Excel export | ClosedXML |
| Auth | Cookie-based (admin/manager) / URL access token (end-user) |
| Password hashing | BCrypt.Net-Next |

---

## Prerequisites

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8)
- SQL Server LocalDB (included with Visual Studio, or install the standalone package)

Verify LocalDB is available:

```bash
sqllocaldb info
sqllocaldb start MSSQLLocalDB
```

---

## First Run

```bash
cd AndonApp/AndonApp
dotnet restore
dotnet run
```

The app automatically runs all pending EF Core migrations and seeds initial data on startup. No manual `dotnet ef database update` is required.

The app starts at `https://localhost:5001` (or `http://localhost:5000`).

---

## Configuration

### 1 — Database connection string

Set in `appsettings.json`. The default targets SQL Server LocalDB:

```json
"ConnectionStrings": {
  "DefaultConnection": "Server=(localdb)\\MSSQLLocalDB;Database=AndonDb;Trusted_Connection=True;MultipleActiveResultSets=true"
}
```

For production, override via environment variable:

```
ConnectionStrings__DefaultConnection=Server=PROD_SERVER;Database=AndonDb;...
```

> This is the only setting that requires a file edit. All other settings are managed from within the admin UI.

---

### 2 — Email (SMTP)

Configured within the app at **Admin → Email Settings** (`/admin/email-settings`). Admin-only.

Settings are saved to `email-settings.json` and hot-reload without a restart.

| Setting | Default | Description |
|---|---|---|
| Mode | `Log Only` | `Log Only` writes emails to the app log. Switch to `SMTP` to send real emails. |
| From Address | `andon@example.com` | The sender address on all outgoing emails. |
| SMTP Host | _(blank)_ | Your SMTP server hostname. |
| SMTP Port | `587` | 587 = STARTTLS, 465 = SSL, 25 = plain/relay. |
| Username | _(blank)_ | SMTP account username. |
| Password | _(blank)_ | SMTP account password. |

Emails are sent (HTML format, with plain-text fallback) when incidents are opened and closed. Failures are caught and logged — they never crash the app.

Common SMTP hosts:

| Provider | Host | Port |
|---|---|---|
| Office 365 / Outlook | `smtp.office365.com` | `587` |
| Gmail | `smtp.gmail.com` | `587` |
| Internal relay (no auth) | your relay hostname | `25` |

---

### 3 — ERP integration (optional)

Configured within the app at **Admin → ERP Settings** (`/admin/erp-settings`). Admin-only.

Settings are saved to `erp-settings.json` and hot-reload without a restart. ERP integration is **disabled by default**.

#### Live polling query (BUILT card)

| Setting | Default | Description |
|---|---|---|
| Enabled | `false` | Master on/off switch. |
| Connection String | _(blank)_ | SQL connection string to the ERP read-only database. |
| SQL Query | _(blank)_ | Returns at least a Pool column and a Quantity column. |
| Pool Column | `Pool` | Name of the pool identifier column. |
| Quantity Column | `Quantity` | Name of the quantity column. |
| Refresh Interval | `60` | Seconds between polls. Minimum 10 s. |

#### Historical build query (Line Review chart)

| Setting | Default | Description |
|---|---|---|
| History SQL Query | _(blank)_ | On-demand query for the Line Review build chart. Returns Pool, Quantity, and a datetime column. |
| Date/Time Column | `Timestamp` | Name of the datetime column in the history query results. |

Both queries use the same connection string, Pool Column, and Quantity Column. The historical query is called manually (not polled) and its results are filtered by line pool and selected date in the Line Review page.

Connection string format for SQL Server 2008 R2:
```
Server=ERPSERVER;Database=AX_DB;User Id=readonly_user;Password=xxx;TrustServerCertificate=True;Encrypt=False;
```

> Use a read-only SQL account. The app enforces SELECT-only queries (rejects anything that does not start with `SELECT` or contains a semicolon).

---

### 4 — General Settings

Configured within the app at **Admin → Settings** (`/admin/general-settings`). Admin-only.

| Setting | Default | Description |
|---|---|---|
| Company Name | _(blank)_ | Shown in the admin nav, login page, and email branding. |

Settings are saved to `general-settings.json` and hot-reload without a restart.

---

## Default Admin Login

| Field | Value |
|---|---|
| URL | `/admin/login` |
| Username | `admin` |
| Password | `Admin@123` |

> **Change this password immediately in production.** The app logs a security warning at startup if the default password has not been changed. Change it via **Admin → Users**.

---

## User Roles

The system supports two admin roles:

| Role | Access |
|---|---|
| **Admin** | Full access — all admin pages including Email Settings, ERP Settings, User Management, and General Settings. |
| **Manager** | Access to Overview, ANDON Codes, Production Lines, Line Types, Today's Targets, ANDON History, and Line Review. Cannot access Email Settings, ERP Settings, User Management, or General Settings. |

Users are managed at **Admin → Users** (`/admin/users`). The last Admin account cannot be deleted.

---

## Admin Routes

| Route | Role | Purpose |
|---|---|---|
| `/admin/overview` | All | **Default landing page.** Production overview — card per line grouped by type, live Expected + Built, group and grand totals. |
| `/admin/andon-codes` | All | Create / edit / delete ANDON codes and manage email recipients. |
| `/admin/andon-codes/{id}/recipients` | All | Email recipients for a specific ANDON code. |
| `/admin/lines` | All | Create / edit / delete production lines (icon action buttons). |
| `/admin/lines/{id}/schedule` | All | Per-line, per-day shift schedule and named break periods. |
| `/admin/lines/{id}/targets` | All | Monthly target calendar — set daily production targets. |
| `/admin/lines/{id}/review` | All | Day-by-day timeline chart + incident list. Optional ERP build chart overlay. |
| `/admin/line-types` | All | Create / edit / delete production line types. |
| `/admin/todays-targets` | All | All lines: today's working hours, targets, and variance. Grouped by line type. |
| `/admin/andon-history` | All | Date-range incident search with export to Excel (.xlsx). |
| `/admin/email-settings` | Admin | SMTP configuration + send test email. |
| `/admin/erp-settings` | Admin | ERP database integration — live poll and historical build queries. |
| `/admin/users` | Admin | Create and delete admin/manager accounts. |
| `/admin/general-settings` | Admin | Company name branding. |

---

## Production Line Status Board

URL: `/line/{slug}?token={AccessToken}`

The slug and token are shown in **Admin → Production Lines**. Invalid combinations show "Access Denied". The screen automatically reloads at midnight so the new day's data (ERP totals, schedule, targets) is always current.

### Status colours

| Colour | Meaning |
|---|---|
| **GREEN** | No open incidents — displays "ALL OK" |
| **AMBER** | At least one AMBER incident open |
| **RED** | At least one RED incident open |

### Stat cards

| Card | Value |
|---|---|
| **SCHEDULED** | Total working minutes today (shift minus breaks) |
| **ELAPSED** | Working minutes elapsed so far today |
| **TARGET** | Admin-set daily target; operator target shown as sub-line |
| **EXPECTED** | `round((Elapsed ÷ Scheduled) × Target)` — updates every second |
| **BUILT** | Live ERP quantity via SignalR; fades when data is stale |

### Actions

- **+ New Incident** — raise an incident (ANDON code, severity AMBER/RED, additional info). Sends HTML email to all recipients of that code.
- **✓ Close Incident** — marks incident closed, sends email with open duration.
- **◑ Line Target** — operator sets their own daily target.

All changes propagate to every connected screen in real-time via SignalR.

---

## ANDON History

URL: `/admin/andon-history`

- Defaults to the last 7 days on load.
- Date range selector with instant search.
- Table columns: Opened, Production Line, Type, Severity, ANDON Code, Additional Info, Status, Closed, Duration.
- **Export to Excel** button generates a styled `.xlsx` file (colour-coded severity, autofilter, frozen header row) and downloads it via the browser.

---

## Seed Data

On first startup the app seeds:

| Type | Value |
|---|---|
| Admin user | `admin` / `Admin@123` |
| ANDON codes | MACH (Machine Fault), QUAL (Quality Issue), SAFE (Safety Alert), MATL (Material Shortage) |
| Production line | Line A — slug: `line-a`, token: _(randomly generated 64-char hex)_ |

> The seed token is cryptographically random and different on every fresh install. Any existing line with the old insecure demo token `demo-token-linea-1234` is automatically replaced on startup.

---

## Database Schema

| Table | Purpose |
|---|---|
| `AdminUsers` | Admin/Manager accounts (BCrypt hashed passwords, Role column) |
| `AndonCodes` | ANDON code definitions |
| `AndonCodeRecipients` | Email addresses per ANDON code |
| `LineTypes` | Production line type definitions |
| `ProductionLines` | Production lines (slug, access token, type, ERP pool) |
| `Incidents` | Raised incidents (severity, status, timestamps) |
| `LineSchedules` | Per-line, per-day shift times |
| `ScheduleBreaks` | Named break periods within a schedule |
| `LineTargets` | Admin-set daily production targets |
| `LineOperatorTargets` | Operator-set daily production targets |

**Migration history:**
`InitialCreate` → `AddWorkSchedules` → `AddLineTargets` → `AddLineOperatorTargets` → `AddProductionLinePool` → `AddLineTypes` → `AddUserRole`

---

## Project Structure

```
AndonApp/
├── .gitignore
├── README.md
├── SECURITY_REVIEW.md
├── AndonApp.sln
└── AndonApp/
    ├── AndonApp.csproj
    ├── Program.cs
    ├── appsettings.json               ← DB connection string, AllowedHosts
    ├── appsettings.Development.json   ← Dev overrides (AllowedHosts: *, EF logging)
    ├── email-settings.json            ← created by admin UI (gitignored)
    ├── erp-settings.json              ← created by admin UI (gitignored)
    ├── general-settings.json          ← created by admin UI (company name)
    ├── Controllers/
    │   └── LineApiController.cs       ← end-user REST API (token-authenticated)
    ├── Data/
    │   ├── AndonDbContext.cs
    │   ├── DbSeeder.cs
    │   ├── Migrations/
    │   └── Models/
    │       ├── AdminUser.cs           ← includes Role property
    │       ├── AndonCode.cs
    │       ├── AndonCodeRecipient.cs
    │       ├── Incident.cs
    │       ├── LineOperatorTarget.cs
    │       ├── LineSchedule.cs
    │       ├── LineTarget.cs
    │       ├── LineType.cs
    │       ├── ProductionLine.cs
    │       └── ScheduleBreak.cs
    ├── Hubs/
    │   └── AndonHub.cs                ← SignalR hub (token validation, admin group join)
    ├── Pages/                         ← Razor Pages (login/logout only)
    │   ├── AdminLogin.cshtml(.cs)     ← brute-force protection, audit logging
    │   └── AdminLogout.cshtml(.cs)    ← POST-only with antiforgery
    ├── Services/
    │   ├── EmailService.cs            ← MailKit SMTP, HTML emails with company branding
    │   ├── ErpDataService.cs          ← live poll + history query, SELECT-only guard
    │   ├── ErpPollingService.cs       ← background poll service
    │   ├── ErpPollStatus.cs           ← thread-safe last-result singleton
    │   ├── ErpSettings.cs             ← live + history query config
    │   ├── GeneralSettings.cs         ← company name config
    │   ├── IErpDataService.cs         ← includes ErpBuildPoint record
    │   ├── IncidentService.cs         ← create/close, SignalR, email, audit log
    │   ├── IncidentSummaryDto.cs
    │   ├── LoginAttemptTracker.cs     ← in-memory brute-force lockout (5 failures → 15 min)
    │   └── GeneralSettings.cs
    ├── Components/
    │   ├── App.razor                  ← theme-init script, CSS/JS links
    │   ├── Layout/
    │   │   ├── AdminLayout.razor      ← NavLink active states, icons, role-based links
    │   │   └── MainLayout.razor
    │   └── Pages/
    │       ├── Admin/
    │       │   ├── AndonCodes.razor
    │       │   ├── AndonCodeRecipients.razor
    │       │   ├── AndonHistory.razor     ← /admin/andon-history (date range + Excel export)
    │       │   ├── EmailConfig.razor
    │       │   ├── ErpConfig.razor        ← live + historical query sections
    │       │   ├── GeneralSettings.razor  ← /admin/general-settings
    │       │   ├── LineReview.razor       ← timeline chart + ERP build bar/line chart
    │       │   ├── Lines.razor            ← icon action buttons
    │       │   ├── LineSchedules.razor
    │       │   ├── LineTargets.razor
    │       │   ├── LineTypes.razor
    │       │   ├── Overview.razor         ← group + grand total cards
    │       │   ├── TodaysTargets.razor    ← grouped by line type
    │       │   └── Users.razor            ← /admin/users (create/delete, role management)
    │       └── Line/
    │           └── LineStatus.razor       ← auto-reloads at midnight
    └── wwwroot/
        ├── css/
        │   └── app.css
        └── js/
            ├── download.js            ← browser file download helper (Excel export)
            └── theme.js               ← dark mode get/set/toggle
```

---

## API Endpoints

### End-user (requires `?token=` query parameter)

```
GET  /api/lines/{slug}/status?token=...              Current status (GREEN/AMBER/RED) + open count
GET  /api/lines/{slug}/incidents?token=...           List of open incidents
POST /api/lines/{slug}/incidents?token=...           Raise a new incident
POST /api/lines/{slug}/incidents/{id}/close?token=...  Close an incident
```

---

## Security Notes

- Admin login is protected against brute-force: 5 failed attempts locks the account for 15 minutes.
- Logout requires a POST request with an antiforgery token; a GET to `/admin/logout` redirects to login without signing out.
- All admin cookies are `HttpOnly`, `Secure` (HTTPS only), and `SameSite=Strict`.
- ERP and email credentials are stored in on-disk JSON files (`erp-settings.json`, `email-settings.json`) that are gitignored. Environment variables override these files in production.
- ERP queries are validated as SELECT-only before execution.
- All admin/manager logins are audit-logged (username + IP). Incident create and close events are also audit-logged.
- Full security review is documented in `SECURITY_REVIEW.md`.
