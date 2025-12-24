using BobCrm.Api.Base.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BobCrm.Api.Infrastructure.Configurations;

public class AuditLogConfiguration : IEntityTypeConfiguration<AuditLog>
{
    public void Configure(EntityTypeBuilder<AuditLog> builder)
    {
        builder.ToTable("AuditLogs");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Module)
            .HasColumnName("Category")
            .IsRequired()
            .HasMaxLength(64);

        builder.Property(x => x.OperationType)
            .HasColumnName("Action")
            .IsRequired()
            .HasMaxLength(64);

        builder.Property(x => x.Description)
            .HasMaxLength(256);

        builder.Property(x => x.ActorId)
            .HasMaxLength(128);

        builder.Property(x => x.ActorName)
            .HasMaxLength(128);

        builder.Property(x => x.IpAddress)
            .HasMaxLength(64);

        builder.Property(x => x.Target)
            .HasMaxLength(128);

        builder.Property(x => x.ContextJson)
            .HasColumnName("Payload");

        builder.Property(x => x.OccurredAt)
            .HasColumnName("CreatedAt");

        builder.HasIndex(x => x.Module).HasDatabaseName("IX_AuditLogs_Category");
        builder.HasIndex(x => x.OccurredAt).HasDatabaseName("IX_AuditLogs_CreatedAt");
        builder.HasIndex(x => new { x.Module, x.OperationType }).HasDatabaseName("IX_AuditLogs_Category_Action");
    }
}
