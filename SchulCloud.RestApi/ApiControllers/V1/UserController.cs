using Asp.Versioning;
using Mapster;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Net.Http.Headers;
using SchulCloud.Authorization;
using SchulCloud.Authorization.Attributes;
using SchulCloud.FileStorage;
using SchulCloud.Identity.Enums;
using SchulCloud.Identity.Models;
using SchulCloud.Identity.Services;
using SchulCloud.RestApi.Extensions;
using SchulCloud.RestApi.Models;

namespace SchulCloud.RestApi.ApiControllers.V1;

/// <summary>
/// Operations for users.
/// </summary>
[ApiController]
[ApiVersion(1)]
[Route($"{VersionPrefix}/users")]
public sealed class UserController(ILogger<UserController> logger, AppUserManager userManager) : ControllerBase
{
    /// <summary>
    /// Gets every user that is registered on the site.
    /// </summary>
    /// <returns>The list of users</returns>
    /// <response code="200">Returns a list of users of the site.</response>
    [HttpGet]
    [FilteringFilter<User>]
    [SortingFilter<User>]
    [PaginationFilter<User>]
    [ProducesResponseType<User[]>(StatusCodes.Status200OK, Application.Json)]
    [RequirePermission(Permissions.Users, PermissionLevel.Read)]
    public IActionResult GetAll()
    {
        if (!userManager.SupportsQueryableUsers)
        {
            return Problem(statusCode: 501, detail: "The server does not support this operation.");
        }

        return Ok(userManager.Users.ProjectToType<User>());
    }

    /// <summary>
    /// Gets a single user by his id.
    /// </summary>
    /// <param name="userId">The id of the user to get.</param>
    /// <returns>The user that has the requested id.</returns>
    /// <response code="200">Returns the user that has the requested id.</response>
    /// <response code="404">No user with the requested id was found.</response>
    [HttpGet("{userId}")]
    [ProducesResponseType<User>(StatusCodes.Status200OK, Application.Json)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, Application.ProblemJson)]
    public async Task<IActionResult> GetAsync([FromRoute] string userId)
    {
        ApplicationUser? user = await userManager.FindByIdAsync(userId).ConfigureAwait(false);
        if (user is null)
        {
            return UserNotFoundResponse(userId);
        }

        string requestingUserId = userManager.GetUserId(User)!;
        logger.LogTrace("User '{userId}' requested user '{requestUserId}'", requestingUserId, userId);

        return Ok(user.Adapt<User>());
    }

    /// <summary>
    /// Gets the roles a user has.
    /// </summary>
    /// <remarks>
    /// Requesting the roles of the requesting user doesn't require any permission but for any other user permission **Users** with level read or greater is required.
    /// </remarks>
    /// <param name="userId">The id of the user to get the roles from.</param>
    /// <param name="roleManager"></param>
    /// <returns>A list of roles.</returns>
    /// <response code="200">Returns a list of roles the user has.</response>
    /// <response code="404">No user with the requested id was found.</response>
    [HttpGet("{userId}/roles")]
    [RequireSelfOrPermission(Permissions.Users, PermissionLevel.Read)]
    [FilteringFilter<User>]
    [SortingFilter<User>]
    [ProducesResponseType<Role[]>(StatusCodes.Status200OK, Application.Json)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, Application.ProblemJson)]
    public async Task<IActionResult> GetRolesAsync([FromRoute] string userId, [FromServices] AppRoleManager roleManager)
    {
        ApplicationUser? user = await userManager.FindByIdAsync(userId).ConfigureAwait(false);
        if (user is null)
            return UserNotFoundResponse(userId);

        IList<string> roleNames = await userManager.GetRolesAsync(user).ConfigureAwait(false);
        ApplicationRole[] roles = await Task.WhenAll(roleNames.Select(async name =>
        {
            return (await roleManager.FindByNameAsync(name).ConfigureAwait(false))!;
        })).ConfigureAwait(false);

        return Ok(roles.Adapt<Role[]>());
    }

    /// <summary>
    /// Retrieves the profile image of a certain user.
    /// </summary>
    /// <param name="userId">The id of the user to get the profile image from.</param>
    /// <response code="200">Returns the profile image of the user.</response>
    /// <response code="204">The user doesn't have a profile image.</response>
    /// <response code="404">No user with the requested id was found.</response>
    [HttpGet("{userId}/image")]
    [ProducesFileResponse(Image.Png, supportsRangeProcessing: true)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, Application.ProblemJson)]
    public async Task<IActionResult> GetProfileImageAsync([FromRoute] string userId)
    {
        ApplicationUser? user = await userManager.FindByIdAsync(userId).ConfigureAwait(false);
        if (user is null)
            return UserNotFoundResponse(userId);

        FileResultInfo? profileImageInfo = await userManager.GetProfileImageAsync(user).ConfigureAwait(false);
        if (profileImageInfo is not null)
        {
            // range processing requires a seekable stream
            Stream responseFile = await EnsureSeekableAsync(
                sourceStream: profileImageInfo.FileStream,
                disposeIfNot: true,
                ct: HttpContext.RequestAborted).ConfigureAwait(false);

            return File(
                fileStream: responseFile,
                contentType: profileImageInfo.ContentType ?? Image.Png,
                lastModified: profileImageInfo.LastModified,
                entityTag: EntityTagHeaderValue.Parse(profileImageInfo.ETag),
                enableRangeProcessing: true);
        }
        else
        {
            return NoContent();
        }
    }

    /// <summary>
    /// Updates the profile image of a certain user.
    /// </summary>
    /// <param name="userId">The id of the user to get the profile image from.</param>
    /// <param name="image">The new image to set. Acceptable image formats are PNG, QOI, PBM, BMP, WebP, JPEG, GIF, TGA and TIFF.</param>
    /// <response code="204">The image were changed successfully.</response>
    /// <response code="400">The uploaded image were invalid.</response>
    /// <response code="404">No user with the requested id was found.</response>
    [HttpPut("{userId}/image")]
    [RequireSelfOrPermission(Permissions.Users, PermissionLevel.Write)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest, Application.ProblemJson)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, Application.ProblemJson)]
    public async Task<IActionResult> UpdateProfileImageAsync([FromRoute] string userId, IFormFile image)
    {
        ApplicationUser? user = await userManager.FindByIdAsync(userId).ConfigureAwait(false);
        if (user is null)
            return UserNotFoundResponse(userId);

        using Stream imageStream = image.OpenReadStream();
        IdentityResult updateResult = await userManager.UpdateProfileImageAsync(user, imageStream).ConfigureAwait(false);

        if (!updateResult.Succeeded)
        {
            int errorCode = updateResult.Errors.Any(error => error.Code == nameof(ExtendedIdentityErrorDescriber.BadImage))
                ? StatusCodes.Status400BadRequest
                : StatusCodes.Status500InternalServerError;
            return this.IdentityErrors(updateResult.Errors, errorCode);
        }
        return NoContent();
    }

    /// <summary>
    /// Removes the profile image of a certain user.
    /// </summary>
    /// <param name="userId">The id of the user to remove the profile image for.</param>
    /// <response code="204">The image were successfully removed.</response>
    /// <response code="404">No user with the requested id was found.</response>
    [HttpDelete("{userId}/image")]
    [RequireSelfOrPermission(Permissions.Users, PermissionLevel.Write)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, Application.ProblemJson)]
    public async Task<IActionResult> DeleteProfileImageAsync([FromRoute] string userId)
    {
        ApplicationUser? user = await userManager.FindByIdAsync(userId).ConfigureAwait(false);
        if (user is null)
            return UserNotFoundResponse(userId);

        IdentityResult deleteResult = await userManager.RemoveProfileImageAsync(user).ConfigureAwait(false);
        return deleteResult.Succeeded
            ? NoContent()
            : this.IdentityErrors(deleteResult.Errors);
    }

    /// <summary>
    /// Retrieves the security settings of a specific user.
    /// </summary>
    /// <param name="userId">The id of the user to retrieve the security settings for.</param>
    /// <response code="200">Returns the security settings of the user.</response>
    /// <response code="404">No user with the requested id was found.</response>
    [HttpGet("{userId}/security")]
    [RequireSelfOrPermission(Permissions.Users, PermissionLevel.Read)]
    [ProducesResponseType<SecuritySettings>(StatusCodes.Status200OK, Application.Json)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, Application.ProblemJson)]
    public async Task<IActionResult> GetSecuritySettingsAsync([FromRoute] string userId)
    {
        ApplicationUser? user = await userManager.FindByIdAsync(userId).ConfigureAwait(false);
        if (user is null)
            return UserNotFoundResponse(userId);

        // Check which 2fa methods are enabled
        HashSet<TwoFactorMethod>? enabledMethods = null;
        if (userManager.SupportsUserTwoFactor && await userManager.GetTwoFactorEnabledAsync(user).ConfigureAwait(false))
        {
            enabledMethods = [TwoFactorMethod.Authenticator];     // Always enabled if 2fa is enabled

            if (userManager.SupportsUserTwoFactorEmail && await userManager.GetTwoFactorEmailEnabledAsync(user).ConfigureAwait(false))
                enabledMethods.Add(TwoFactorMethod.Email);
            if (userManager.SupportsUserTwoFactorSecurityKeys && await userManager.GetTwoFactorSecurityKeyEnableAsync(user).ConfigureAwait(false))
                enabledMethods.Add(TwoFactorMethod.SecurityKey);
            if (userManager.SupportsUserTwoFactorRecoveryCodes && await userManager.CountRecoveryCodesAsync(user).ConfigureAwait(false) > 0)
                enabledMethods.Add(TwoFactorMethod.Recovery);
        }

        // Fetch and convert FIDO2 credentials
        Fido2Credential[]? credentials = null;
        if (userManager.SupportsUserCredentials)
        {
            IEnumerable<UserCredential> userCredentials = await userManager.FindFido2CredentialsByUserAsync(user).ConfigureAwait(false);
            credentials = await Task.WhenAll(userCredentials.Select(async cred =>
            {
                return cred.Adapt<Fido2Credential>() with
                {
                    IsPasskey = await userManager.GetIsPasskeyAsync(cred).ConfigureAwait(false)
                };
            })).ConfigureAwait(false);
        }

        SecuritySettings settings = new(
            userManager.SupportsUserPassword
                ? await userManager.HasPasswordAsync(user).ConfigureAwait(false)
                : null,
            userManager.SupportsUserTwoFactor
                ? enabledMethods?.Count > 0
                : null,
            userManager.SupportsUserTwoFactor
                ? enabledMethods?.ToArray() ?? []
                : null,
            userManager.SupportsUserPasskeys
                ? await userManager.GetPasskeySignInEnabledAsync(user).ConfigureAwait(false)
                : null,
            credentials);
        return Ok(settings);
    }

    /// <summary>
    /// Retrieves the login attempts of a user.
    /// </summary>
    /// <param name="userId">The id of the user to get the login attempts of.</param>
    /// <response code="200">Returns the login attempts of the user ordered descending by the login time.</response>
    /// <response code="404">No user with the requested id was found.</response>
    [HttpGet("{userId}/loginAttempts")]
    [RequireSelfOrPermission(Permissions.Users, PermissionLevel.Read)]
    [FilteringFilter<LoginAttempt>]
    [PaginationFilter<LoginAttempt>]
    [ProducesResponseType<LoginAttempt[]>(StatusCodes.Status200OK, Application.Json)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, Application.ProblemJson)]
    public async Task<IActionResult> GetLoginAttemptsAsync([FromRoute] string userId)
    {
        ApplicationUser? user = await userManager.FindByIdAsync(userId).ConfigureAwait(false);
        if (user is null)
            return UserNotFoundResponse(userId);

        IEnumerable<UserLoginAttempt> attempts = await userManager.FindLoginAttemptsByUserAsync(user).ConfigureAwait(false);
        return Ok(attempts.Adapt<LoginAttempt[]>(LoginAttempt._adapterConfig).OrderByDescending(a => a.DateTime));
    }

    [NonAction]
    private ObjectResult UserNotFoundResponse(string userId)
    {
        return Problem(
                title: "User not found",
                statusCode: StatusCodes.Status404NotFound,
                detail: "No user with the specified ID was found.",
                extensions: new Dictionary<string, object?> { { "UserId", userId } });
    }

    [NonAction]
    private static async Task<Stream> EnsureSeekableAsync(Stream sourceStream, bool disposeIfNot = false, CancellationToken ct = default)
    {
        if (!sourceStream.CanSeek)
        {
            MemoryStream stream = new();
            await sourceStream.CopyToAsync(stream, ct).ConfigureAwait(false);
            stream.Seek(0, SeekOrigin.Begin);

            if (disposeIfNot)
                await sourceStream.DisposeAsync().ConfigureAwait(false);
            return stream;
        }
        else
        {
            return sourceStream;
        }
    }
}
