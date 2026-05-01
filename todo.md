# AndonApp – Review TODO

## High Priority

- [x] **1. Scoped DbContext in Blazor Server circuits**
  Replaced `AddDbContext` with `AddDbContextFactory` in `Program.cs`. All five Blazor components (`AndonCodes.razor`, `Lines.razor`, `AndonCodeRecipients.razor`, `LineSchedules.razor`, `LineStatus.razor`) updated to inject `IDbContextFactory<AndonDbContext>` and create a short-lived `await using var db` per method instead of holding a single circuit-lifetime context.

- [x] **2. Silent failure on CloseIncident**
  Added `_closingId`, `_closeError`, and `_closeErrorId` fields. `CloseIncident()` now disables the button while in-flight and displays an inline error message on the specific incident card if the call fails.

- [x] **3. SignalR hub has no access control**
  `AndonHub` now injects `IDbContextFactory<AndonDbContext>`. `JoinLine` requires a `token` parameter and validates slug + token + IsActive against the DB before adding the connection to the group. Invalid requests throw `HubException`. `LineStatus.razor` updated to pass `Token` when invoking `JoinLine`.

- [x] **4. Weak email validation**
  Replaced `email.Contains('@')` with a `System.Net.Mail.MailAddress` constructor parse in `AndonCodeRecipients.razor`. Invalid addresses throw a `FormatException` which is caught to show the validation error.

## Medium Priority

- [x] **5. Duplicate data access paths (Admin UI vs Admin API)**
  Deleted `AdminApiController.cs` and its DTOs (`AndonCodeDto`, `RecipientDto`, `ProductionLineDto`). The Blazor admin pages are the authoritative data path; the API was unused dead code with no remaining references.

- [x] **6. SignalR broadcasts only incident ID, causing an extra DB round-trip**
  Created `IncidentSummaryDto` record in `Services/`. `IncidentService` now broadcasts the full DTO on `IncidentCreated`. `LineStatus.razor` updated to hold `List<IncidentSummaryDto>`, with the `IncidentCreated` handler adding directly to the list (sorted by `CreatedAt`) and `IncidentClosed` removing by ID — no DB round-trip on either event. `LoadIncidentsAsync` uses an EF Core `.Select()` projection instead of `Include`.

- [x] **7. `LineSchedule.razor` is a dead stub**
  File deleted. `LineSchedules.razor` at the same route (`/admin/lines/{Id:int}/schedule`) is the live implementation.

## New Functionality

- [x] **A. Working minutes display in title bar**
  Added `GetTotalWorkingMinutes()` — returns null if no workday schedule, otherwise `(EndTime - StartTime)` minus sum of all break durations. Added `FormatMinutes()` to render as `8h 30m` / `45m`. Displayed in the top bar as a "Scheduled" tile; hidden when no schedule is set. Updates automatically each clock tick via `_todaySchedule`.

- [x] **B. Elapsed working minutes display in title bar**
  Added `GetElapsedMinutes()` — `(Now - StartTime)` in minutes, clamped to 0 before shift start and to total shift duration after shift end, no break deduction. Displayed as "ELAPSED" in the second stat card, formatted via the shared `FormatMinutes()` helper. Updates every clock tick.

- [x] **C. Target Calendar per Production Line**
  Allow admins to set a daily build target for each production line, stored in the database and viewed as a month-at-a-time calendar.

  **New model:** `LineTarget` — `Id`, `ProductionLineId` (FK), `Date` (date only, no time), `Target` (int). Unique index on `(ProductionLineId, Date)`.

  **New migration:** add `LineTargets` table.

  **Admin UI:** new page at `/admin/lines/{Id}/targets` linked from the Lines table (alongside the existing Schedule button). Displays a month calendar grid — each day cell shows the current target (if set) and is clickable to set or edit the value inline. Month navigation (previous/next) to move between months.

  **Line status board:** show today's target value in the top bar alongside the clock and work status, fetched from `LineTargets` for the current line and today's date. If no target is set for today, display nothing.

  **Data notes:** `Date` should be stored as `DateOnly` (EF Core 6+ supports this natively for SQL Server `date` columns). Targets are optional per day — days with no row have no target.

- [x] **D. Expected build count card**
  Show an "EXPECTED" value in the fourth stat card. Calculated as `(Elapsed ÷ Scheduled) × Target`, giving the number of units that should have been built by this point in the day based on linear progress through the shift. Requires all three of Elapsed, Scheduled, and Target to be available — display `–` if any are missing or Scheduled is zero. Round to the nearest whole number. Updates every clock tick alongside Elapsed.

- [x] **E. Line Target button on production line status board**
  Add a "Line Target" button to the bottom-left of the `LineStatus.razor` screen, mirroring the position of the existing "+ New Incident" button on the bottom-right. Clicking it opens a modal allowing the end user to enter a target value for the current day. The value is saved to the existing `LineTargets` table (same model used by the admin Target Calendar), so it appears in both places.

  **Behaviour:**
  - If a target already exists for today, the modal pre-populates with the current value so it can be updated or cleared.
  - On save, `_todayTarget` is updated immediately so the TARGET and EXPECTED cards refresh without a page reload.
  - No admin login required — the line access token is sufficient authorisation.
  - The admin Target Calendar should display any target set via this button unchanged — no schema or calendar UI changes needed.

## Deferred

- [ ] **9. No brute-force protection on admin login**
  `AdminLogin.cshtml.cs` has no rate limiting or lockout on failed attempts. Consider ASP.NET Core's built-in IP rate limiting middleware for production.

- [ ] **10. `MigrateAsync()` runs on every startup**
  `DbSeeder.SeedAsync` calls `db.Database.MigrateAsync()` unconditionally. Fine for development; consider moving it to a separate startup step or CLI command for production deployments.

- [ ] **11. `AndonCode` model likely missing `CreatedAt` default**
  The migration creates a non-nullable `CreatedAt datetime2` column on `AndonCodes`, but `DbSeeder` does not set it and the model may not have a default. Verify `AndonCode.cs` has `public DateTime CreatedAt { get; set; } = DateTime.UtcNow;` to avoid inserting `0001-01-01`.

- [ ] **8. Access token exposed in URL query string**
  `/line/{slug}?token={AccessToken}` appears in server access logs, browser history, and HTTP Referer headers. The spec defines this URL format so the link generation can't change, but exposure can be reduced after the initial load.
  **Proposed approach:** use JS `history.replaceState()` via interop after the token is validated to strip it from the browser URL bar. This prevents the token appearing in browser history and Referer headers on subsequent navigation. Server access logs will still record it on the first request — that is unavoidable without a server-side token-exchange flow.
  **To implement:** inject `IJSRuntime` into `LineStatus.razor`, and in `OnAfterRenderAsync` (first render, authorized) call `JS.InvokeVoidAsync("history.replaceState", null, "", $"/line/{Slug}")`. The `Token` value is already held in component memory so all subsequent operations continue to work.
