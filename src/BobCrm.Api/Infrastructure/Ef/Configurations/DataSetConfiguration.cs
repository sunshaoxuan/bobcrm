using BobCrm.Api.Base.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BobCrm.Api.Infrastructure.Ef.Configurations;

public class DataSetConfiguration : IEntityTypeConfiguration<DataSet>
{
    public void Configure(EntityTypeBuilder<DataSet> builder)
    {
        builder.HasKey(d => d.Id);

        builder.Property(d => d.Code)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(d => d.Name)
            .HasMaxLength(200)
            .IsRequired();

        // DisplayName 和 Description 使用 jsonb 存储多语文本
        // 这些配置需要在 AppDbContext.OnModelCreating 中统一设置 ValueConverter

        builder.Property(d => d.DataSourceTypeCode)
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(d => d.ConfigJson);

        builder.Property(d => d.FieldsJson);

        builder.Property(d => d.SupportsPaging)
            .HasDefaultValue(true);

        builder.Property(d => d.SupportsSorting)
            .HasDefaultValue(true);

        builder.Property(d => d.DefaultSortField)
            .HasMaxLength(100);

        builder.Property(d => d.DefaultSortDirection)
            .HasMaxLength(10)
            .HasDefaultValue("asc");

        builder.Property(d => d.DefaultPageSize)
            .HasDefaultValue(20);

        builder.Property(d => d.IsSystem)
            .HasDefaultValue(false);

        builder.Property(d => d.IsEnabled)
            .HasDefaultValue(true);

        builder.Property(d => d.CreatedAt)
            .IsRequired();

        builder.Property(d => d.CreatedBy)
            .HasMaxLength(450);

        builder.Property(d => d.UpdatedAt)
            .IsRequired();

        builder.Property(d => d.UpdatedBy)
            .HasMaxLength(450);

        // 关系配置
        builder.HasOne(d => d.QueryDefinition)
            .WithMany()
            .HasForeignKey(d => d.QueryDefinitionId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(d => d.PermissionFilter)
            .WithMany()
            .HasForeignKey(d => d.PermissionFilterId)
            .OnDelete(DeleteBehavior.SetNull);

        // 索引
        builder.HasIndex(d => d.Code)
            .IsUnique();

        builder.HasIndex(d => d.DataSourceTypeCode);

        builder.HasIndex(d => d.IsSystem);

        builder.HasIndex(d => d.IsEnabled);
    }
}
