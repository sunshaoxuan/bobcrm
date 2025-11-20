using BobCrm.Api.Base.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BobCrm.Api.Infrastructure.Ef.Configurations;

/// <summary>
/// FieldPermission 实体配置
/// </summary>
public class FieldPermissionConfiguration : IEntityTypeConfiguration<FieldPermission>
{
    public void Configure(EntityTypeBuilder<FieldPermission> builder)
    {
        builder.ToTable("FieldPermissions");

        builder.HasKey(fp => fp.Id);

        // 索引：提高按角色和实体类型查询的性能
        builder.HasIndex(fp => new { fp.RoleId, fp.EntityType, fp.FieldName })
            .IsUnique()
            .HasDatabaseName("IX_FieldPermissions_Role_Entity_Field");

        // 外键关系
        builder.HasOne(fp => fp.Role)
            .WithMany(r => r.FieldPermissions)
            .HasForeignKey(fp => fp.RoleId)
            .OnDelete(DeleteBehavior.Cascade);

        // 字段配置
        builder.Property(fp => fp.EntityType)
            .IsRequired()
            .HasMaxLength(128);

        builder.Property(fp => fp.FieldName)
            .IsRequired()
            .HasMaxLength(128);

        builder.Property(fp => fp.Remarks)
            .HasMaxLength(512);

        builder.Property(fp => fp.CanRead)
            .HasDefaultValue(true);

        builder.Property(fp => fp.CanWrite)
            .HasDefaultValue(false);

        // 审计字段
        builder.Property(fp => fp.CreatedAt)
            .IsRequired();

        builder.Property(fp => fp.UpdatedAt)
            .IsRequired();

        builder.Property(fp => fp.CreatedBy)
            .HasMaxLength(128);

        builder.Property(fp => fp.UpdatedBy)
            .HasMaxLength(128);
    }
}
