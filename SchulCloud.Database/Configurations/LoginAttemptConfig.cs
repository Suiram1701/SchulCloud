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
        builder.Property(l => l.Method).IsRequired();
        builder.Property(l => l.Result).IsRequired();
        builder.Property(l => l.IpAddress).HasMaxLength(16).IsRequired();
        builder.Property(l => l.Latitude).HasPrecision(8, 6);     // latitude and longitude precisions https://stackoverflow.com/a/1196429/20339558
        builder.Property(l => l.Longitude).HasPrecision(9, 6);
        builder.Property(l => l.UserAgent);
        builder.Property(l => l.DateTime).IsRequired();

        builder.ToTable("AspNetLoginAttempts");
    }
}
