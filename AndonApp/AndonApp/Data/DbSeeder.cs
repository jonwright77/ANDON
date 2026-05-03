using AndonApp.Data.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AndonApp.Data;

public static class DbSeeder
{
    public static async Task SeedAsync(AndonDbContext db, ILogger logger)
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
                AccessToken = Guid.NewGuid().ToString("N") + Guid.NewGuid().ToString("N"),
                IsActive = true
            });
        }

        // Replace any line that still carries the insecure demo token from earlier seeds
        var insecureTokenLine = await db.ProductionLines
            .FirstOrDefaultAsync(l => l.AccessToken == "demo-token-linea-1234");
        if (insecureTokenLine != null)
            insecureTokenLine.AccessToken = Guid.NewGuid().ToString("N") + Guid.NewGuid().ToString("N");

        await db.SaveChangesAsync();

        // Warn loudly if the default admin password has never been changed
        var adminUser = await db.AdminUsers.FirstOrDefaultAsync(u => u.Username == "admin");
        if (adminUser != null && BCrypt.Net.BCrypt.Verify("Admin@123", adminUser.PasswordHash))
        {
            logger.LogWarning(
                "SECURITY WARNING: The admin account is still using the default password 'Admin@123'. " +
                "Change it immediately before deploying to production.");
        }
    }
}
