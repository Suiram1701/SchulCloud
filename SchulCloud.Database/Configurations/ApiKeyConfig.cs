using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SchulCloud.Database.Models;
using SchulCloud.Database.ValueComparers;
using SchulCloud.Database.ValueConverters;

namespace SchulCloud.Database.Configurations;

internal sealed class ApiKeyConfig : IEntityTypeConfiguration<ApiKey>
{
    public void Configure(EntityTypeBuilder<ApiKey> builder)
    {
        builder.HasIndex(key => key.Id);
        builder.HasIndex(key => key.KeyHash).IsUnique();
        builder.HasIndex(key => new { key.UserId, key.Name }).IsUnique();

        builder.Property(key => key.Id).HasMaxLength(256).IsRequired();
        builder.Property(key => key.UserId).HasMaxLength(256).IsRequired();
        builder.Property(key => key.Name).HasMaxLength(256);
        builder.Property(key => key.Notes).HasMaxLength(512);
        builder.Property(key => key.KeyHash).HasMaxLength(256).IsRequired();
        builder.Property(key => key.AllPermissions).HasDefaultValue(false);
        builder.Property(key => key.PermissionLevels).HasConversion<ApiKeyPermissionConverter>(new ApiKeyPermissionComparer());
        builder.Property(key => key.Created).IsRequired();
        builder.Property(key => key.Expiration);

        builder.ToTable("AspNetApiKeys");

        builder.HasQueryFilter(key => key.Expiration > DateTime.UtcNow || key.Expiration == null);
    }
}
