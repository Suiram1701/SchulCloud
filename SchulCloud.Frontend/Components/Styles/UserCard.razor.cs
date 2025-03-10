using Microsoft.AspNetCore.Components;
using SchulCloud.FileStorage;

namespace SchulCloud.Frontend.Components.Styles;

public sealed partial class UserCard : ComponentBase
{
    #region Injections
    [Inject]
    private ApplicationUserManager UserManager { get; set; } = default!;
    #endregion

    [Parameter]
    public ApplicationUser User { get; set; } = default!;

    private string? _username;
    private string? _base64ProfileImage = null;
    private bool _isInitialized;

    protected override async Task OnParametersSetAsync()
    {
        if (User is not null && !_isInitialized)     // using OnP
        {
            _username = await UserManager.GetUserNameAsync(User);

            await using FileResultInfo? imageInfo = await UserManager.GetProfileImageAsync(User);
            if (imageInfo is not null)
            {
                using MemoryStream memory = new();
                await imageInfo.FileStream.CopyToAsync(memory).ConfigureAwait(false);

                _base64ProfileImage = Convert.ToBase64String(memory.GetBuffer());
            }

            _isInitialized = true;
        }
    }

    private static string NameToDisplayedAvatar(string username)
    {
        if (string.IsNullOrWhiteSpace(username))
            return string.Empty;
        return string.Concat(username.Split(' ', 2).Select(part => part.First()));
    }
}
