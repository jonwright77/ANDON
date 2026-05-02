using System.ComponentModel.DataAnnotations;

namespace AndonApp.Data.Models;

public class LineType
{
    public int Id { get; set; }

    [Required, MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(500)]
    public string? Description { get; set; }

    public ICollection<ProductionLine> ProductionLines { get; set; } = new List<ProductionLine>();
}
