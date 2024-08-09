using FluentValidation;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Localization;
using SchulCloud.Database.Models;
using System.ComponentModel.DataAnnotations;

namespace SchulCloud.Web.Models;

/// <summary>
/// The model for the <see cref="Components.Pages.Auth.ResetPassword"/> page.
/// </summary>
public class PasswordResetModel
{
    /// <summary>
    /// The new password.
    /// </summary>
    public string NewPassword { get; set; } = default!;

    /// <summary>
    /// The <see cref="NewPassword"/> again as confirmation.
    /// </summary>
    public string ConfirmedPassword { get; set; } = default!;

    /// <summary>
    /// A validator for the <see cref="PasswordResetModel"/>.
    /// </summary>>
    public class Validator : AbstractValidator<PasswordResetModel>
    {
        private readonly IPasswordValidator<User> _passwordValidator;
        private readonly UserManager<User> _userManager;

        public Validator(IStringLocalizer<PasswordResetModel> localizer, IPasswordValidator<User> passwordValidator, UserManager<User> userManager)
        {
            _passwordValidator = passwordValidator;
            _userManager = userManager;

            RuleFor(model => model.NewPassword).CustomAsync(ValidateNewPasswordAsync);
            RuleFor(model => model.ConfirmedPassword)
                .NotEmpty()
                .WithMessage(localizer["confirmedPasswordNotMatch"])
                .Equal(model => model.NewPassword)
                .WithMessage(localizer["confirmedPasswordNotMatch"]);
        }

        private async Task ValidateNewPasswordAsync(string password, ValidationContext<PasswordResetModel> context, CancellationToken ct)
        {
            IdentityResult result = await _passwordValidator.ValidateAsync(_userManager, null!, password);
            if (!result.Succeeded)
            {
                foreach (IdentityError error in result.Errors)
                {
                    context.AddFailure(context.PropertyPath, error.Description);
                }
            }
        }
    }
}