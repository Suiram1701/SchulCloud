using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.DependencyInjection;
using SchulCloud.Authentication.AuthenticationSchemes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SchulCloud.Authentication;

public static class AuthenticationBuilderExtensions
{
    /// <summary>
    /// Adds the api key authentication scheme.
    /// </summary>
    /// <typeparam name="TUser">The type of the user. This is required to request a service that depends on this type.</typeparam>
    /// <param name="builder">The builder to use.</param>
    /// <returns>The builder pipeline.</returns>
    public static AuthenticationBuilder AddApiKey<TUser>(this AuthenticationBuilder builder)
        where TUser : class
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.Services.AddMemoryCache();
        builder.Services.AddProblemDetails();
        return builder.AddScheme<ApiKeySchemeOptions, ApiKeyScheme<TUser>>(SchemeNames.ApiKeyScheme, null);
    }
}
