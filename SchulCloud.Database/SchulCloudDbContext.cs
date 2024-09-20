using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using SchulCloud.Database.Configurations;
using SchulCloud.Database.Models;

namespace SchulCloud.Database;

public class SchulCloudDbContext(DbContextOptions options) : IdentityDbContext<SchulCloudUser, SchulCloudRole, string>(options)
{
    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        builder.ApplyConfiguration(new Fido2CredentialConfig());
        builder.ApplyConfiguration(new LogInAttemptConfig());

        builder.Entity<SchulCloudUser>(b =>
        {
            b.Property(u => u.PasskeysEnabled).HasDefaultValue(false).IsRequired();
            b.Ignore(u => u.TwoFactorEnabled);
            b.Property(u => u.TwoFactorEnabledFlags);

            b.HasMany<Fido2Credential>().WithOne().HasForeignKey(uc => uc.UserId).IsRequired();
            b.HasMany<LogInAttempt>().WithOne().HasForeignKey(la => la.UserId).IsRequired();
        });
    }
}
