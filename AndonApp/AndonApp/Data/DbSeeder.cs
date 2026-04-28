using AndonApp.Data.Models;
using Microsoft.EntityFrameworkCore;

namespace AndonApp.Data;

public static class DbSeeder
{
    public static async Task SeedAsync(AndonDbContext db)
    {
        await db.Database.MigrateAsync();

        // Seed admin user (password: Admin@123)
        if (!await db.AdminUsers.AnyAsync())
        {
            db.AdminUsers.Add(new AdminUser
            {
                Username = "admin",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("Admin@123")
            });
        }

        // Seed ANDON codes
        if (!await db.AndonCodes.AnyAsync())
        {
            var codes = new[]
            {
                new AndonCode { Code = "MACH", Name = "Machine Fault", Description = "Machine breakdown or malfunction", IsActive = true },
                new AndonCode { Code = "QUAL", Name = "Quality Issue", Description = "Quality defect detected on line", IsActive = true },
                new AndonCode { Code = "SAFE", Name = "Safety Alert", Description = "Safety concern requiring immediate attention", IsActive = true },
                new AndonCode { Code = "MATL", Name = "Material Shortage", Description = "Materials required to continue production", IsActive = true },
            };
            db.AndonCodes.AddRange(codes);
        }

        // Seed production line
        if (!await db.ProductionLines.AnyAsync())
        {
            db.ProductionLines.Add(new ProductionLine
            {
                Name = "Line A",
                Slug = "line-a",
                AccessToken = "demo-token-linea-1234",
                IsActive = true
            });
        }

        await db.SaveChangesAsync();
    }
}
