using System.ComponentModel.DataAnnotations;

namespace AndonApp.Data.Models;

public enum Severity { AMBER, RED }
public enum IncidentStatus { OPEN, CLOSED }

public class Incident
{
    public int Id { get; set; }

    public int ProductionLineId { get; set; }
    public ProductionLine ProductionLine { get; set; } = null!;

    public int AndonCodeId { get; set; }
    public AndonCode AndonCode { get; set; } = null!;

    public Severity Severity { get; set; }

    public IncidentStatus Status { get; set; } = IncidentStatus.OPEN;

    [MaxLength(1000)]
    public string? AdditionalInfo { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? ClosedAt { get; set; }
}
