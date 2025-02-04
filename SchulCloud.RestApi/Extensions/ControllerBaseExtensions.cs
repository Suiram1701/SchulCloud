using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace SchulCloud.RestApi.Extensions;

internal static class ControllerBaseExtensions
{
    /// <summary>
    /// Calls <see cref="ControllerBase.Problem(string?, string?, int?, string?, string?, IDictionary{string, object?}?)"/> with the provided <paramref name="errors"/> and <paramref name="statusCode"/>.
    /// </summary>
    /// <remarks>
    /// If <paramref name="errors"/> is empty an <see cref="InvalidOperationException"/> will be thrown.
    /// </remarks>
    /// <param name="controller">The controller of the calling action.</param>
    /// <param name="errors">The errors to return.</param>
    /// <param name="statusCode">The error status code to return.</param>
    /// <returns>The created problem response.</returns>
    public static ObjectResult IdentityErrors(this ControllerBase controller, IEnumerable<IdentityError> errors, int? statusCode = StatusCodes.Status500InternalServerError)
    {
        ArgumentNullException.ThrowIfNull(controller);

        if (!errors.Any())
            throw new InvalidOperationException("At least one error is required");

        if (errors.Count() == 1)
        {
            IdentityError error = errors.First();
            return controller.Problem(
                title: error.Code,
                statusCode: statusCode,
                detail: error.Description);
        }
        else
        {
            Dictionary<string, object?> extensions = new()
            {
                { "errors", errors.Select(error => new { error.Code, error.Description }) }
            };
            return controller.Problem(
                statusCode: statusCode,
                detail: "Multiple error occurred. See extension 'errors' for all errors.",
                extensions: extensions);
        }
    }
}
