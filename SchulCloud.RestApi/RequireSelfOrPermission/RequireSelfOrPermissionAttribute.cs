using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ApiExplorer;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.Formatters;
using SchulCloud.Authorization;
using SchulCloud.Authorization.Extensions;

namespace SchulCloud.RestApi.RequireSelfOrPermission;

/// <summary>
/// An authorization filter which requires the user to have a specific permission or request his own data.
/// </summary>
/// <param name="permission">The required permission.</param>
/// <param name="level">The least level of the permission required.</param>
/// <param name="selfParameter">The route data parameter which contains the requested user id for comparison.</param>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
public class RequireSelfOrPermissionAttribute(string permission, PermissionLevel level, string selfParameter = "userId") : Attribute, IAsyncAuthorizationFilter, IApiResponseMetadataProvider
{
    /// <summary>
    /// The name of the required permission is its not the current user.
    /// </summary>
    public string PermissionName => permission;

    /// <summary>
    /// The least required level of the permission.
    /// </summary>
    public PermissionLevel PermissionLevel => level;

    /// <summary>
    /// The name of the route parameter used to access the requested user id.
    /// </summary>
    public string SelfParameter => selfParameter;

    Type? IApiResponseMetadataProvider.Type => typeof(ProblemDetails);

    int IApiResponseMetadataProvider.StatusCode => StatusCodes.Status403Forbidden;

    /// <inheritdoc />
    public async Task OnAuthorizationAsync(AuthorizationFilterContext context)
    {
        ILogger logger = RequestService<ILogger<RequireSelfOrPermissionAttribute>>(context);
        var authorization = RequestService<IAuthorizationService>(context);
        UserManager<ApplicationUser> userManager = RequestService<UserManager<ApplicationUser>>(context);

        var requestingUser = context.RouteData.Values[selfParameter]?.ToString();
        if (requestingUser == userManager.GetUserId(context.HttpContext.User))     // Requested user isn't the authorized user.
            return;

        AuthorizationResult authResult = await authorization.RequirePermissionAsync(context.HttpContext.User, permission, level).ConfigureAwait(false);
        if (authResult.Succeeded)     // Authorized user has the right permission
            return;

        logger.LogInformation("Authorization failed because user isn't requested user or missing permission.");
        context.Result = new ForbidResult();
    }

    void IApiResponseMetadataProvider.SetContentTypes(MediaTypeCollection contentTypes) => contentTypes.Add(Application.ProblemJson);

    private static TService RequestService<TService>(AuthorizationFilterContext context) where TService : notnull
        => context.HttpContext.RequestServices.GetRequiredService<TService>();
}
