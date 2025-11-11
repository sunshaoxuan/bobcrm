using BobCrm.Api.Domain.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BobCrm.Api.Infrastructure.Ef.Configurations;

/// <summary>
/// SubEntityDefinition 实体配置
/// </summary>
public class SubEntityDefinitionConfiguration : IEntityTypeConfiguration<SubEntityDefinition>
{
    public void Configure(EntityTypeBuilder<SubEntityDefinition> builder)
    {
        builder.ToTable("SubEntityDefinitions");

        // 主键
        builder.HasKey(e => e.Id);

        // 基本属性
        builder.Property(e => e.Code)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(e => e.DisplayName)
            .IsRequired()
            .HasColumnType("jsonb");

        builder.Property(e => e.Description)
            .HasColumnType("jsonb");

        builder.Property(e => e.DefaultSortField)
            .HasMaxLength(100);

        builder.Property(e => e.ForeignKeyField)
            .HasMaxLength(100);

        builder.Property(e => e.CollectionPropertyName)
            .HasMaxLength(100);

        builder.Property(e => e.CascadeDeleteBehavior)
            .HasMaxLength(20)
            .IsRequired();

        // 导航属性：与主实体的关系
        builder.HasOne(e => e.EntityDefinition)
            .WithMany()
            .HasForeignKey(e => e.EntityDefinitionId)
            .OnDelete(DeleteBehavior.Cascade);

        // 导航属性：与字段的关系
        builder.HasMany(e => e.Fields)
            .WithOne(f => f.SubEntityDefinition)
            .HasForeignKey(f => f.SubEntityDefinitionId)
            .OnDelete(DeleteBehavior.Cascade);

        // 索引
        builder.HasIndex(e => e.EntityDefinitionId);

        builder.HasIndex(e => new { e.EntityDefinitionId, e.Code })
            .IsUnique()
            .HasDatabaseName("IX_SubEntityDefinitions_EntityDefinitionId_Code");

        builder.HasIndex(e => e.SortOrder);
    }
}
