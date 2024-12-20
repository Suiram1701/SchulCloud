namespace SchulCloud.Authentication;

public class HeaderNames
{
    /// <summary>
    /// The name of the header the api key used by <see cref="AuthenticationBuilderExtensions.AddApiKey(Microsoft.AspNetCore.Authentication.AuthenticationBuilder)"/>.
    /// </summary>
    public const string ApiKeyHeader = "x-api-key";
}
