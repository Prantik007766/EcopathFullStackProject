using EcoPath.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace EcoPath.Infrastructure.Data;

public class EcoPathDbContext : DbContext
{
    public EcoPathDbContext(DbContextOptions<EcoPathDbContext> options) : base(options) { }

    public DbSet<Mine> Mines => Set<Mine>();
    public DbSet<EmissionFactor> EmissionFactors => Set<EmissionFactor>();
    public DbSet<Report> Reports => Set<Report>();
    public DbSet<CalcEntry> CalcEntries => Set<CalcEntry>();
    public DbSet<PathwaysEntry> PathwaysEntries => Set<PathwaysEntry>();
    public DbSet<OffsetEntry> OffsetEntries => Set<OffsetEntry>();
    public DbSet<Profile> Profiles => Set<Profile>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.Entity<Mine>(e =>
        {
            e.Property(p => p.Name).HasMaxLength(120).IsRequired();
            e.Property(p => p.Location).HasMaxLength(160);
        });
        modelBuilder.Entity<EmissionFactor>(e =>
        {
            e.Property(p => p.Code).HasMaxLength(64).IsRequired();
            e.Property(p => p.Unit).HasMaxLength(64).IsRequired();
        });

        modelBuilder.Entity<Profile>(e =>
        {
            e.Property(p => p.MineName).HasMaxLength(160).IsRequired();
            e.Property(p => p.MineId).HasMaxLength(64).IsRequired();
            e.Property(p => p.Location).HasMaxLength(160);
            e.Property(p => p.Area).HasMaxLength(64);
            e.Property(p => p.Email).HasMaxLength(160);
            e.Property(p => p.Phone).HasMaxLength(40);
        });

        modelBuilder.Entity<Report>(e =>
        {
            e.Property(p => p.Title).HasMaxLength(160).IsRequired();
        });
        modelBuilder.Entity<CalcEntry>(e =>
        {
            e.Property(p => p.Activity).HasMaxLength(160).IsRequired();
            e.Property(p => p.Unit).HasMaxLength(64).IsRequired();
            e.HasOne(p => p.Report)
             .WithMany(r => r.CalcEntries)
             .HasForeignKey(p => p.ReportId)
             .OnDelete(DeleteBehavior.Cascade);
        });
        modelBuilder.Entity<PathwaysEntry>(e =>
        {
            e.HasOne(p => p.Report)
             .WithMany(r => r.PathwaysEntries)
             .HasForeignKey(p => p.ReportId)
             .OnDelete(DeleteBehavior.Cascade);
        });
        modelBuilder.Entity<OffsetEntry>(e =>
        {
            e.HasOne(p => p.Report)
             .WithMany(r => r.OffsetEntries)
             .HasForeignKey(p => p.ReportId)
             .OnDelete(DeleteBehavior.Cascade);
        });
    }
}
