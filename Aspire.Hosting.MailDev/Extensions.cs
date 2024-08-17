using Aspire.Hosting.ApplicationModel;

namespace Aspire.Hosting.MailDev;

public static class Extensions
{
    public static IResourceBuilder<MailDevResource> AddMailDev(
        this IDistributedApplicationBuilder builder,
        string name,
        int? httpPort = null,
        int? smtpPort = null,
        IResourceBuilder<ParameterResource>? username = null,
        IResourceBuilder<ParameterResource>? password = null)
    {
        ParameterResource passwordParameter = password?.Resource
            ?? ParameterResourceBuilderExtensions.CreateDefaultPasswordParameter(builder, $"{name}-password");

        MailDevResource mailDev = new(name, username?.Resource, passwordParameter);
        return builder.AddResource(mailDev)
            .WithImage("maildev/maildev", tag: "2.1.0")
            .WithImageRegistry("docker.io")
            .WithHttpEndpoint(port: httpPort, targetPort: 1080)
            .WithEndpoint(name: "smtp", port: smtpPort, targetPort: 1025)
            .WithEnvironment(context =>
            {
                context.EnvironmentVariables["MAILDEV_INCOMING_USER"] = mailDev.UsernameReference;
                context.EnvironmentVariables["MAILDEV_INCOMING_PASS"] = mailDev.PasswordParameter;
            });
    }
}
