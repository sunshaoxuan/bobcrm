using BobCrm.Api.Domain.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BobCrm.Api.Infrastructure.Configurations;

public class RoleDataScopeConfiguration : IEntityTypeConfiguration<RoleDataScope>
{
    public void Configure(EntityTypeBuilder<RoleDataScope> builder)
    {
        builder.ToTable("RoleDataScopes");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.EntityName).IsRequired().HasMaxLength(128);
        builder.Property(x => x.ScopeType).IsRequired().HasMaxLength(32);
        builder.Property(x => x.FilterExpression).HasMaxLength(512);
    }
}
