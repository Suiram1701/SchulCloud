using Asp.Versioning;
using Mapster;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using SchulCloud.Authorization;
using SchulCloud.Authorization.Attributes;
using SchulCloud.Authorization.Extensions;
using SchulCloud.RestApi.ActionFilters;
using SchulCloud.RestApi.Models;
using System.Net.Mime;

namespace SchulCloud.RestApi.ApiControllers.V1;

/// <summary>
/// Operations for roles.
/// </summary>
[ApiController]
[ApiVersion(1)]
[Route($"{VersionPrefix}/roles")]
public sealed class RoleController(ILogger<RoleController> logger, IAuthorizationService authorizationService, AppUserManager userManager, AppRoleManager roleManager) : ControllerBase
{
    /// <summary>
    /// Gets every role that is available.
    /// </summary>
    /// <remarks>
    /// Requires the permission **Roles** with level **Read** or greater.
    /// </remarks>
    /// <returns>A list of roles.</returns>
    /// <response code="200">Returns a list of roles.</response>
    [HttpGet]
    [PaginationFilter<Role>]
    [ProducesResponseType<Role[]>(StatusCodes.Status200OK, MediaTypeNames.Application.Json)]
    [RequirePermission(Permissions.Roles, PermissionLevel.Read)]
    public IActionResult GetAll()
    {
        if (!roleManager.SupportsQueryableRoles)
        {
            return Problem(statusCode: 501, detail: "The server does not support this operation.");
        }

        return Ok(roleManager.Roles.ProjectToType<Role>());
    }

    /// <summary>
    /// Gets a single role by its id.
    /// </summary>
    /// <param name="id">The id of the roles to get.</param>
    /// <returns>The role that has the requested id.</returns>
    /// <response code="200">Returns the role with the specified id.</response>
    /// <response code="404">No role with the requested id was found.</response>
    [HttpGet("{id}")]
    [ProducesResponseType<Role>(StatusCodes.Status200OK, MediaTypeNames.Application.Json)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, MediaTypeNames.Application.ProblemJson)]
    public async Task<IActionResult> GetAsync([FromRoute] string id)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            return Problem(statusCode: 404, detail: "No role with the specified was found.");
        }

        ApplicationRole? role = await roleManager.FindByIdAsync(id).ConfigureAwait(false);
        if (role is null)
        {
            return Problem(statusCode: 404, detail: "No role with the specified was found.");
        }

        string userId = userManager.GetUserId(User)!;
        logger.LogTrace("User '{userId}' requested role '{requestRole}'.", userId, id);

        return Ok(role.Adapt<Role>());
    }

    /// <summary>
    /// Gets every user that is in the role with the requested id.
    /// </summary>
    /// <remarks>
    /// Requires the permission **Roles** with level **Read** or greater.
    /// Some fields of a user are only set if the permission **Users** with level **Read** or greater is available.
    /// </remarks>
    /// <param name="id">The id of the role to get the users from.</param>
    /// <returns>A list of users.</returns>
    /// <response code="200">Returns a list of the users with the role.</response>
    /// <response code="404">No role with the specified id was found.</response>
    [HttpGet("{id}/users")]
    [PaginationFilter<User>]
    [ProducesResponseType<User[]>(StatusCodes.Status200OK, MediaTypeNames.Application.Json)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, MediaTypeNames.Application.ProblemJson)]
    [RequirePermission(Permissions.Roles, PermissionLevel.Read)]
    public async Task<IActionResult> GetRoleUsersAsync([FromRoute] string id)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            return Problem(statusCode: 404, detail: "No role with the specified was found.");
        }

        ApplicationRole? role = await roleManager.FindByIdAsync(id).ConfigureAwait(false);
        if (role is null)
        {
            return Problem(statusCode: 404, detail: "No role with the specified was found.");
        }

        string roleName = (await roleManager.GetRoleNameAsync(role).ConfigureAwait(false))!;
        IList<ApplicationUser> users = await userManager.GetUsersInRoleAsync(roleName).ConfigureAwait(false);
        IList<User> userDtos = users.Adapt<IList<User>>();

        if (!(await authorizationService.RequirePermissionAsync(User, Permissions.Users, PermissionLevel.Read).ConfigureAwait(false)).Succeeded)
        {
            foreach (User userDto in userDtos)
            {
                // The permission Users >= Read is required to get these fields.
                userDto.Email = null;
                userDto.PhoneNumber = null;
            }
        }

        string userId = userManager.GetUserId(User)!;
        logger.LogTrace("User '{userId}' requested users of role '{requestRole}'.", userId, id);

        return Ok(userDtos);
    }

    /// <summary>
    /// Adds a list of users to the specified role.
    /// </summary>
    /// <remarks>
    /// Requires the permission **Roles** with the level **Write** or greater.
    /// 
    /// Sample request:
    /// 
    ///     PATCH /v1/roles/{id}/users
    ///     [
    ///         "86d91795-89a7-4eca-8fdb-0180cab6b8b8",
    ///         "b4b7bbd0-4a5a-4481-917b-ac0a6c0c00d9",
    ///         "347cf3ee-b105-429c-97b1-edf5f15543d6"
    ///     ]
    ///     
    /// Users that are already in this role will get ignored.
    /// </remarks>
    /// <param name="id">The id of the role to add the users to.</param>
    /// <param name="userIds">The ids of the users to add.</param>
    /// <response code="204">The users were successfully added.</response>
    /// <response code="404">The role wasn't found by the id or one of the users wasn't found.</response>
    [HttpPatch("{id}/users")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest, MediaTypeNames.Application.ProblemJson)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, MediaTypeNames.Application.ProblemJson)]
    [RequirePermission(Permissions.Roles, PermissionLevel.Write)]
    public async Task<IActionResult> AddUsersToRole([FromRoute] string id, [FromBody] string[] userIds)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            return Problem(statusCode: 404, detail: "No role with the specified was found.");
        }

        ApplicationRole? role = await roleManager.FindByIdAsync(id).ConfigureAwait(false);
        if (role is null)
        {
            return Problem(statusCode: 404, detail: "No role with the specified was found.");
        }

        List<ApplicationUser> users = [];
        foreach (string userId in userIds)
        {
            ApplicationUser? user = await userManager.FindByIdAsync(userId).ConfigureAwait(false);
            if (user is not null)
            {
                users.Add(user);
            }
            else
            {
                return Problem(statusCode: 404, detail: $"No user with the id '{userId}' exists.");
            }
        }

        string operatingUserId = (userManager.GetUserId(User))!;

        List<string> errorsUsers = [];
        string roleName = (await roleManager.GetRoleNameAsync(role).ConfigureAwait(false))!;
        foreach (ApplicationUser user in users)
        {
            string userId = await userManager.GetUserIdAsync(user).ConfigureAwait(false);

            IdentityResult addResult = await userManager.AddToRoleAsync(user, roleName).ConfigureAwait(false);
            if (!addResult.Succeeded && addResult.Errors.All(error => error.Code != nameof(IdentityErrorDescriber.UserAlreadyInRole)))
            {
                logger.LogError("An error occurred while adding user '{userId}' to role '{roleId}'.", userId, id);
                errorsUsers.Add(userId);
            }
            else
            {
                logger.LogTrace("User '{operatingUserId}' added user '{userId}' to role '{roleId}'.", operatingUserId, userId, id);
            }
        }

        if (errorsUsers.Count == 0)
        {
            return NoContent();
        }
        else
        {
            Dictionary<string, object?> extensions = new()
            {
                { "Users", errorsUsers }
            };
            return Problem(statusCode: 500, detail: "An error occurred while adding one or more users to the role.", extensions: extensions);
        }
    }

    /// <summary>
    /// Removes a list of users from the specified role.
    /// </summary>
    /// <remarks>
    /// Requires the permission **Roles** with the level **Write** or greater.
    /// 
    /// Sample request:
    /// 
    ///     DELETE /v1/roles/{id}/users
    ///     [
    ///         "86d91795-89a7-4eca-8fdb-0180cab6b8b8",
    ///         "b4b7bbd0-4a5a-4481-917b-ac0a6c0c00d9",
    ///         "347cf3ee-b105-429c-97b1-edf5f15543d6"
    ///     ]
    /// </remarks>
    /// <param name="id">The id of the role to remove the users from.</param>
    /// <param name="userIds">The ids of the users to remove</param>
    /// <response code="204">The users were successfully removed.</response>
    /// <response code="400">One or more users could not be removed because there not in that role. The property 'users' in the response contains the ids of the users that could not be removed.</response>
    /// <response code="404">The role wasn't found by the id or one of the users wasn't found.</response>
    [HttpDelete("{id}/users")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest, MediaTypeNames.Application.ProblemJson)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, MediaTypeNames.Application.ProblemJson)]
    [RequirePermission(Permissions.Roles, PermissionLevel.Write)]
    public async Task<IActionResult> DeleteUsersFromRole([FromRoute] string id, [FromBody] string[] userIds)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            return Problem(statusCode: 404, detail: "No role with the specified was found.");
        }

        ApplicationRole? role = await roleManager.FindByIdAsync(id).ConfigureAwait(false);
        if (role is null)
        {
            return Problem(statusCode: 404, detail: "No role with the specified was found.");
        }

        List<ApplicationUser> users = [];
        foreach (string userId in userIds)
        {
            ApplicationUser? user = await userManager.FindByIdAsync(userId).ConfigureAwait(false);
            if (user is not null)
            {
                users.Add(user);
            }
            else
            {
                return Problem(statusCode: 404, detail: $"No user with the id '{userId}' exists.");
            }
        }

        string operatingUserId = (userManager.GetUserId(User))!;

        Dictionary<string, IdentityError[]> errors = [];
        string roleName = (await roleManager.GetRoleNameAsync(role).ConfigureAwait(false))!;
        foreach (ApplicationUser user in users)
        {
            string userId = await userManager.GetUserIdAsync(user).ConfigureAwait(false)!;

            IdentityResult removeResult = await userManager.RemoveFromRoleAsync(user, roleName).ConfigureAwait(false);
            if (!removeResult.Succeeded)
            {
                errors.Add(userId, removeResult.Errors.ToArray());
            }
            else
            {
                logger.LogTrace("User '{operatingUserId}' added user '{userId}' to role '{roleId}'.", operatingUserId, userId, id);
            }
        }

        if (errors.Count == 0)
        {
            return NoContent();
        }
        else
        {
            Dictionary<string, object?> extensions = new()
            {
                { "Users", errors.Select(kvp => kvp.Key) }
            };
            if (errors.SelectMany(kvp => kvp.Value).All(error => error.Code == nameof(IdentityErrorDescriber.UserNotInRole)))
            {
                return Problem(statusCode: 400, detail: "One or more users does not have the role.", extensions: extensions);
            }
            else
            {
                foreach ((string userId, IdentityError[] identityErrors) in errors)
                {
                    string errorsStr = string.Join(';', identityErrors.Select(err => err.Description));
                    logger.LogError("An error occurred while removing user '{userId}' from role '{roleId}'. Errors: {errors}", userId, id, errorsStr);
                }

                return Problem(statusCode: 500, detail: "An error occurred while removing one or more users from the role.", extensions: extensions);
            }
        }
    }
}
