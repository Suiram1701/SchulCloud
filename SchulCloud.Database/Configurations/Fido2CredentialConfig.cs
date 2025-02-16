﻿using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SchulCloud.Database.Models;

namespace SchulCloud.Database.Configurations;

internal sealed class Fido2CredentialConfig : IEntityTypeConfiguration<Credential>
{
    public void Configure(EntityTypeBuilder<Credential> builder)
    {
        builder.HasKey(c => c.Id);
        builder.HasIndex(cu => new { cu.UserId, cu.Name }).IsUnique();

        builder.Property(c => c.Id).HasMaxLength(256).IsRequired();
        builder.Property(c => c.UserId).HasMaxLength(256).IsRequired();
        builder.Property(c => c.Name).HasMaxLength(256);
        builder.Property(c => c.IsPasskey).HasDefaultValue(false);
        builder.Property(c => c.PublicKey).IsRequired();
        builder.Property(c => c.Transports).HasJsonPropertyName(null);
        builder.Property(c => c.SignCount).HasDefaultValue(0);
        builder.Property(c => c.RegDate).IsRequired();
        builder.Property(c => c.AaGuid).IsRequired();

        builder.ToTable("AspNetCredentials");
    }
}
