using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SchulCloud.Database.Models;

namespace SchulCloud.Database.Configurations;

internal sealed class LoginAttemptConfig : IEntityTypeConfiguration<LoginAttempt>
{
    public void Configure(EntityTypeBuilder<LoginAttempt> builder)
    {
        builder.HasKey(l => l.Id);

        builder.Property(l => l.Id).HasMaxLength(256).IsRequired();
        builder.Property(l => l.UserId).HasMaxLength(256).IsRequired();
        builder.Property(l => l.Method).HasMaxLength(64).IsRequired();
        builder.Property(l => l.Succeeded).HasDefaultValue(false);
        builder.Property(l => l.IpAddress).HasMaxLength(4).IsRequired();
        builder.Property(l => l.UserAgent);
        builder.Property(l => l.DateTime).IsRequired();

        builder.ToTable("AspNetLoginAttempts");
    }
}
