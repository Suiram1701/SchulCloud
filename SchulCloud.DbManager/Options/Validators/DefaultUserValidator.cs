using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using System.Net.Mail;

namespace SchulCloud.DbManager.Options.Validators;

internal class DefaultUserValidator(IOptions<UserOptions> userOptions, IOptions<PasswordOptions> passwordOptions) : IValidateOptions<DefaultUserOptions>
{
    private readonly UserOptions _userOptions = userOptions.Value;
    private readonly PasswordOptions _passwordOptions = passwordOptions.Value;

    public ValidateOptionsResult Validate(string? name, DefaultUserOptions options)
    {
        List<string> errors = [];

        if (string.IsNullOrWhiteSpace(options.UserName))
        {
            errors.Add("The username mustn't be empty");
        }
        else
        {
            if (!options.UserName.All(_userOptions.AllowedUserNameCharacters.Contains))
            {
                errors.Add("Username contains illegal characters.");
            }
        }

        try
        {
            MailAddress address = new(options.Email);
        }
        catch (FormatException ex)
        {
            errors.Add($"The provided email isn't in the correct format: {ex.Message}");
        }

        if (string.IsNullOrWhiteSpace(options.Password))
        {
            errors.Add("The password mustn't be empty.");
        }
        else
        {
            ValidatePassword(options.Password, errors);
        }

        if (errors.Count != 0)
        {
            return ValidateOptionsResult.Fail(errors);
        }
        return ValidateOptionsResult.Success;
    }

    // Password validation code is copied and modified from https://github.com/dotnet/aspnetcore/blob/main/src/Identity/Extensions.Core/src/PasswordValidator.cs
    private void ValidatePassword(string password, List<string> errors)
    {
        if (password.Length < _passwordOptions.RequiredLength)
        {
            errors.Add($"The provided password is too short. At least {_passwordOptions.RequiredLength} are required.");
        }
        if (_passwordOptions.RequireNonAlphanumeric && password.All(IsLetterOrDigit))
        {
            errors.Add("The provided password have to contain non alphanumeric characters.");
        }
        if (_passwordOptions.RequireDigit && !password.Any(IsDigit))
        {
            errors.Add("The provided password have to contain at least one digit.");
        }
        if (_passwordOptions.RequireLowercase && !password.Any(IsLower))
        {
            errors.Add("The provided password have to contain at least one lowercase character.");
        }
        if (_passwordOptions.RequireUppercase && !password.Any(IsUpper))
        {
            errors.Add("The provided password have to contain at least one uppercase character.");
        }
        if (_passwordOptions.RequiredUniqueChars >= 1 && password.Distinct().Count() < _passwordOptions.RequiredUniqueChars)
        {
            errors.Add($"The provided password have to contain at least {_passwordOptions.RequiredUniqueChars} unique characters.");
        }
    }

    private bool IsDigit(char c)
    {
        return c >= '0' && c <= '9';
    }

    private bool IsLower(char c)
    {
        return c >= 'a' && c <= 'z';
    }

    private bool IsUpper(char c)
    {
        return c >= 'A' && c <= 'Z';
    }

    private bool IsLetterOrDigit(char c)
    {
        return IsUpper(c) || IsLower(c) || IsDigit(c);
    }
}
