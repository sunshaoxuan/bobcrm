using BobCrm.Api.Domain.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BobCrm.Api.Infrastructure.Configurations;

public class RoleAssignmentConfiguration : IEntityTypeConfiguration<RoleAssignment>
{
    public void Configure(EntityTypeBuilder<RoleAssignment> builder)
    {
        builder.ToTable("RoleAssignments");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.UserId).IsRequired().HasMaxLength(450);
        builder.HasIndex(x => new { x.UserId, x.RoleId, x.OrganizationId }).IsUnique();
    }
}
