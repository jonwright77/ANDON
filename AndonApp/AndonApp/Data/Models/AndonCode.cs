using System.ComponentModel.DataAnnotations;

namespace AndonApp.Data.Models;

public class AndonCode
{
    public int Id { get; set; }

    [Required, MaxLength(50)]
    public string Code { get; set; } = string.Empty;

    [MaxLength(100)]
    public string? Name { get; set; }

    [MaxLength(500)]
    public string? Description { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<AndonCodeRecipient> Recipients { get; set; } = new List<AndonCodeRecipient>();
    public ICollection<Incident> Incidents { get; set; } = new List<Incident>();
}
