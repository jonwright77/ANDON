using AndonApp.Data.Models;
using Microsoft.EntityFrameworkCore;

namespace AndonApp.Data;

public class AndonDbContext : DbContext
{
    public AndonDbContext(DbContextOptions<AndonDbContext> options) : base(options) { }

    public DbSet<AndonCode> AndonCodes => Set<AndonCode>();
    public DbSet<AndonCodeRecipient> AndonCodeRecipients => Set<AndonCodeRecipient>();
    public DbSet<ProductionLine> ProductionLines => Set<ProductionLine>();
    public DbSet<Incident> Incidents => Set<Incident>();
    public DbSet<AdminUser> AdminUsers => Set<AdminUser>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // ProductionLines: unique slug
        modelBuilder.Entity<ProductionLine>()
            .HasIndex(l => l.Slug)
            .IsUnique();

        // AndonCodeRecipients: unique (AndonCodeId, Email)
        modelBuilder.Entity<AndonCodeRecipient>()
            .HasIndex(r => new { r.AndonCodeId, r.Email })
            .IsUnique();

        // Incidents: composite indexes
        modelBuilder.Entity<Incident>()
            .HasIndex(i => new { i.ProductionLineId, i.Status });

        modelBuilder.Entity<Incident>()
            .HasIndex(i => new { i.ProductionLineId, i.CreatedAt });

        // Enums as strings for readability
        modelBuilder.Entity<Incident>()
            .Property(i => i.Severity)
            .HasConversion<string>()
            .HasMaxLength(20);

        modelBuilder.Entity<Incident>()
            .Property(i => i.Status)
            .HasConversion<string>()
            .HasMaxLength(20);

        // Relationships
        modelBuilder.Entity<Incident>()
            .HasOne(i => i.ProductionLine)
            .WithMany(l => l.Incidents)
            .HasForeignKey(i => i.ProductionLineId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Incident>()
            .HasOne(i => i.AndonCode)
            .WithMany(c => c.Incidents)
            .HasForeignKey(i => i.AndonCodeId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<AndonCodeRecipient>()
            .HasOne(r => r.AndonCode)
            .WithMany(c => c.Recipients)
            .HasForeignKey(r => r.AndonCodeId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
