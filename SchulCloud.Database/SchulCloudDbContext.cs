using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using SchulCloud.Database.Models;

namespace SchulCloud.Database;

public class SchulCloudDbContext(DbContextOptions options) : IdentityDbContext<SchulCloudUser, SchulCloudRole, string>(options)
{
    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<SchulCloudUser>(b =>
        {
            b.Ignore(u => u.TwoFactorEnabled);
            b.Property(u => u.TwoFactorEnabledFlags);

            b.HasMany<Fido2Credential>().WithOne().HasForeignKey(uc => uc.UserId).IsRequired();
        });

        builder.Entity<Fido2Credential>(b =>
        {
            b.HasKey(c => c.Id);
            b.HasIndex(cu => new { cu.UserId, cu.SecurityKeyName }).IsUnique();

            b.Property(c => c.Id).HasMaxLength(256).IsRequired();
            b.Property(c => c.UserId).HasMaxLength(256).IsRequired();
            b.Property(c => c.SecurityKeyName).HasMaxLength(256);
            b.Property(c => c.IsUsernameless).HasDefaultValue(false);
            b.Property(c => c.PublicKey).IsRequired();
            b.Property(c => c.Transports).HasJsonPropertyName(null);
            b.Property(c => c.SignCount).HasDefaultValue(0);
            b.Property(c => c.RegDate).IsRequired();
            b.Property(c => c.AaGuid).IsRequired();

            b.OwnsMany(c => c.DevicePublicKeys, pdkB =>
            {
                pdkB.WithOwner().HasForeignKey(pdk => pdk.CredentialId);

                pdkB.ToTable("AspNetCredentialDeviceKeys");
            });

            b.ToTable("AspNetCredentials");
        });
    }
}
