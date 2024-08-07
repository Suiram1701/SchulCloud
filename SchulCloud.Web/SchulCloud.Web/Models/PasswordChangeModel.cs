using System.ComponentModel.DataAnnotations;

namespace SchulCloud.Web.Models;

public class PasswordChangeModel
{
    [Required]
    [DataType(DataType.Password)]
    public string CurrentPassword { get; set; } = string.Empty;

    [Required]
    [DataType(DataType.Password)]
    public string NewPassword { get; set; } = string.Empty;

    [Required]
    [DataType(DataType.Password)]
    public string ConfirmedPassword { get; set; } = string.Empty;
}
