using System.ComponentModel.DataAnnotations;

namespace AndonApp.Data.Models;

public class AdminUser
{
    public int Id { get; set; }

    [Required, MaxLength(100)]
    public string Username { get; set; } = string.Empty;

    [Required, MaxLength(200)]
    public string PasswordHash { get; set; } = string.Empty;
}
