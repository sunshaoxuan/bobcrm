using BobCrm.Api.Base.Models.Metadata;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BobCrm.Api.Infrastructure.Ef.Configurations;

public class DataSourceTypeEntryConfiguration : IEntityTypeConfiguration<DataSourceTypeEntry>
{
    public void Configure(EntityTypeBuilder<DataSourceTypeEntry> builder)
    {
        builder.HasKey(d => d.Id);

        builder.Property(d => d.Code)
            .HasMaxLength(50)
            .IsRequired();

        // DisplayName 和 Description 使用 jsonb 存储多语文本
        // 这些配置在 AppDbContext.OnModelCreating 中统一设置 ValueConverter

        builder.Property(d => d.HandlerType)
            .HasMaxLength(500)
            .IsRequired();

        builder.Property(d => d.ConfigSchema);

        builder.Property(d => d.Category)
            .HasMaxLength(100)
            .HasDefaultValue("General");

        builder.Property(d => d.Icon)
            .HasMaxLength(100);

        builder.Property(d => d.IsSystem)
            .HasDefaultValue(true);

        builder.Property(d => d.IsEnabled)
            .HasDefaultValue(true);

        builder.Property(d => d.SortOrder)
            .HasDefaultValue(100);

        builder.Property(d => d.CreatedAt)
            .IsRequired();

        builder.Property(d => d.UpdatedAt)
            .IsRequired();

        // 索引
        builder.HasIndex(d => d.Code)
            .IsUnique();

        builder.HasIndex(d => d.Category);

        builder.HasIndex(d => d.IsSystem);

        builder.HasIndex(d => d.IsEnabled);

        builder.HasIndex(d => d.SortOrder);
    }
}
