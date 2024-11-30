namespace SchulCloud.Identity.Options;

/// <summary>
/// Options for automated email messages.
/// </summary>
public class EmailSenderOptions
{
    /// <summary>
    /// The displayed name of the sender.
    /// </summary>
    public string DisplayedName { get; set; } = string.Empty;

    /// <summary>
    /// The email address of the sender.
    /// </summary>
    public string Email { get; set; } = string.Empty;

    /// <summary>
    /// Displayed life spans of tokens sent via email.
    /// </summary>
    public EmailTokensLifeSpanOptions TokensLifeSpans { get; set; } = new();
}
