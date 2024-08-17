using Aspire.Hosting.ApplicationModel;

namespace Aspire.Hosting.MailDev;

public sealed class MailDevResource(string name, ParameterResource? username, ParameterResource password) : ContainerResource(name), IResourceWithConnectionString, IResourceWithEndpoints
{
    private const string _defaultUserName = "mail-dev";

    internal ReferenceExpression UsernameReference => ReferenceExpression.Create($"{UsernameParameter?.Value ?? _defaultUserName}");

    public ParameterResource? UsernameParameter { get; set; } = username;

    public ParameterResource PasswordParameter { get; set; } = password;

    public EndpointReference SmtpEndpoint => _smtpEndpoint ??= new(this, "smtp");
    private EndpointReference? _smtpEndpoint;

    public ReferenceExpression ConnectionStringExpression =>
        ReferenceExpression.Create($"Endpoint=smtp://{SmtpEndpoint.Property(EndpointProperty.Host)}:{SmtpEndpoint.Property(EndpointProperty.Port)};Username={UsernameReference};Password={PasswordParameter}");
}
