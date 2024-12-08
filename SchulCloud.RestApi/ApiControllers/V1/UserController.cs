using Asp.Versioning;
using Mapster;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SchulCloud.Authorization;
using SchulCloud.Authorization.Attributes;
using SchulCloud.Authorization.Extensions;
using SchulCloud.RestApi.Models;
using System.Net.Mime;

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
    [ProducesResponseType<User[]>(StatusCodes.Status200OK, MediaTypeNames.Application.Json)]
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
    [ProducesResponseType<User>(StatusCodes.Status200OK, MediaTypeNames.Application.Json)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, MediaTypeNames.Application.ProblemJson)]
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
    [ProducesResponseType<Role[]>(StatusCodes.Status200OK, MediaTypeNames.Application.Json)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, MediaTypeNames.Application.ProblemJson)]
    [RequirePermission(Permissions.Users, PermissionLevel.Read)]
    public async Task<IActionResult> GetRolesAsync([FromRoute] string userId)
    {
        ApplicationUser? user = await userManager.FindByIdAsync(userId).ConfigureAwait(false);
        if (user is null)
        {
            return UserNotFoundResponse(userId);
        }

        IList<string> roleNames = await userManager.GetRolesAsync(user).ConfigureAwait(false);
        ApplicationRole[] roles = await Task.WhenAll(roleNames.Select(async name =>
        {
            return (await roleManager.FindByNameAsync(name).ConfigureAwait(false))!;
        })).ConfigureAwait(false);

        return Ok(roles.Adapt<Role[]>());
    }

    private ObjectResult UserNotFoundResponse(string roleId)
    {
        return Problem(
                title: "User not found",
                statusCode: 404,
                detail: "No user with the specified was found.",
                extensions: new Dictionary<string, object?> { { "UserId", roleId } });
    }
}
