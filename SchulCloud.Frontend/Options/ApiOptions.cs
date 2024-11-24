namespace SchulCloud.Frontend.Options;

/// <summary>
/// Options for the api.
/// </summary>
public class ApiOptions
{
    /// <summary>
    /// A mapping of the api name to the documentation.
    /// </summary>
    public Dictionary<string, Uri?> DocumentationLinks { get; set; } = [];
}
