namespace SchulCloud.ServiceDefaults.Options;

/// <summary>
/// Options for a service.
/// </summary>
public class ServiceOptions
{
    /// <summary>
    /// The path prefix of the application
    /// </summary>
    public string BasePath { get; set; } = default!;

    public string GetPathWithBase(string path) => $"{BasePath.TrimEnd('/')}/{path.TrimStart('/')}";
}
