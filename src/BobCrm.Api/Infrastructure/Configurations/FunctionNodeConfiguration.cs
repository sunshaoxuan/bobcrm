using BobCrm.Api.Base.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BobCrm.Api.Infrastructure.Configurations;

public class FunctionNodeConfiguration : IEntityTypeConfiguration<FunctionNode>
{
    public void Configure(EntityTypeBuilder<FunctionNode> builder)
    {
        builder.ToTable("FunctionNodes");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Code).IsRequired().HasMaxLength(100);
        builder.HasIndex(x => x.Code).IsUnique();
        builder.Property(x => x.Name).IsRequired().HasMaxLength(200);
        builder.Property(x => x.DisplayName).HasColumnType("jsonb");
        builder.Property(x => x.Route).HasMaxLength(256);
        builder.Property(x => x.Icon).HasMaxLength(64);

        builder.HasMany(x => x.Children)
            .WithOne(x => x.Parent)
            .HasForeignKey(x => x.ParentId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.TemplateStateBinding)
            .WithMany()
            .HasForeignKey(x => x.TemplateStateBindingId)
            .OnDelete(DeleteBehavior.SetNull);

        // Legacy template link (deprecated but still mapped for backward compatibility / existing schema)
#pragma warning disable CS0618
        builder.HasOne(x => x.Template)
            .WithMany()
            .HasForeignKey(x => x.TemplateId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(x => x.TemplateBinding)
            .WithMany()
            .HasForeignKey(x => x.TemplateBindingId)
            .OnDelete(DeleteBehavior.SetNull);
#pragma warning restore CS0618
    }
}
