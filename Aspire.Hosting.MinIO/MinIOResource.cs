using Aspire.Hosting.ApplicationModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Aspire.Hosting.MinIO;

public class MinIOResource(string name, ParameterResource username, ParameterResource password) : ContainerResource(name), IResourceWithConnectionString, IResourceWithEndpoints
{
    public ParameterResource UsernameParameter { get; set; } = username;

    public ParameterResource PasswordParameter { get; set; } = password;

    public EndpointReference Endpoint => _endpoint ??= new(this, "http");
    private EndpointReference? _endpoint;

    public ReferenceExpression ConnectionStringExpression =>
        ReferenceExpression.Create(
            $"Endpoint={Endpoint.Property(EndpointProperty.Scheme)}://{Endpoint.Property(EndpointProperty.Host)}:{Endpoint.Property(EndpointProperty.Port)};" +
            $"Username={UsernameParameter};Password={PasswordParameter}");
}