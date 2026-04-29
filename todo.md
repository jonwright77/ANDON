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

- [ ] **7. `LineSchedule.razor` is a dead stub**
  The file contains only `@* Replaced by LineSchedules.razor *@` and should be deleted.

## Low Priority / Notes

- [ ] **8. Access token exposed in URL query string**
  `/line/{slug}?token={AccessToken}` appears in server access logs, browser history, and HTTP Referer headers. Accepted trade-off per spec but worth revisiting for production hardening.

- [ ] **9. No brute-force protection on admin login**
  `AdminLogin.cshtml.cs` has no rate limiting or lockout on failed attempts. Consider ASP.NET Core's built-in IP rate limiting middleware for production.

- [ ] **10. `MigrateAsync()` runs on every startup**
  `DbSeeder.SeedAsync` calls `db.Database.MigrateAsync()` unconditionally. Fine for development; consider moving it to a separate startup step or CLI command for production deployments.

- [ ] **11. `AndonCode` model likely missing `CreatedAt` default**
  The migration creates a non-nullable `CreatedAt datetime2` column on `AndonCodes`, but `DbSeeder` does not set it and the model may not have a default. Verify `AndonCode.cs` has `public DateTime CreatedAt { get; set; } = DateTime.UtcNow;` to avoid inserting `0001-01-01`.
