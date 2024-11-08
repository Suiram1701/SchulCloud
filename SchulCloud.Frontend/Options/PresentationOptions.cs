namespace SchulCloud.Frontend.Options;

/// <summary>
/// Represents settings of the presentation of the web app.
/// </summary>
public class PresentationOptions
{
    /// <summary>
    /// The displayed name of the web app.
    /// </summary>
    public string ApplicationName { get; set; } = string.Empty;

    /// <summary>
    /// Icons of the web app.
    /// </summary>
    public IEnumerable<Favicon> Favicons { get; set; } = [];

    public record Favicon(string Path, string MimeType, string? Sizes);
}
