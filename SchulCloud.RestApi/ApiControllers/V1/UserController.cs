using Asp.Versioning;
using Mapster;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using SchulCloud.Authorization;
using SchulCloud.Authorization.Attributes;
using SchulCloud.Authorization.Extensions;
using SchulCloud.Identity.Services;
using SchulCloud.RestApi.Extensions;
using SchulCloud.RestApi.Models;
using static System.Net.Mime.MediaTypeNames;

namespace SchulCloud.RestApi.ApiControllers.V1;

/// <summary>
/// Operations for users.
/// </summary>
[ApiController]
[ApiVersion(1)]
[Route($"{VersionPrefix}/users")]
public sealed class UserController(ILogger<UserController> logger, IAuthorizationService authorizationService, AppUserManager userManager, AppRoleManager roleManager) : ControllerBase
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
    /// <remarks>
    /// This endpoint can be called without any permission but some fields of the model are only set of the permission **Users** is **Read** or greater is available.
    /// </remarks>
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

        User userDto = user.Adapt<User>();
        if (!(await authorizationService.RequirePermissionAsync(User, Permissions.Users, PermissionLevel.Read).ConfigureAwait(false)).Succeeded)
        {
            // The permission Users >= Read is required to get these fields.
            userDto.Email = null;
            userDto.PhoneNumber = null;
        }

        string requestingUserId = userManager.GetUserId(User)!;
        logger.LogTrace("User '{userId}' requested user '{requestUserId}'", requestingUserId, userId);

        return Ok(userDto);
    }

    /// <summary>
    /// Gets the roles a user has.
    /// </summary>
    /// <param name="userId">The id of the user to get the roles from.</param>
    /// <returns>A list of roles.</returns>
    /// <response code="200">Returns a list of roles the user has.</response>
    /// <response code="404">No user with the requested id was found.</response>
    [HttpGet("{userId}/roles")]
    [FilteringFilter<User>]
    [SortingFilter<User>]
    [ProducesResponseType<Role[]>(StatusCodes.Status200OK, Application.Json)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, Application.ProblemJson)]
    [RequirePermission(Permissions.Users, PermissionLevel.Read)]
    public async Task<IActionResult> GetRolesAsync([FromRoute] string userId)
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
    [ProducesResponseType<FileStreamResult>(StatusCodes.Status200OK, Image.Png)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetProfileImageAsync([FromRoute] string userId)
    {
        ApplicationUser? user = await userManager.FindByIdAsync(userId).ConfigureAwait(false);
        if (user is null)
            return UserNotFoundResponse(userId);

        Stream? profileImage = await userManager.GetProfileImageAsync(user).ConfigureAwait(false);
        return profileImage is not null
            ? File(profileImage, Image.Png)
            : NoContent();
    }

    /// <summary>
    /// Updates the profile image of a certain user.
    /// </summary>
    /// <remarks>
    /// This endpoint can be called without any permission if **userId** is the id of the API key owner otherwise 403 will be returned. 
    /// If the permissions **Users** is **Write** or higher any users' image can be changed.
    /// </remarks>
    /// <param name="userId">The id of the user to get the profile image from.</param>
    /// <param name="image">The new image to set. Acceptable image formats are PNG, QOI, PBM, BMP, WebP, JPEG, GIF, TGA and TIFF.</param>
    /// <response code="204">The image were changed successfully.</response>
    /// <response code="400">The uploaded image were invalid.</response>
    /// <response code="403">The current user isn't permitted to change the profile image.</response>
    /// <response code="404">No user with the requested id was found.</response>
    [HttpPut("{userId}/image")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest, Application.ProblemJson)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden, Application.ProblemJson)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, Application.ProblemJson)]
    public async Task<IActionResult> UpdateProfileImageAsync([FromRoute] string userId, IFormFile image)
    {
        ApplicationUser? user = await userManager.FindByIdAsync(userId).ConfigureAwait(false);
        if (user is null)
            return UserNotFoundResponse(userId);

        ObjectResult? authResponse = await CheckProfileImageAccessAsync(userId).ConfigureAwait(false);
        if (authResponse is not null)
            return authResponse;

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
    /// <remarks>
    /// This endpoint can be called without any permission if **userId** is the id of the API key owner otherwise 403 will be returned. 
    /// If the permissions **Users** is **Write** or higher any users' image can be removed.
    /// </remarks>
    /// <param name="userId">The id of the user to remove the profile image for.</param>
    /// <response code="204">The image were successfully removed.</response>
    /// <response code="403">The current user isn't permitted to change the profile image.</response>
    /// <response code="404">No user with the requested id was found.</response>
    [HttpDelete("{userId}/image")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden, Application.ProblemJson)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, Application.ProblemJson)]
    public async Task<IActionResult> DeleteProfileImageAsync([FromRoute] string userId)
    {
        ApplicationUser? user = await userManager.FindByIdAsync(userId).ConfigureAwait(false);
        if (user is null)
            return UserNotFoundResponse(userId);

        ObjectResult? authResponse = await CheckProfileImageAccessAsync(userId).ConfigureAwait(false);
        if (authResponse is not null)
            return authResponse;

        IdentityResult deleteResult = await userManager.RemoveProfileImageAsync(user).ConfigureAwait(false);
        return deleteResult.Succeeded
            ? NoContent()
            : this.IdentityErrors(deleteResult.Errors);
    }

    private ObjectResult UserNotFoundResponse(string userId)
    {
        return Problem(
                title: "User not found",
                statusCode: StatusCodes.Status404NotFound,
                detail: "No user with the specified ID was found.",
                extensions: new Dictionary<string, object?> { { "UserId", userId } });
    }

    /// <summary>
    /// Checks whether the current user is authorized to change a certain user's profile image.
    /// </summary>
    /// <param name="userId">The user to check the access to.</param>
    /// <returns>If <c>null</c> authorized if not it is the error response.</returns>
    private async Task<ObjectResult?> CheckProfileImageAccessAsync(string userId)
    {
        if (userManager.GetUserId(HttpContext.User) == userId)
        {
            return null;
        }
        if ((await authorizationService.RequirePermissionAsync(HttpContext.User, Permissions.Users, PermissionLevel.Write)).Succeeded)
        {
            return null;
        }
        else
        {
            return Problem(
                statusCode: StatusCodes.Status403Forbidden,
                detail: "The current user isn't authorized to change this user's profile image.",
                extensions: new Dictionary<string, object?> { { "UserId", userId } });
        }
    }
}
