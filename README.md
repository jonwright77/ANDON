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
| Email | MailKit (SMTP) |
| ERP integration | Microsoft.Data.SqlClient (direct SQL, supports SQL Server 2008 R2+) |
| Auth | Cookie-based (admin) / URL access token (end-user) |

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

The app automatically runs all pending EF Core migrations and seeds initial data on startup. The database is created on first run — no manual `dotnet ef database update` is required.

The app starts at `https://localhost:5001` (or `http://localhost:5000`).

---

## Configuration

### 1 — Database connection string

The database connection is set in `appsettings.json`. The default targets SQL Server LocalDB for development:

```json
"ConnectionStrings": {
  "DefaultConnection": "Server=(localdb)\\MSSQLLocalDB;Database=AndonDb;Trusted_Connection=True;MultipleActiveResultSets=true"
}
```

For production, override via environment variable:

```
ConnectionStrings__DefaultConnection=Server=PROD_SERVER;Database=AndonDb;...
```

> This is the only setting that requires a file edit or environment variable — all other settings can be changed from within the admin UI.

---

### 2 — Email (SMTP)

Email settings are configured entirely within the app at **Admin → Email Settings** (`/admin/email-settings`).

Settings are saved to `email-settings.json` and hot-reload without a restart.

| Setting | Default | Description |
|---|---|---|
| Mode | `Log Only` | `Log Only` writes emails to the app log. Switch to `SMTP` to send real emails. |
| From Address | `andon@example.com` | The sender address on all outgoing emails. |
| SMTP Host | _(blank)_ | Your SMTP server hostname. |
| SMTP Port | `587` | SMTP port (587 = STARTTLS, 465 = SSL, 25 = plain/relay). |
| Username | _(blank)_ | SMTP account username. Leave blank for unauthenticated relays. |
| Password | _(blank)_ | SMTP account password. |

Use the **Send Test Email** button on the settings page to verify your configuration before saving.

Common SMTP hosts:

| Provider | Host | Port |
|---|---|---|
| Office 365 / Outlook | `smtp.office365.com` | `587` |
| Gmail | `smtp.gmail.com` | `587` |
| Internal relay (no auth) | your relay hostname | `25` |

Emails are sent when incidents are opened and closed. Email failures are caught and logged — they will never crash the application.

---

### 3 — ERP integration (optional)

ERP settings are configured within the app at **Admin → ERP Settings** (`/admin/erp-settings`).

Settings are saved to `erp-settings.json` and hot-reload without a restart. ERP integration is **disabled by default** — the app runs fully without it.

| Setting | Default | Description |
|---|---|---|
| Enabled | `false` | Master on/off switch. |
| Connection String | _(blank)_ | SQL connection string to the ERP read-only database. |
| SQL Query | _(blank)_ | Query that returns at least a Pool column and a Quantity column. |
| Pool Column | `Pool` | Name of the pool identifier column in the query results. |
| Quantity Column | `Quantity` | Name of the quantity column in the query results. |
| Refresh Interval | `60` | Seconds between polls. Minimum 10 s. |

The integration polls the ERP database on the configured interval and pushes the `Quantity` value to each production line screen (and the Overview board) in real-time via SignalR. Each production line has an optional **Pool** field that maps it to a row in the query results.

Use the **Test Connection** button to run the query and preview the first 5 rows before enabling.

Connection string format for SQL Server 2008 R2 with SQL authentication:
```
Server=ERPSERVER;Database=AX_DB;User Id=readonly_user;Password=xxx;TrustServerCertificate=True;Encrypt=False;
```

> The ERP database user should have `SELECT`-only permissions. Write operations in the query are not supported.

---

## Default Admin Login

| Field | Value |
|---|---|
| Username | `admin` |
| Password | `Admin@123` |

Admin URL: `/admin/login`

> Change this password immediately in production via the `AdminUsers` table or by updating `DbSeeder.cs`.

---

## Admin Features

| Route | Purpose |
|---|---|
| `/admin/overview` | Production overview — card per line, grouped by type, live Expected + Built values |
| `/admin/andon-codes` | Create / edit / delete ANDON codes |
| `/admin/andon-codes/{id}/recipients` | Manage email recipients for a code |
| `/admin/lines` | Create / edit / delete production lines |
| `/admin/lines/{id}/schedule` | Shift schedule and named break periods per day |
| `/admin/lines/{id}/targets` | Monthly target calendar (admin targets) |
| `/admin/lines/{id}/review` | Timeline chart + incident list for any date |
| `/admin/line-types` | Create / edit / delete production line types |
| `/admin/todays-targets` | Today's working hours, targets and variance for all lines |
| `/admin/email-settings` | SMTP configuration + test email |
| `/admin/erp-settings` | ERP database integration + connection test |

The admin nav includes a **🌙 / ☀ dark mode toggle** that persists across sessions.

---

## Production Line Status Board

URL: `/line/{slug}?token={AccessToken}`

The slug and token are shown in **Admin → Production Lines**. Validate the slug and token before accessing — invalid combinations show "Access Denied".

### Status colours

| Colour | Meaning |
|---|---|
| **GREEN** | No open incidents — displays "ALL OK" |
| **AMBER** | At least one AMBER incident open |
| **RED** | At least one RED incident open |

### Stat cards (top bar)

| Card | Value |
|---|---|
| **SCHEDULED** | Total working minutes today (shift minus breaks) |
| **ELAPSED** | Working minutes elapsed so far today |
| **TARGET** | Admin-set daily target; operator target shown as sub-line |
| **EXPECTED** | `round((Elapsed ÷ Scheduled) × Target)` — updates every second |
| **BUILT** | Live ERP quantity via SignalR; fades when data is stale |

### Actions

- **+ New Incident** — raise an incident (ANDON code, severity AMBER/RED, additional info). Sends email to all recipients of that code.
- **✓ Close Incident** — marks incident closed, sends email.
- **◑ Line Target** — operator sets their own daily target. Shown as a sub-line in TARGET and EXPECTED cards.

All changes update every connected screen in real-time via SignalR.

---

## Seed Data

On first startup the app seeds:

| Type | Value |
|---|---|
| Admin user | `admin` / `Admin@123` |
| ANDON codes | MACH (Machine Fault), QUAL (Quality Issue), SAFE (Safety Alert), MATL (Material Shortage) |
| Production line | Line A — slug: `line-a`, token: `demo-token-linea-1234` |

Example line URL: `/line/line-a?token=demo-token-linea-1234`

---

## Database Schema

| Table | Purpose |
|---|---|
| `AdminUsers` | Admin accounts (BCrypt hashed passwords) |
| `AndonCodes` | ANDON code definitions |
| `AndonCodeRecipients` | Email addresses per ANDON code |
| `LineTypes` | Production line type definitions |
| `ProductionLines` | Production lines (slug, access token, type, ERP pool) |
| `Incidents` | Raised incidents (severity, status, timestamps) |
| `LineSchedules` | Per-line, per-day shift times |
| `ScheduleBreaks` | Named break periods within a schedule |
| `LineTargets` | Admin-set daily production targets |
| `LineOperatorTargets` | Operator-set daily production targets |

**Migration history:** `InitialCreate` → `AddWorkSchedules` → `AddLineTargets` → `AddLineOperatorTargets` → `AddProductionLinePool` → `AddLineTypes`

---

## Project Structure

```
AndonApp/
├── AndonApp.sln
└── AndonApp/
    ├── AndonApp.csproj
    ├── Program.cs
    ├── appsettings.json               ← DB connection string, defaults
    ├── email-settings.json            ← created by admin UI (add to .gitignore)
    ├── erp-settings.json              ← created by admin UI (add to .gitignore)
    ├── Controllers/
    │   └── LineApiController.cs       ← end-user REST API (token-authenticated)
    ├── Data/
    │   ├── AndonDbContext.cs
    │   ├── DbSeeder.cs
    │   ├── Migrations/
    │   └── Models/
    │       ├── AdminUser.cs
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
    │   └── AndonHub.cs                ← SignalR hub (slug + token validation)
    ├── Pages/                         ← Razor Pages (login/logout only)
    │   ├── AdminLogin.cshtml(.cs)
    │   └── AdminLogout.cshtml(.cs)
    ├── Services/
    │   ├── EmailService.cs            ← MailKit SMTP + test method
    │   ├── ErpDataService.cs          ← raw SqlClient ERP queries
    │   ├── ErpPollingService.cs       ← background poll service
    │   ├── ErpPollStatus.cs           ← thread-safe last-result singleton
    │   ├── ErpSettings.cs             ← ERP config class
    │   ├── IErpDataService.cs
    │   ├── IncidentService.cs         ← create/close incidents, SignalR + email
    │   └── IncidentSummaryDto.cs
    ├── Components/
    │   ├── App.razor                  ← theme-init script, CSS link
    │   ├── Layout/
    │   │   ├── AdminLayout.razor      ← nav bar + dark mode toggle
    │   │   └── MainLayout.razor
    │   └── Pages/
    │       ├── Admin/
    │       │   ├── AndonCodes.razor
    │       │   ├── AndonCodeRecipients.razor
    │       │   ├── EmailConfig.razor  ← /admin/email-settings
    │       │   ├── ErpConfig.razor    ← /admin/erp-settings
    │       │   ├── LineReview.razor
    │       │   ├── Lines.razor
    │       │   ├── LineSchedules.razor
    │       │   ├── LineTargets.razor
    │       │   ├── LineTypes.razor
    │       │   ├── Overview.razor     ← /admin/overview
    │       │   └── TodaysTargets.razor
    │       └── Line/
    │           └── LineStatus.razor   ← end-user status board
    └── wwwroot/
        ├── css/
        │   └── app.css
        └── js/
            └── theme.js               ← dark mode get/set/toggle helpers
```

---

## API Endpoints

### End-user (requires `?token=` query parameter)

```
GET  /api/lines/{slug}/status?token=...        Current status (GREEN/AMBER/RED) + open count
GET  /api/lines/{slug}/incidents?token=...     List of open incidents
POST /api/lines/{slug}/incidents?token=...     Raise a new incident
POST /api/lines/{slug}/incidents/{id}/close?token=...   Close an incident
```

---

## .gitignore Additions

The following files are created at runtime by the admin UI and contain sensitive credentials. Add them to `.gitignore`:

```
AndonApp/AndonApp/email-settings.json
AndonApp/AndonApp/erp-settings.json
```
