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
        builder.ApplyConfiguration(new LoginAttemptConfig());
        builder.ApplyConfiguration(new ApiKeyConfig());

        builder.Entity<SchulCloudUser>(b =>
        {
            b.Property(u => u.PasskeysEnabled).HasDefaultValue(false).IsRequired();
            b.Ignore(u => u.TwoFactorEnabled);
            b.Property(u => u.TwoFactorEnabledFlags);

            b.HasMany<Credential>().WithOne().HasForeignKey(uc => uc.UserId).IsRequired();
            b.HasMany<LoginAttempt>().WithOne().HasForeignKey(la => la.UserId).IsRequired();
            b.HasMany<ApiKey>().WithOne().HasForeignKey(key => key.UserId).IsRequired();
        });
    }
}
