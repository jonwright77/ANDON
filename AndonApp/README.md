# ANDON System

A full-stack manufacturing ANDON incident management system built with ASP.NET Core 8 Blazor Server.

---

## Tech Stack

| Layer      | Technology                          |
|------------|-------------------------------------|
| UI         | Blazor Server (ASP.NET Core 8)      |
| Database   | SQL Server / LocalDB (dev)          |
| ORM        | Entity Framework Core (code-first)  |
| Realtime   | SignalR                             |
| Email      | MailKit (SMTP)                      |
| Auth       | Cookie-based (admin) / URL token    |

---

## Prerequisites

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8)
- SQL Server LocalDB (included with Visual Studio, or install separately)
- Optional: Visual Studio 2022 or VS Code with C# Dev Kit

---

## LocalDB Setup

LocalDB is used automatically in Development mode. No setup required beyond having it installed.

Verify LocalDB is available:

```bash
sqllocaldb info
sqllocaldb start MSSQLLocalDB
```

---

## Connection String

`appsettings.Development.json` is pre-configured:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=(localdb)\\MSSQLLocalDB;Database=AndonDb;Trusted_Connection=True;MultipleActiveResultSets=true"
  }
}
```

For production, set the `ConnectionStrings__DefaultConnection` environment variable or update `appsettings.json`.

---

## Migration Commands

Run from the `AndonApp/AndonApp` project directory:

```bash
cd AndonApp/AndonApp

# Create migration (already included — only needed for schema changes)
dotnet ef migrations add InitialCreate

# Apply migrations and create database
dotnet ef database update
```

> **Note:** The app calls `MigrateAsync()` on startup, so `dotnet ef database update` is optional — the DB is created automatically on first run.

---

## Run Instructions

```bash
cd AndonApp/AndonApp
dotnet restore
dotnet run
```

The app starts at `https://localhost:5001` (or `http://localhost:5000`).

---

## Default Admin Login

| Field    | Value     |
|----------|-----------|
| Username | `admin`   |
| Password | `Admin@123` |

Admin URL: `https://localhost:5001/admin/login`

---

## Example Production Line URL

The seed data creates a sample production line. Its URL is:

```
https://localhost:5001/line/line-a?token=demo-token-linea-1234
```

To find the URL for any line: go to **Admin → Production Lines** and copy the URL shown in the table.

---

## Email Configuration

By default `EMAIL_MODE=LogOnly` — emails are logged to the console rather than sent.

To enable real email sending, set these environment variables (or add to `appsettings.json`):

```
EMAIL_MODE=Smtp
SMTP_HOST=smtp.example.com
SMTP_PORT=587
SMTP_USER=user@example.com
SMTP_PASS=yourpassword
EMAIL_FROM=andon@example.com
```

Email failures are caught and logged — they will **never** crash the application.

---

## Admin Features

| Route                                      | Purpose                        |
|--------------------------------------------|--------------------------------|
| `/admin/login`                             | Admin sign-in                  |
| `/admin/logout`                            | Admin sign-out                 |
| `/admin/andon-codes`                       | Manage ANDON codes             |
| `/admin/andon-codes/{id}/recipients`       | Manage email recipients        |
| `/admin/lines`                             | Manage production lines        |

---

## End-User Production Line

| Route                                 | Purpose                    |
|---------------------------------------|----------------------------|
| `/line/{slug}?token={AccessToken}`    | Live status board          |

- **GREEN** background = no open incidents (ALL OK)
- **AMBER** background = at least one AMBER incident, no RED
- **RED** background = at least one RED incident
- Multiple incidents are shown simultaneously
- Updates in real-time via SignalR (no page refresh needed)

---

## API Endpoints

### Admin (requires cookie auth)

```
GET    /api/admin/andon-codes
POST   /api/admin/andon-codes
PUT    /api/admin/andon-codes/{id}
DELETE /api/admin/andon-codes/{id}

GET    /api/admin/andon-codes/{id}/recipients
POST   /api/admin/andon-codes/{id}/recipients
DELETE /api/admin/andon-codes/{id}/recipients/{recipientId}

GET    /api/admin/lines
POST   /api/admin/lines
PUT    /api/admin/lines/{id}
DELETE /api/admin/lines/{id}
```

### End-User (requires `?token=` query param)

```
GET  /api/lines/{slug}/status?token=...
GET  /api/lines/{slug}/incidents?token=...
POST /api/lines/{slug}/incidents?token=...
POST /api/lines/{slug}/incidents/{id}/close?token=...
```

---

## Seed Data

On first startup the app seeds:

- **Admin user**: `admin` / `Admin@123`
- **ANDON codes**: MACH (Machine Fault), QUAL (Quality Issue), SAFE (Safety Alert), MATL (Material Shortage)
- **Production line**: Line A (slug: `line-a`, token: `demo-token-linea-1234`)

---

## Project Structure

```
AndonApp/
├── AndonApp.sln
└── AndonApp/
    ├── AndonApp.csproj
    ├── Program.cs
    ├── appsettings.json
    ├── appsettings.Development.json
    ├── Controllers/
    │   ├── AdminApiController.cs     # Admin REST API
    │   ├── AuthController.cs         # Auth REST API
    │   └── LineApiController.cs      # End-user REST API
    ├── Data/
    │   ├── AndonDbContext.cs
    │   ├── DbSeeder.cs
    │   ├── Migrations/
    │   └── Models/
    │       ├── AdminUser.cs
    │       ├── AndonCode.cs
    │       ├── AndonCodeRecipient.cs
    │       ├── Incident.cs
    │       └── ProductionLine.cs
    ├── Hubs/
    │   └── AndonHub.cs               # SignalR hub
    ├── Pages/                        # Razor Pages (login/logout only)
    │   ├── AdminLogin.cshtml
    │   └── AdminLogout.cshtml
    ├── Services/
    │   ├── EmailService.cs           # MailKit email
    │   └── IncidentService.cs        # Incident business logic
    ├── Components/                   # Blazor Server UI
    │   ├── App.razor
    │   ├── Routes.razor
    │   ├── _Imports.razor
    │   ├── Layout/
    │   │   ├── AdminLayout.razor
    │   │   └── MainLayout.razor
    │   └── Pages/
    │       ├── Index.razor
    │       ├── Admin/
    │       │   ├── AndonCodes.razor
    │       │   ├── AndonCodeRecipients.razor
    │       │   └── Lines.razor
    │       └── Line/
    │           └── LineStatus.razor
    └── wwwroot/
        └── css/
            └── app.css
```
