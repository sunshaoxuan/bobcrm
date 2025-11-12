using BobCrm.Api.Domain.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BobCrm.Api.Infrastructure.Configurations;

public class RoleFunctionPermissionConfiguration : IEntityTypeConfiguration<RoleFunctionPermission>
{
    public void Configure(EntityTypeBuilder<RoleFunctionPermission> builder)
    {
        builder.ToTable("RoleFunctionPermissions");
        builder.HasKey(x => new { x.RoleId, x.FunctionId });

        builder.HasOne(x => x.Function)
            .WithMany(x => x.Roles)
            .HasForeignKey(x => x.FunctionId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
