﻿using Microsoft.AspNetCore.Identity;
using SchulCloud.Server.Options;
using SchulCloud.Server.Services.Interfaces;

namespace SchulCloud.Server.Extensions;

public static class IdentityBuilderExtensions
{
    /// <summary>
    /// Adds an email sender to the identity infrastructure.
    /// </summary>
    /// <typeparam name="TSender">The type of the email sender.</typeparam>
    /// <param name="builder">The identity builder.</param>
    /// <returns>The identity builder pipeline.</returns>
    public static IdentityBuilder AddEmailSender<TSender>(this IdentityBuilder builder)
        where TSender : class
    {
        ArgumentNullException.ThrowIfNull(builder, nameof(builder));

        Type emailSenderType = typeof(IEmailSender<>).MakeGenericType(builder.UserType);
        if (!typeof(TSender).IsAssignableTo(emailSenderType))
        {
            throw new InvalidOperationException($"{typeof(TSender)} have to implement {emailSenderType}.");
        }

        builder.Services.AddScoped(emailSenderType, typeof(TSender));
        return builder;
    }

    /// <summary>
    /// Adds a password reset limiter to the service collection.
    /// </summary>
    /// <typeparam name="TLimiter">The type of the reset limiter.</typeparam>
    /// <param name="builder">The identity builder.</param>
    /// <returns>The identity builder pipeline.</returns>
    public static IdentityBuilder AddPasswordResetLimiter<TLimiter>(this IdentityBuilder builder, Action<PasswordResetLimiterOptions>? optionsAction = null)
        where TLimiter : class
    {
        ArgumentNullException.ThrowIfNull(builder, nameof(builder));

        Type limiterType = typeof(IPasswordResetLimiter<>).MakeGenericType(builder.UserType);
        if (!typeof(TLimiter).IsAssignableTo(limiterType))
        {
            throw new InvalidOperationException($"{typeof(TLimiter)} have to implement {limiterType}.");
        }

        builder.Services.Configure(optionsAction ?? (o => { }));

        builder.Services.AddSingleton(limiterType, typeof(TLimiter));
        return builder;
    }
}