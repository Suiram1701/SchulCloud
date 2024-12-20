using FluentValidation;
using FluentValidation.Results;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;

namespace SchulCloud.DbManager.Options;

internal class DefaultUserOptions
{
    /// <summary>
    /// The name of the default user.
    /// </summary>
    public string UserName { get; set; } = string.Empty;

    /// <summary>
    /// The email of the default user.
    /// </summary>
    public string Email { get; set; } = string.Empty;

    /// <summary>
    /// The clear-text default password of the default user.
    /// </summary>
    public string Password { get; set; } = string.Empty;

    /// <summary>
    /// An options compatible validator for the <see cref="DefaultUserOptions"/>.
    /// </summary>
    public sealed class Validator : AbstractValidator<DefaultUserOptions>, IValidateOptions<DefaultUserOptions>
    {
        public Validator(IOptions<UserOptions> userOptionsAccessor)
        {
            UserOptions userOptions = userOptionsAccessor.Value;

            RuleFor(options => options.UserName)
                .NotEmpty()
                .ForEach(inlineValidator =>
                {
                    inlineValidator
                        .Must(c => userOptions.AllowedUserNameCharacters.Contains(c))
                        .WithMessage("The UserName contains a not allowed char: '{PropertyValue}'.");
                });
            RuleFor(options => options.Email)
                .NotEmpty()
                .EmailAddress();
            RuleFor(options => options.Password).NotEmpty();
        }

        ValidateOptionsResult IValidateOptions<DefaultUserOptions>.Validate(string? name, DefaultUserOptions options)
        {
            ValidationResult result = Validate(options);
            if (!result.IsValid)
            {
                return ValidateOptionsResult.Fail(result.Errors.Select(error => error.ToString()));
            }
            return ValidateOptionsResult.Success;
        }
    }
}
