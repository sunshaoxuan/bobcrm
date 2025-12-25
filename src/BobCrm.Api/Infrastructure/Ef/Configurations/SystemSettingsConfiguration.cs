using BobCrm.Api.Base;
using BobCrm.Api.Base.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BobCrm.Api.Infrastructure.Ef.Configurations;

public class SystemSettingsConfiguration : IEntityTypeConfiguration<SystemSettings>
{
    public void Configure(EntityTypeBuilder<SystemSettings> builder)
    {
        builder.HasKey(s => s.Id);
        builder.Property(s => s.CompanyName).HasMaxLength(128).IsRequired();
        builder.Property(s => s.DefaultTheme).HasMaxLength(32).IsRequired();
        builder.Property(s => s.DefaultPrimaryColor).HasMaxLength(16);
        builder.Property(s => s.DefaultLanguage).HasMaxLength(16).IsRequired();
        builder.Property(s => s.DefaultHomeRoute).HasMaxLength(64).IsRequired();
        builder.Property(s => s.DefaultNavMode)
            .HasMaxLength(32)
            .HasDefaultValue(NavDisplayModes.IconText)
            .IsRequired();
        builder.Property(s => s.TimeZoneId).HasMaxLength(64).IsRequired();
        builder.Property(s => s.AllowSelfRegistration).HasDefaultValue(false);

        builder.Property(s => s.SmtpHost).HasMaxLength(256);
        builder.Property(s => s.SmtpPort).HasDefaultValue(25);
        builder.Property(s => s.SmtpUsername).HasMaxLength(256);
        builder.Property(s => s.SmtpPasswordEncrypted).HasMaxLength(2048);
        builder.Property(s => s.SmtpEnableSsl).HasDefaultValue(false);
        builder.Property(s => s.SmtpFromAddress).HasMaxLength(256);
        builder.Property(s => s.SmtpDisplayName).HasMaxLength(256);
    }
}
