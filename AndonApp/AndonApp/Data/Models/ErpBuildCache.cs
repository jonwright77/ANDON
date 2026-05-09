using System.ComponentModel.DataAnnotations;

namespace AndonApp.Data.Models;

public class ErpBuildCache
{
    public int Id { get; set; }

    [Required, MaxLength(200)]
    public string Pool { get; set; } = string.Empty;

    public DateTime Timestamp { get; set; }

    public int Quantity { get; set; }
}
