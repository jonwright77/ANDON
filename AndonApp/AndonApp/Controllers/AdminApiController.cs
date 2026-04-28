using AndonApp.Data;
using AndonApp.Data.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AndonApp.Controllers;

[ApiController]
[Route("api/admin")]
[Authorize(Policy = "AdminOnly")]
public class AdminApiController : ControllerBase
{
    private readonly AndonDbContext _db;

    public AdminApiController(AndonDbContext db) => _db = db;

    // ---- ANDON CODES ----

    [HttpGet("andon-codes")]
    public async Task<IActionResult> GetAndonCodes()
    {
        var codes = await _db.AndonCodes
            .Include(c => c.Recipients)
            .OrderBy(c => c.Code)
            .ToListAsync();
        return Ok(codes);
    }

    [HttpPost("andon-codes")]
    public async Task<IActionResult> CreateAndonCode([FromBody] AndonCodeDto dto)
    {
        if (await _db.AndonCodes.AnyAsync(c => c.Code == dto.Code))
            return BadRequest($"Code '{dto.Code}' already exists.");

        var code = new AndonCode
        {
            Code = dto.Code.Trim().ToUpper(),
            Name = dto.Name?.Trim(),
            Description = dto.Description?.Trim(),
            IsActive = dto.IsActive
        };
        _db.AndonCodes.Add(code);
        await _db.SaveChangesAsync();
        return CreatedAtAction(nameof(GetAndonCodes), new { id = code.Id }, code);
    }

    [HttpPut("andon-codes/{id:int}")]
    public async Task<IActionResult> UpdateAndonCode(int id, [FromBody] AndonCodeDto dto)
    {
        var code = await _db.AndonCodes.FindAsync(id);
        if (code == null) return NotFound();

        if (await _db.AndonCodes.AnyAsync(c => c.Code == dto.Code && c.Id != id))
            return BadRequest($"Code '{dto.Code}' already exists.");

        code.Code = dto.Code.Trim().ToUpper();
        code.Name = dto.Name?.Trim();
        code.Description = dto.Description?.Trim();
        code.IsActive = dto.IsActive;
        await _db.SaveChangesAsync();
        return Ok(code);
    }

    [HttpDelete("andon-codes/{id:int}")]
    public async Task<IActionResult> DeleteAndonCode(int id)
    {
        var code = await _db.AndonCodes.FindAsync(id);
        if (code == null) return NotFound();
        if (await _db.Incidents.AnyAsync(i => i.AndonCodeId == id))
            return BadRequest("Cannot delete: incidents exist for this code.");
        _db.AndonCodes.Remove(code);
        await _db.SaveChangesAsync();
        return NoContent();
    }

    // ---- RECIPIENTS ----

    [HttpGet("andon-codes/{id:int}/recipients")]
    public async Task<IActionResult> GetRecipients(int id)
    {
        if (!await _db.AndonCodes.AnyAsync(c => c.Id == id)) return NotFound();
        var recipients = await _db.AndonCodeRecipients
            .Where(r => r.AndonCodeId == id)
            .ToListAsync();
        return Ok(recipients);
    }

    [HttpPost("andon-codes/{id:int}/recipients")]
    public async Task<IActionResult> AddRecipient(int id, [FromBody] RecipientDto dto)
    {
        if (!await _db.AndonCodes.AnyAsync(c => c.Id == id)) return NotFound();

        var email = dto.Email.Trim().ToLowerInvariant();
        if (await _db.AndonCodeRecipients.AnyAsync(r => r.AndonCodeId == id && r.Email == email))
            return BadRequest("Email already exists for this code.");

        var recipient = new AndonCodeRecipient { AndonCodeId = id, Email = email };
        _db.AndonCodeRecipients.Add(recipient);
        await _db.SaveChangesAsync();
        return Ok(recipient);
    }

    [HttpDelete("andon-codes/{id:int}/recipients/{recipientId:int}")]
    public async Task<IActionResult> DeleteRecipient(int id, int recipientId)
    {
        var recipient = await _db.AndonCodeRecipients
            .FirstOrDefaultAsync(r => r.Id == recipientId && r.AndonCodeId == id);
        if (recipient == null) return NotFound();
        _db.AndonCodeRecipients.Remove(recipient);
        await _db.SaveChangesAsync();
        return NoContent();
    }

    // ---- PRODUCTION LINES ----

    [HttpGet("lines")]
    public async Task<IActionResult> GetLines()
    {
        var lines = await _db.ProductionLines.OrderBy(l => l.Name).ToListAsync();
        return Ok(lines);
    }

    [HttpPost("lines")]
    public async Task<IActionResult> CreateLine([FromBody] ProductionLineDto dto)
    {
        var slug = dto.Slug.Trim().ToLower();
        if (await _db.ProductionLines.AnyAsync(l => l.Slug == slug))
            return BadRequest($"Slug '{slug}' already exists.");

        var line = new ProductionLine
        {
            Name = dto.Name.Trim(),
            Slug = slug,
            AccessToken = Guid.NewGuid().ToString("N") + Guid.NewGuid().ToString("N"),
            IsActive = dto.IsActive
        };
        _db.ProductionLines.Add(line);
        await _db.SaveChangesAsync();
        return Ok(line);
    }

    [HttpPut("lines/{id:int}")]
    public async Task<IActionResult> UpdateLine(int id, [FromBody] ProductionLineDto dto)
    {
        var line = await _db.ProductionLines.FindAsync(id);
        if (line == null) return NotFound();

        var slug = dto.Slug.Trim().ToLower();
        if (await _db.ProductionLines.AnyAsync(l => l.Slug == slug && l.Id != id))
            return BadRequest($"Slug '{slug}' already exists.");

        line.Name = dto.Name.Trim();
        line.Slug = slug;
        line.IsActive = dto.IsActive;
        await _db.SaveChangesAsync();
        return Ok(line);
    }

    [HttpDelete("lines/{id:int}")]
    public async Task<IActionResult> DeleteLine(int id)
    {
        var line = await _db.ProductionLines.FindAsync(id);
        if (line == null) return NotFound();
        if (await _db.Incidents.AnyAsync(i => i.ProductionLineId == id))
            return BadRequest("Cannot delete: incidents exist for this line.");
        _db.ProductionLines.Remove(line);
        await _db.SaveChangesAsync();
        return NoContent();
    }
}

// ---- DTOs ----

public record AndonCodeDto(string Code, string? Name, string? Description, bool IsActive = true);
public record RecipientDto(string Email);
public record ProductionLineDto(string Name, string Slug, bool IsActive = true);
