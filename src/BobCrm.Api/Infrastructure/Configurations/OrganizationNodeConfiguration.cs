using BobCrm.Api.Domain.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BobCrm.Api.Infrastructure.Configurations;

public class OrganizationNodeConfiguration : IEntityTypeConfiguration<OrganizationNode>
{
    public void Configure(EntityTypeBuilder<OrganizationNode> builder)
    {
        builder.ToTable("OrganizationNodes");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Code).IsRequired().HasMaxLength(64);
        builder.Property(x => x.Name).IsRequired().HasMaxLength(128);
        builder.Property(x => x.PathCode).IsRequired().HasMaxLength(256);
        builder.Property(x => x.SortOrder).HasDefaultValue(100);
        builder.Property(x => x.Level).HasDefaultValue(0);

        builder.HasIndex(x => new { x.ParentId, x.Code }).IsUnique();
        builder.HasIndex(x => x.PathCode).IsUnique();

        builder.HasMany(x => x.Children)
            .WithOne(x => x.Parent!)
            .HasForeignKey(x => x.ParentId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
