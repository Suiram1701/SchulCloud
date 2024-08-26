using FluentValidation;
using Microsoft.Extensions.Localization;

namespace SchulCloud.Web.Models;

public class RenameModel
{
    /// <summary>
    /// The old name of the item.
    /// </summary>
    public string? OldName { get; set; }

    /// <summary>
    /// The new name of the item.
    /// </summary>
    public string? NewName { get; set; }

    /// <summary>
    /// Names that should be ex
    /// </summary>
    public IEnumerable<string?> ExcludedNames { get; set; } = [];

    public class Validator : AbstractValidator<RenameModel>
    {
        public Validator(IStringLocalizer<RenameModel> localizer)
        {
            RuleFor(m => m.NewName)
                .NotNull()
                .NotEmpty()
                .WithMessage(localizer["notEmpty"])
                .NotEqual(model => model.OldName)
                .WithMessage(localizer["notEqual"])
                .Must((model, value) => !(model.ExcludedNames?.Contains(value) ?? false))
                .WithMessage(localizer["alreadyTaken"]);
        }
    }
}