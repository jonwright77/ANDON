using System.ComponentModel.DataAnnotations;

namespace AndonApp.Data.Models;

public class AndonCodeRecipient
{
    public int Id { get; set; }

    public int AndonCodeId { get; set; }
    public AndonCode AndonCode { get; set; } = null!;

    [Required, MaxLength(254), EmailAddress]
    public string Email { get; set; } = string.Empty;
}
