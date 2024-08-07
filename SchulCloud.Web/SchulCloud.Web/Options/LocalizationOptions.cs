namespace SchulCloud.Web.Options;

/// <summary>
/// Options for localization.
/// </summary>
public class LocalizationOptions
{
    /// <summary>
    /// A list of the culture code of every supported culture.
    /// </summary>
    public IEnumerable<string> SupportedCultures { get; set; } = [];

    /// <summary>
    /// Indicates whether the localization should fallback to the parent culture when the culture isn't supported (default <c>true</c>).
    /// </summary>
    public bool FallbackToParentCulture { get; set; } = true;

    /// <summary>
    /// Indicates whether the culture will be applied in the <c>Content-Language</c> header (by default <c>true</c>).
    /// </summary>
    public bool ApplyToHeader { get; set; } = true;
}
