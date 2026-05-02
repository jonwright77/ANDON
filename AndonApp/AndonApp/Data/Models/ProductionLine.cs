using System.ComponentModel.DataAnnotations;

namespace AndonApp.Data.Models;

public class ProductionLine
{
    public int Id { get; set; }

    [Required, MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    [Required, MaxLength(100)]
    public string Slug { get; set; } = string.Empty;

    [Required, MaxLength(200)]
    public string AccessToken { get; set; } = string.Empty;

    public bool IsActive { get; set; } = true;

    [MaxLength(100)]
    public string? Pool { get; set; }

    public int? LineTypeId { get; set; }
    public LineType? LineType { get; set; }

    public ICollection<Incident> Incidents { get; set; } = new List<Incident>();
    public ICollection<LineSchedule> Schedules { get; set; } = new List<LineSchedule>();
}
