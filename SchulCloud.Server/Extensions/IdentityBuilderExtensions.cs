using Microsoft.AspNetCore.Identity;

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
        Type emailSenderType = typeof(IEmailSender<>).MakeGenericType(builder.UserType);
        if (!typeof(TSender).IsAssignableTo(emailSenderType))
        {
            throw new InvalidOperationException($"{typeof(TSender)} have to implement {emailSenderType}");
        }

        builder.Services.AddScoped(emailSenderType, typeof(TSender));
        return builder;
    }
}
