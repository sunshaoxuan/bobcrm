using BobCrm.Api.Base.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BobCrm.Api.Infrastructure.Ef.Configurations;

public class QueryDefinitionConfiguration : IEntityTypeConfiguration<QueryDefinition>
{
    public void Configure(EntityTypeBuilder<QueryDefinition> builder)
    {
        builder.HasKey(q => q.Id);

        builder.Property(q => q.Code)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(q => q.Name)
            .HasMaxLength(200)
            .IsRequired();

        // DisplayName 和 Description 使用 jsonb 存储多语文本
        // 这些配置需要在 AppDbContext.OnModelCreating 中统一设置 ValueConverter

        builder.Property(q => q.ConditionsJson);

        builder.Property(q => q.ParametersJson);

        builder.Property(q => q.AggregationsJson);

        builder.Property(q => q.GroupByFields)
            .HasMaxLength(500);

        builder.Property(q => q.IsEnabled)
            .HasDefaultValue(true);

        builder.Property(q => q.CreatedAt)
            .IsRequired();

        builder.Property(q => q.CreatedBy)
            .HasMaxLength(450);

        builder.Property(q => q.UpdatedAt)
            .IsRequired();

        builder.Property(q => q.UpdatedBy)
            .HasMaxLength(450);

        // 索引
        builder.HasIndex(q => q.Code)
            .IsUnique();

        builder.HasIndex(q => q.IsEnabled);
    }
}
