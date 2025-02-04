using Aspire.Hosting.ApplicationModel;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace Aspire.Hosting.MinIO;

/// <summary>
/// A resource that represents a MinIO storage container.
/// </summary>
/// <param name="name">The name of the resource.</param>
/// <param name="username">The root username parameter. If <c>null</c> the default username will be used.</param>
/// <param name="password">The root password parameter.</param>
public class MinIOServerResource(string name, ParameterResource? username, ParameterResource password) : ContainerResource(name), IResourceWithConnectionString, IResourceWithEndpoints
{
    /// <summary>
    /// The username parameter.
    /// </summary>
    public ParameterResource? UsernameParameter { get; } = username;

    private ReferenceExpression UsernameReference => UsernameParameter is not null
        ? ReferenceExpression.Create($"{UsernameParameter}")
        : ReferenceExpression.Create($"minio");     // Default username

    /// <summary>
    /// The password parameter.
    /// </summary>
    public ParameterResource PasswordParameter { get; } = ThrowIfNull(password);

    /// <summary>
    /// The API endpoint of this resource.
    /// </summary>
    public EndpointReference Endpoint => _endpoint ??= new(this, "http");
    private EndpointReference? _endpoint;

    public ReferenceExpression ConnectionStringExpression =>
        ReferenceExpression.Create($"Endpoint={Endpoint.Property(EndpointProperty.Url)};Username={(UsernameReference)};Password={PasswordParameter}");

    /// <summary>
    /// A dictionary of resource names and their bucket name.
    /// </summary>
    public IReadOnlyDictionary<string, string> Buckets => _buckets;
    private readonly Dictionary<string, string> _buckets = [];

    internal void AddBucket(string name, string bucketName)
    {
        if (_buckets.ContainsValue(bucketName))
            throw new InvalidOperationException($"A bucket with the name '{bucketName}' is already registered.");
        _buckets.Add(name, bucketName);
    }

    private static T ThrowIfNull<T>([NotNull] T? argument, [CallerArgumentExpression(nameof(argument))] string? paramName = null) => argument ?? throw new ArgumentNullException(paramName);
}