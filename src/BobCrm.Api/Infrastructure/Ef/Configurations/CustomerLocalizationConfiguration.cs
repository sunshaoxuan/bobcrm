using BobCrm.Api.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BobCrm.Api.Infrastructure.Ef.Configurations;

public class CustomerLocalizationConfiguration : IEntityTypeConfiguration<CustomerLocalization>
{
    public void Configure(EntityTypeBuilder<CustomerLocalization> builder)
    {
        builder.HasKey(x => new { x.CustomerId, x.Language });
        builder.HasOne(x => x.Customer)
            .WithMany()
            .HasForeignKey(x => x.CustomerId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

