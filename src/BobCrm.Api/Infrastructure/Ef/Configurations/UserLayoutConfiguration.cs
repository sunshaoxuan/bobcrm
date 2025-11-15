using BobCrm.Api.Base;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BobCrm.Api.Infrastructure.Ef.Configurations;

public class UserLayoutConfiguration : IEntityTypeConfiguration<UserLayout>
{
    public void Configure(EntityTypeBuilder<UserLayout> builder)
    {
        builder.HasIndex(x => new { x.UserId, x.EntityType }).IsUnique();
    }
}

