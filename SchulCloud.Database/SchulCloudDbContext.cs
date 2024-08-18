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
            b.Ignore(user => user.TwoFactorEnabled);
            b.Property(user => user.TwoFactorEnabledFlags);
        });
    }
}
