using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SchulCloud.Database.Models;

namespace SchulCloud.Database.Configurations;

internal sealed class LogInAttemptConfig : IEntityTypeConfiguration<LogInAttempt>
{
    public void Configure(EntityTypeBuilder<LogInAttempt> builder)
    {
        builder.HasKey(l => l.Id);

        builder.Property(l => l.Id).HasMaxLength(256).IsRequired();
        builder.Property(l => l.UserId).HasMaxLength(256).IsRequired();
        builder.Property(l => l.MethodCode).HasMaxLength(3).IsRequired();
        builder.Property(l => l.Succeeded).HasDefaultValue(false);
        builder.Property(l => l.IpAddress).HasMaxLength(4).IsRequired();
        builder.Property(l => l.UserAgent);
        builder.Property(l => l.DateTime).IsRequired();

        builder.ToTable("AspNetLogInAttempts");
    }
}
