namespace SchulCloud.Web.Options;

public class AuthenticationTokenProviderOptions
{
    /// <summary>
    /// The lifespan of the generated token.
    /// </summary>
    public TimeSpan TokenLifeSpan { get; set; } = TimeSpan.FromHours(1);
}
