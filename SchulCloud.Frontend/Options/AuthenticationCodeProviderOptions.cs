namespace SchulCloud.Frontend.Options;

public class AuthenticationCodeProviderOptions
{
    /// <summary>
    /// The lifespan of the generated token.
    /// </summary>
    public TimeSpan TokenLifeSpan { get; set; } = TimeSpan.FromHours(1);
}
