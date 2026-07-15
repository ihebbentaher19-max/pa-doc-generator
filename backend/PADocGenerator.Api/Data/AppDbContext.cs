using Microsoft.EntityFrameworkCore;
using PADocGenerator.Api.Models.Entities;

namespace PADocGenerator.Api.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<ApplicationUser> Users => Set<ApplicationUser>();
    public DbSet<FlowImport> FlowImports => Set<FlowImport>();
    public DbSet<Documentation> Documentations => Set<Documentation>();
    public DbSet<DocumentationVersion> DocumentationVersions => Set<DocumentationVersion>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<ApplicationUser>(e =>
        {
            e.HasIndex(u => u.Email).IsUnique();
            e.Property(u => u.Email).HasMaxLength(256).IsRequired();
            e.Property(u => u.FullName).HasMaxLength(200).IsRequired();
            e.Property(u => u.Role).HasConversion<string>();
        });

        modelBuilder.Entity<FlowImport>(e =>
        {
            e.Property(f => f.Name).HasMaxLength(300).IsRequired();

            // PostgreSQL JSONB : cf. section 5 du cahier des charges - "PostgreSQL
            // permet de gérer efficacement ces données relationnelles tout en
            // offrant le type JSONB pour stocker et interroger les flux JSON
            // sans imposer de schéma spécifique."
            e.Property(f => f.RawJson).HasColumnType("jsonb");

            e.HasOne(f => f.ImportedByUser)
                .WithMany(u => u.ImportedFlows)
                .HasForeignKey(f => f.ImportedByUserId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Documentation>(e =>
        {
            e.Property(d => d.Title).HasMaxLength(300).IsRequired();
            e.Property(d => d.Status).HasConversion<string>();

            e.HasOne(d => d.FlowImport)
                .WithMany(f => f.Documentations)
                .HasForeignKey(d => d.FlowImportId)
                .OnDelete(DeleteBehavior.Cascade);

            e.HasOne(d => d.CreatedByUser)
                .WithMany()
                .HasForeignKey(d => d.CreatedByUserId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<DocumentationVersion>(e =>
        {
            e.Property(v => v.StructuredContentJson).HasColumnType("jsonb");

            e.HasOne(v => v.Documentation)
                .WithMany(d => d.Versions)
                .HasForeignKey(v => v.DocumentationId)
                .OnDelete(DeleteBehavior.Cascade);

            e.HasOne(v => v.EditedByUser)
                .WithMany()
                .HasForeignKey(v => v.EditedByUserId)
                .OnDelete(DeleteBehavior.Restrict);

            e.HasIndex(v => new { v.DocumentationId, v.VersionNumber }).IsUnique();
        });
    }
}
