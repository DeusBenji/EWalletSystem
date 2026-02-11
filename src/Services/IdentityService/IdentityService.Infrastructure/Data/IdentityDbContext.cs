using IdentityService.Domain.Entities;
using IdentityService.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;

namespace IdentityService.Infrastructure.Data;

/// <summary>
/// Database context for IdentityService.
/// Manages policy definitions, attestations, and audit logs.
/// </summary>
public class IdentityDbContext : DbContext
{
    public IdentityDbContext(DbContextOptions<IdentityDbContext> options)
        : base(options)
    {
    }

    public DbSet<PolicyDefinition> PolicyDefinitions => Set<PolicyDefinition>();
    public DbSet<PolicyAttestation> PolicyAttestations => Set<PolicyAttestation>();
    public DbSet<PolicyAuditLog> PolicyAuditLogs => Set<PolicyAuditLog>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // PolicyDefinition configuration
        modelBuilder.Entity<PolicyDefinition>(entity =>
        {
            entity.HasKey(e => new { e.PolicyId, e.Version });
            
            entity.Property(e => e.PolicyId)
                .HasMaxLength(100)
                .IsRequired();
            
            entity.Property(e => e.Version)
                .HasMaxLength(20)
                .IsRequired();
            
            entity.Property(e => e.CircuitId)
                .HasMaxLength(100)
                .IsRequired();
            
            entity.Property(e => e.VerificationKeyId)
                .HasMaxLength(100)
                .IsRequired();
            
            entity.Property(e => e.VerificationKeyFingerprint)
                .HasMaxLength(200)
                .IsRequired();
            
            entity.Property(e => e.CompatibleVersions)
                .HasMaxLength(100)
                .IsRequired();
            
            entity.Property(e => e.DefaultExpiry)
                .HasMaxLength(20)
                .IsRequired();
            
            entity.Property(e => e.RequiredPublicSignalsSchema)
                .HasColumnType("nvarchar(max)")
                .IsRequired();
            
            entity.Property(e => e.Signature)
                .HasMaxLength(500);
            
            entity.HasIndex(e => e.Status);
            entity.HasIndex(e => e.CreatedAt);
        });

        // PolicyAttestation configuration
        modelBuilder.Entity<PolicyAttestation>(entity =>
        {
            entity.HasKey(e => e.Id);
            
            entity.Property(e => e.PolicyId)
                .HasMaxLength(100)
                .IsRequired();
            
            entity.Property(e => e.SubjectId)
                .HasMaxLength(200)
                .IsRequired();
            
            entity.Property(e => e.ProviderId)
                .HasMaxLength(50)
                .IsRequired();
            
            entity.Property(e => e.AssuranceLevel)
                .HasMaxLength(50)
                .IsRequired();
            
            entity.Property(e => e.PolicyHash)
                .HasMaxLength(200);
            
            entity.Property(e => e.Metadata)
                .HasColumnType("nvarchar(max)");
            
            // Indexes for common queries
            entity.HasIndex(e => new { e.SubjectId, e.PolicyId });
            entity.HasIndex(e => e.ExpiresAt);
            entity.HasIndex(e => e.VerifiedAt);
        });

        // PolicyAuditLog configuration
        modelBuilder.Entity<PolicyAuditLog>(entity =>
        {
            entity.HasKey(e => e.Id);
            
            entity.Property(e => e.PolicyId)
                .HasMaxLength(100)
                .IsRequired();
            
            entity.Property(e => e.Version)
                .HasMaxLength(20)
                .IsRequired();
            
            entity.Property(e => e.OldStatus)
                .HasMaxLength(50)
                .IsRequired();
            
            entity.Property(e => e.NewStatus)
                .HasMaxLength(50)
                .IsRequired();
            
            entity.Property(e => e.Reason)
                .HasMaxLength(500)
                .IsRequired();
            
            entity.Property(e => e.Actor)
                .HasMaxLength(200)
                .IsRequired();
            
            entity.Property(e => e.Signature)
                .HasMaxLength(500);
            
            // Audit logs are append-only, indexed by timestamp
            entity.HasIndex(e => e.Timestamp);
            entity.HasIndex(e => new { e.PolicyId, e.Version });
        });
    }
}
