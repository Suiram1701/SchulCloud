using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using SchulCloud.Web.Options;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace SchulCloud.Web.Identity.TokenProviders;

/// <summary>
/// A token provider that stores the generated token as an authentication token.
/// </summary>
public partial class AuthenticationCodeTokenProvider<TUser>(ILogger<AuthenticationCodeTokenProvider<TUser>> logger, IOptions<AuthenticationCodeProviderOptions> optionsAccessor)
    : IUserTwoFactorTokenProvider<TUser>
    where TUser : class
{
    private const string _providerName = "[AuthenticationCodeTokenProvider]";

    private readonly ILogger _logger = logger;
    private readonly AuthenticationCodeProviderOptions _options = optionsAccessor.Value;

    public Task<bool> CanGenerateTwoFactorTokenAsync(UserManager<TUser> manager, TUser user)
    {
        ArgumentNullException.ThrowIfNull(manager);
        return Task.FromResult(manager.SupportsUserAuthenticationTokens);
    }

    public async Task<string> GenerateAsync(string purpose, UserManager<TUser> manager, TUser user)
    {
        ArgumentNullException.ThrowIfNullOrEmpty(purpose);
        ArgumentNullException.ThrowIfNull(manager);
        ArgumentNullException.ThrowIfNull(user);

        string code = GenerateRandomToken();
        AuthenticationTokenModel model = new(DateTimeOffset.UtcNow.Add(_options.TokenLifeSpan), HashCode(purpose, code));

        IdentityResult result = await manager.SetAuthenticationTokenAsync(user, _providerName, GetTokenName(purpose), JsonSerializer.Serialize(model));
        if (!result.Succeeded)
        {
            LogCodeCreationError(purpose, result.Errors.Select(error => error.Description));
            return null!;
        }

        LogCodeCreated(purpose);
        return code;
    }

    public async Task<bool> ValidateAsync(string purpose, string token, UserManager<TUser> manager, TUser user)
    {
        ArgumentNullException.ThrowIfNullOrEmpty(purpose);
        ArgumentNullException.ThrowIfNull(manager);
        ArgumentNullException.ThrowIfNull(user);

        if (string.IsNullOrWhiteSpace(token))
        {
            return false;
        }

        string? rawToken = await manager.GetAuthenticationTokenAsync(user, _providerName, GetTokenName(purpose));
        if (rawToken is null || JsonSerializer.Deserialize<AuthenticationTokenModel>(rawToken) is not AuthenticationTokenModel model)
        {
            return false;
        }

        if (model.ExpirationTime <= DateTimeOffset.UtcNow)
        {
            if (await TryRemoveTokenAsync(manager, user, purpose))
            {
                LogCodeRemoved("Expired", purpose);
            }
            return false;
        }

        if (model.CodeHash?.Equals(HashCode(purpose, token)) ?? false)
        {
            if (await TryRemoveTokenAsync(manager, user, purpose))
            {
                LogCodeRemoved("Used", purpose);
            }
            return true;
        }
        return false;
    }

    private static string GetTokenName(string purpose) => $"AuthenticationCode-{purpose}";

    private static string HashCode(string purpose, string code)
    {
        byte[] keyBytes = Encoding.UTF8.GetBytes(purpose);
        byte[] codeBytes = Encoding.UTF8.GetBytes(code);

        return Convert.ToBase64String(HMACSHA256.HashData(keyBytes, codeBytes));
    }

    private async Task<bool> TryRemoveTokenAsync(UserManager<TUser> manager, TUser user, string purpose)
    {
        IdentityResult removeResult = await manager.RemoveAuthenticationTokenAsync(user, _providerName, GetTokenName(purpose));
        if (!removeResult.Succeeded && _logger.IsEnabled(LogLevel.Error))
        {
            LogCodeRemovedError(purpose, removeResult.Errors.Select(error => error.Description));
        }

        return removeResult.Succeeded;
    }

    private static string GenerateRandomToken()
    {
        return string.Concat(values: [
            RandomChar(),
            RandomChar(),
            RandomChar(),
            RandomChar(),
            RandomChar(),
            '-',
            RandomChar(),
            RandomChar(),
            RandomChar(),
            RandomChar(),
            RandomChar(),
            ]);

        static char RandomChar()
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";

            int i = Random.Shared.Next(0, chars.Length - 1);
            return chars[i];
        }
    }

    private record AuthenticationTokenModel(DateTimeOffset ExpirationTime, string? CodeHash);

    [LoggerMessage(LogLevel.Debug, "Authentication code with purpose '{purpose}' created and stored.")]
    private partial void LogCodeCreated(string purpose);

    [LoggerMessage(LogLevel.Debug, "Authentication code with purpose '{purpose}' removed with cause '{cause}'.")]
    private partial void LogCodeRemoved(string cause, string purpose);

    [LoggerMessage(LogLevel.Error, "An error occurred while creating an authentication code with purpose '{purpose}'. {errors}")]
    private partial void LogCodeCreationError(string purpose, IEnumerable<string> errors);

    [LoggerMessage(LogLevel.Error, "An error occurred while removing an authentication code with purpose '{purpose}'. {errors}")]
    private partial void LogCodeRemovedError(string purpose, IEnumerable<string> errors);
}
