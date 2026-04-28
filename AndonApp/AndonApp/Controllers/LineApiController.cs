using AndonApp.Data;
using AndonApp.Data.Models;
using AndonApp.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AndonApp.Controllers;

[ApiController]
[Route("api/lines")]
public class LineApiController : ControllerBase
{
    private readonly AndonDbContext _db;
    private readonly IIncidentService _incidents;

    public LineApiController(AndonDbContext db, IIncidentService incidents)
    {
        _db = db;
        _incidents = incidents;
    }

    private async Task<ProductionLine?> AuthorizeLineAsync(string slug, string? token)
    {
        if (string.IsNullOrWhiteSpace(token)) return null;
        return await _db.ProductionLines
            .FirstOrDefaultAsync(l => l.Slug == slug && l.AccessToken == token && l.IsActive);
    }

    [HttpGet("{slug}/status")]
    public async Task<IActionResult> GetStatus(string slug, [FromQuery] string? token)
    {
        var line = await AuthorizeLineAsync(slug, token);
        if (line == null) return Unauthorized("Invalid line or token.");

        var open = await _incidents.GetOpenIncidentsAsync(line.Id);
        var worst = open.Any()
            ? open.Any(i => i.Severity == Severity.RED) ? "RED" : "AMBER"
            : "GREEN";

        return Ok(new { status = worst, openCount = open.Count });
    }

    [HttpGet("{slug}/incidents")]
    public async Task<IActionResult> GetIncidents(string slug, [FromQuery] string? token)
    {
        var line = await AuthorizeLineAsync(slug, token);
        if (line == null) return Unauthorized("Invalid line or token.");

        var open = await _incidents.GetOpenIncidentsAsync(line.Id);
        return Ok(open);
    }

    [HttpPost("{slug}/incidents")]
    public async Task<IActionResult> CreateIncident(
        string slug,
        [FromQuery] string? token,
        [FromBody] CreateIncidentDto dto)
    {
        var line = await AuthorizeLineAsync(slug, token);
        if (line == null) return Unauthorized("Invalid line or token.");

        var code = await _db.AndonCodes.FindAsync(dto.AndonCodeId);
        if (code == null || !code.IsActive) return BadRequest("Invalid ANDON code.");

        if (!Enum.TryParse<Severity>(dto.Severity, true, out var severity))
            return BadRequest("Severity must be AMBER or RED.");

        var incident = await _incidents.CreateIncidentAsync(
            line.Id, dto.AndonCodeId, severity, dto.AdditionalInfo);

        return Ok(new { incident.Id, incident.CreatedAt });
    }

    [HttpPost("{slug}/incidents/{id:int}/close")]
    public async Task<IActionResult> CloseIncident(string slug, int id, [FromQuery] string? token)
    {
        var line = await AuthorizeLineAsync(slug, token);
        if (line == null) return Unauthorized("Invalid line or token.");

        var incident = await _incidents.CloseIncidentAsync(id, line.Id);
        if (incident == null) return NotFound("Incident not found or already closed.");

        return Ok(new { incident.Id, incident.ClosedAt });
    }
}

public record CreateIncidentDto(int AndonCodeId, string Severity, string? AdditionalInfo);
