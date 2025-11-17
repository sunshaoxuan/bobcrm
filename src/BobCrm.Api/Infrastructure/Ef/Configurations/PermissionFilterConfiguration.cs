using BobCrm.Api.Base.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BobCrm.Api.Infrastructure.Ef.Configurations;

public class PermissionFilterConfiguration : IEntityTypeConfiguration<PermissionFilter>
{
    public void Configure(EntityTypeBuilder<PermissionFilter> builder)
    {
        builder.HasKey(p => p.Id);

        builder.Property(p => p.Code)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(p => p.Name)
            .HasMaxLength(200)
            .IsRequired();

        // DisplayName 和 Description 使用 jsonb 存储多语文本
        // 这些配置需要在 AppDbContext.OnModelCreating 中统一设置 ValueConverter

        builder.Property(p => p.RequiredFunctionCode)
            .HasMaxLength(100);

        builder.Property(p => p.DataScopeTag)
            .HasMaxLength(100);

        builder.Property(p => p.EntityType)
            .HasMaxLength(100);

        builder.Property(p => p.FilterRulesJson);

        builder.Property(p => p.EnableFieldLevelPermissions)
            .HasDefaultValue(false);

        builder.Property(p => p.FieldPermissionsJson);

        builder.Property(p => p.IsSystem)
            .HasDefaultValue(false);

        builder.Property(p => p.IsEnabled)
            .HasDefaultValue(true);

        builder.Property(p => p.CreatedAt)
            .IsRequired();

        builder.Property(p => p.CreatedBy)
            .HasMaxLength(450);

        builder.Property(p => p.UpdatedAt)
            .IsRequired();

        builder.Property(p => p.UpdatedBy)
            .HasMaxLength(450);

        // 索引
        builder.HasIndex(p => p.Code)
            .IsUnique();

        builder.HasIndex(p => p.RequiredFunctionCode);

        builder.HasIndex(p => p.DataScopeTag);

        builder.HasIndex(p => p.EntityType);

        builder.HasIndex(p => p.IsSystem);

        builder.HasIndex(p => p.IsEnabled);
    }
}
