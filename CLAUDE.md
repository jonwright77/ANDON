You are Claude Code acting as a senior full-stack engineer. Build a complete ANDON web application using .NET and SQL Server.

This system will be used in a manufacturing environment to raise and track production incidents (ANDON system). It must include an Admin area and an End-User production line status board with realtime updates.

----------------------------------
## TECH STACK (MANDATORY)
----------------------------------
- Backend: ASP.NET Core 8 (C#)
- UI: Blazor Server (use this for the entire UI)
- Database:
  - Development: SQL Server LocalDB (MSSQLLocalDB)
  - Production: SQL Server
- ORM: Entity Framework Core (code-first)
- Realtime: SignalR
- Email: MailKit (SMTP)
- Auth:
  - Admin: simple cookie-based authentication (seed a default admin user)
  - End-user: no login, secured via URL token

----------------------------------
## LOCALDB CONFIG (REQUIRED)
----------------------------------
Use this connection string in appsettings.Development.json:

Server=(localdb)\MSSQLLocalDB;Database=AndonDb;Trusted_Connection=True;MultipleActiveResultSets=true

Ensure:
- EF Core migrations work with LocalDB
- Include commands:
  - dotnet ef migrations add InitialCreate
  - dotnet ef database update

----------------------------------
## CORE CONCEPTS
----------------------------------

### ANDON Codes
Defined by Admin. Each has:
- Id
- Code (string)
- Name (optional)
- Description (optional)
- IsActive
- CreatedAt

Each ANDON code has MANY email recipients.

---

### Production Lines
Defined by Admin:
- Id
- Name
- Slug (unique, URL-safe)
- AccessToken (random string)
- IsActive

Each line has its own status screen.

---

### Incidents
Created by end users:
- Id
- ProductionLineId
- AndonCodeId
- Severity: AMBER or RED
- Status: OPEN or CLOSED
- AdditionalInfo (text)
- CreatedAt
- ClosedAt (nullable)

----------------------------------
## ADMIN FEATURES
----------------------------------

Admin UI routes:
- /admin/login
- /admin/andon-codes
- /admin/andon-codes/{id}/recipients
- /admin/lines

Admin can:
1. Create/edit ANDON codes
2. Assign email addresses to each code
3. Create/edit production lines
4. See generated end-user URL:
   /line/{slug}?token={AccessToken}

----------------------------------
## END USER (PRODUCTION LINE)
----------------------------------

Access:
- /line/{slug}?token={AccessToken}

Validate slug + token before allowing access.

---

### Status Screen Behaviour

IF no OPEN incidents:
- Background = GREEN
- Show large text: "ALL OK"

IF incidents exist:
- Background = worst severity:
  GREEN < AMBER < RED
- Display ALL open incidents:
  - Severity
  - ANDON code
  - Additional info
  - Created time

---

### Create Incident
Button: "New Incident"

Form:
- ANDON code dropdown
- Severity (AMBER or RED)
- Additional info textbox

On submit:
- Save incident
- Send email to all recipients of that ANDON code

---

### Close Incident
- Button on each incident
- Marks incident CLOSED
- Sets ClosedAt
- Sends email

---

### Multiple incidents must be supported simultaneously

----------------------------------
## EMAIL REQUIREMENTS
----------------------------------

Use MailKit.

Environment variables / config:
- SMTP_HOST
- SMTP_PORT
- SMTP_USER
- SMTP_PASS
- EMAIL_FROM
- EMAIL_MODE = LogOnly | Smtp

Emails sent:
1. On incident creation
2. On incident closure

Subject examples:
- [ANDON][RED] Line A – Code XYZ opened
- [ANDON][AMBER] Line A – Code XYZ closed

Email body must include:
- Production line
- Severity
- ANDON code
- Additional info
- Timestamp(s)

Failures must NOT crash the app—log them.

----------------------------------
## REALTIME (CRITICAL)
----------------------------------

Use SignalR.

- Create hub: AndonHub
- Clients join group: "line:{slug}"
- Broadcast when:
  - Incident created
  - Incident closed
- UI must update instantly without refresh

----------------------------------
## DATABASE SCHEMA
----------------------------------

Tables:

AndonCodes
AndonCodeRecipients (Id, AndonCodeId, Email)
ProductionLines (Slug UNIQUE)
Incidents

Indexes:
- ProductionLines.Slug UNIQUE
- AndonCodeRecipients (AndonCodeId, Email) UNIQUE
- Incidents (ProductionLineId, Status)
- Incidents (ProductionLineId, CreatedAt)

----------------------------------
## API ENDPOINTS
----------------------------------

Admin:
- GET /api/admin/andon-codes
- POST /api/admin/andon-codes
- PUT /api/admin/andon-codes/{id}
- DELETE /api/admin/andon-codes/{id}

- GET /api/admin/andon-codes/{id}/recipients
- POST /api/admin/andon-codes/{id}/recipients
- DELETE /api/admin/andon-codes/{id}/recipients/{recipientId}

- GET /api/admin/lines
- POST /api/admin/lines
- PUT /api/admin/lines/{id}
- DELETE /api/admin/lines/{id}

End-user:
- GET /api/lines/{slug}/status?token=...
- GET /api/lines/{slug}/incidents?token=...
- POST /api/lines/{slug}/incidents?token=...
- POST /api/lines/{slug}/incidents/{id}/close?token=...

----------------------------------
## UI REQUIREMENTS (BLAZOR SERVER)
----------------------------------

- Full screen status board (TV-friendly)
- Large “ALL OK” text
- Color-coded background
- Incident list
- Close buttons
- New incident modal/form
- Clean, simple layout

----------------------------------
## SECURITY
----------------------------------

- Admin routes require authentication
- Line view requires valid slug + token
- Validate all inputs
- Prevent cross-line access

----------------------------------
## DELIVERABLES
----------------------------------

- Complete .NET solution (AndonApp.sln)
- EF Core migrations
- Working Blazor UI
- SignalR integration
- Email service
- Seed data:
  - Admin user
  - Sample line
  - Sample ANDON codes

README must include:
- LocalDB setup
- Connection string
- Migration commands
- Run instructions
- Default admin login
- Example line URL

----------------------------------
## EXECUTION PLAN (FOLLOW THIS)
----------------------------------

1. Create solution + projects
2. Configure EF Core + LocalDB
3. Define models + migrations
4. Seed data
5. Build admin APIs + UI
6. Build incident APIs + email service
7. Implement SignalR
8. Build end-user UI
9. Polish + documentation

----------------------------------
## OUTPUT FORMAT
----------------------------------

Start by outputting:
1. Confirmation of stack (Blazor Server + LocalDB)
2. Database schema
3. API routes

Then:
- Show full project structure
- Provide complete code files
- Provide CLI commands:
  - dotnet new
  - dotnet ef
  - dotnet run

DO NOT leave placeholders.
Produce working, runnable code.

----------------------------------
## IMPORTANT
----------------------------------

Use exact terminology:
- ANDON
- Admin
- Production Line
- Incident
- AMBER
- RED
- ALL OK