using AndonApp.Data;
using AndonApp.Data.Models;
using AndonApp.Hubs;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace AndonApp.Services;

public interface IIncidentService
{
    Task<Incident> CreateIncidentAsync(int productionLineId, int andonCodeId, Severity severity, string? additionalInfo);
    Task<Incident?> CloseIncidentAsync(int incidentId, int productionLineId);
    Task<List<Incident>> GetOpenIncidentsAsync(int productionLineId);
}

public class IncidentService : IIncidentService
{
    private readonly AndonDbContext _db;
    private readonly IEmailService _email;
    private readonly IHubContext<AndonHub> _hub;
    private readonly ILogger<IncidentService> _logger;

    public IncidentService(AndonDbContext db, IEmailService email, IHubContext<AndonHub> hub, ILogger<IncidentService> logger)
    {
        _db = db;
        _email = email;
        _hub = hub;
        _logger = logger;
    }

    public async Task<Incident> CreateIncidentAsync(int productionLineId, int andonCodeId, Severity severity, string? additionalInfo)
    {
        var incident = new Incident
        {
            ProductionLineId = productionLineId,
            AndonCodeId = andonCodeId,
            Severity = severity,
            Status = IncidentStatus.OPEN,
            AdditionalInfo = additionalInfo,
            CreatedAt = DateTime.UtcNow
        };

        _db.Incidents.Add(incident);
        await _db.SaveChangesAsync();

        // Load navigation properties for email / SignalR
        await _db.Entry(incident).Reference(i => i.ProductionLine).LoadAsync();
        await _db.Entry(incident).Reference(i => i.AndonCode).LoadAsync();
        await _db.Entry(incident.AndonCode).Collection(c => c.Recipients).LoadAsync();

        // Broadcast via SignalR
        var slug = incident.ProductionLine.Slug;
        await _hub.Clients.Group($"line:{slug}").SendAsync("IncidentCreated", new IncidentSummaryDto(
            incident.Id,
            incident.Severity,
            incident.AndonCode.Code,
            incident.AndonCode.Name,
            incident.AdditionalInfo,
            incident.CreatedAt
        ));

        // Send email (non-blocking failure)
        try { await _email.SendIncidentOpenedAsync(incident); }
        catch (Exception ex) { _logger.LogError(ex, "Email failed for incident {Id}", incident.Id); }

        return incident;
    }

    public async Task<Incident?> CloseIncidentAsync(int incidentId, int productionLineId)
    {
        var incident = await _db.Incidents
            .Include(i => i.ProductionLine)
            .Include(i => i.AndonCode)
            .ThenInclude(c => c.Recipients)
            .FirstOrDefaultAsync(i => i.Id == incidentId && i.ProductionLineId == productionLineId && i.Status == IncidentStatus.OPEN);

        if (incident == null) return null;

        incident.Status = IncidentStatus.CLOSED;
        incident.ClosedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        var slug = incident.ProductionLine.Slug;
        await _hub.Clients.Group($"line:{slug}").SendAsync("IncidentClosed", incident.Id);

        try { await _email.SendIncidentClosedAsync(incident); }
        catch (Exception ex) { _logger.LogError(ex, "Email failed for incident close {Id}", incident.Id); }

        return incident;
    }

    public async Task<List<Incident>> GetOpenIncidentsAsync(int productionLineId)
    {
        return await _db.Incidents
            .Include(i => i.AndonCode)
            .Where(i => i.ProductionLineId == productionLineId && i.Status == IncidentStatus.OPEN)
            .OrderBy(i => i.CreatedAt)
            .ToListAsync();
    }
}
