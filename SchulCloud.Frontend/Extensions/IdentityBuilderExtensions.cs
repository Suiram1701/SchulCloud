﻿using Microsoft.AspNetCore.Identity;
using SchulCloud.Frontend.Identity.TokenProviders;
using SchulCloud.Frontend.Options;
using SchulCloud.Frontend.Services.Interfaces;
using SchulCloud.Identity.Options;

namespace SchulCloud.Frontend.Extensions;

public static class IdentityBuilderExtensions
{
    /// <summary>
    /// Configures the default identity cookies.
    /// </summary>
    /// <param name="builder">The identity builder.</param>
    /// <returns>The identity builder pipeline.</returns>
    public static IdentityBuilder ConfigureDefaultIdentityCookies(this IdentityBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.Services.ConfigureApplicationCookie(options =>
        {
            options.LoginPath = Routes.Login();
            options.LogoutPath = Routes.Logout();
            options.AccessDeniedPath = Routes.Forbidden();

            options.ReturnUrlParameter = "returnUrl";

            options.ExpireTimeSpan = TimeSpan.FromDays(31);     // this time is used for persistent sessions.
            options.SlidingExpiration = true;
        });

        return builder;
    }

    /// <summary>
    /// Adds an email sender to the identity infrastructure.
    /// </summary>
    /// <remarks>
    /// The <typeparamref name="TSender"/> have to implement <see cref="Identity.EmailSenders.IEmailSender{TUser}"/>.
    /// </remarks>
    /// <typeparam name="TSender">The type of the email sender.</typeparam>
    /// <param name="builder">The identity builder.</param>
    /// <returns>The identity builder pipeline.</returns>
    public static IdentityBuilder AddEmailSender<TSender>(this IdentityBuilder builder)
        where TSender : class
    {
        ArgumentNullException.ThrowIfNull(builder);

        Type emailSenderType = typeof(Identity.EmailSenders.IEmailSender<>).MakeGenericType(builder.UserType);
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
    /// <remarks>
    /// The <typeparamref name="TLimiter"/> have to implement <see cref="IRequestLimiter{TUser}"/>.
    /// </remarks>
    /// <typeparam name="TLimiter">The type of the reset limiter.</typeparam>
    /// <param name="builder">The identity builder.</param>
    /// <returns>The identity builder pipeline.</returns>
    public static IdentityBuilder AddRequestLimiter<TLimiter>(this IdentityBuilder builder, Action<RequestLimiterOptions>? optionsAction = null)
        where TLimiter : class
    {
        ArgumentNullException.ThrowIfNull(builder);

        Type limiterType = typeof(IRequestLimiter<>).MakeGenericType(builder.UserType);
        if (!typeof(TLimiter).IsAssignableTo(limiterType))
        {
            throw new InvalidOperationException($"{typeof(TLimiter)} have to implement {limiterType}.");
        }

        builder.Services.Configure(optionsAction ?? (o => { }));
        builder.Services.AddScoped(limiterType, typeof(TLimiter));
        return builder;
    }

    /// <summary>
    /// Adds the token providers of the application to the identity builder.
    /// </summary>
    /// <param name="builder">The identity builder.</param>
    /// <returns>The identity builder.</returns>
    public static IdentityBuilder AddTokenProviders(this IdentityBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        Type emailProviderType = typeof(AuthenticationCodeTokenProvider<>).MakeGenericType(builder.UserType);
        return builder.AddTokenProvider(ExtendedTokenProviderOptions.AuthenticationTokenProvider, emailProviderType);
    }
}
