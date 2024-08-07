using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Localization;
using System.Runtime.CompilerServices;

namespace SchulCloud.Web.Identity;

/// <summary>
/// An identity error describer that uses localized resources for descriptions.
/// </summary>
/// <param name="localizer">The localizer to use</param>
public class LocalizedErrorDescriber(IStringLocalizer<LocalizedErrorDescriber> localizer) : IdentityErrorDescriber
{
    private readonly IStringLocalizer _localizer = localizer;

    private IdentityError LocalizeErrorMessage([CallerMemberName] string? resourceName = null, params object[] arguments)
    {
        ArgumentNullException.ThrowIfNull(resourceName, nameof(resourceName));

        return new()
        {
            Code = resourceName,
            Description = _localizer[resourceName, arguments]
        };
    }

    public override IdentityError DefaultError() => LocalizeErrorMessage();

    public override IdentityError ConcurrencyFailure() => LocalizeErrorMessage();

    public override IdentityError PasswordMismatch() => LocalizeErrorMessage();

    public override IdentityError InvalidToken() => LocalizeErrorMessage();

    public override IdentityError RecoveryCodeRedemptionFailed() => LocalizeErrorMessage();

    /// <summary>
    /// This application doesn't support external logins. This method will always throw an exception.
    /// </summary>
    /// <returns></returns>
    /// <exception cref="NotImplementedException"></exception>
    public override IdentityError LoginAlreadyAssociated()
    {
        throw new NotImplementedException("This application doesn't support external login at all.");
    }

    public override IdentityError InvalidUserName(string? userName) => LocalizeErrorMessage(arguments: userName ?? string.Empty);

    public override IdentityError InvalidEmail(string? email) => LocalizeErrorMessage(arguments: email ?? string.Empty);

    public override IdentityError DuplicateUserName(string userName) => LocalizeErrorMessage(arguments: userName ?? string.Empty);

    public override IdentityError DuplicateEmail(string email) => LocalizeErrorMessage(arguments: email ?? string.Empty);

    public override IdentityError InvalidRoleName(string? role) => LocalizeErrorMessage(arguments: role ?? string.Empty);

    public override IdentityError DuplicateRoleName(string role) => LocalizeErrorMessage(arguments: role ?? string.Empty);

    public override IdentityError UserAlreadyHasPassword() => LocalizeErrorMessage();

    public override IdentityError UserLockoutNotEnabled() => LocalizeErrorMessage();

    public override IdentityError UserAlreadyInRole(string role) => LocalizeErrorMessage(arguments: role ?? string.Empty);

    public override IdentityError UserNotInRole(string role) => LocalizeErrorMessage(arguments: role ?? string.Empty);

    public override IdentityError PasswordTooShort(int length) => LocalizeErrorMessage(arguments: length);

    public override IdentityError PasswordRequiresUniqueChars(int uniqueChars) => LocalizeErrorMessage(arguments: uniqueChars);

    public override IdentityError PasswordRequiresNonAlphanumeric() => LocalizeErrorMessage();

    public override IdentityError PasswordRequiresDigit() => LocalizeErrorMessage();

    public override IdentityError PasswordRequiresLower() => LocalizeErrorMessage();

    public override IdentityError PasswordRequiresUpper() => LocalizeErrorMessage();
}
