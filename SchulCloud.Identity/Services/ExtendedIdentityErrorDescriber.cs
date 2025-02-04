using Microsoft.AspNetCore.Identity;

namespace SchulCloud.Identity.Services;

/// <summary>
/// An extended version of <see cref="IdentityErrorDescriber"/> that provides SchulCloud specific errors.
/// </summary>
public class ExtendedIdentityErrorDescriber : IdentityErrorDescriber
{
    /// <summary>
    /// Gets an error that indicates an invalid image were provided.
    /// </summary>
    /// <returns>The created error.</returns>
    public virtual IdentityError BadImage()
    {
        return new()
        {
            Code = nameof(BadImage),
            Description = "Bad image. Unable to process the provided image."
        };
    }

    /// <summary>
    /// Gets an error that indicates an update / removal error of a user's profile image.
    /// </summary>
    /// <param name="userId">The ID of the user whose profile image should be updated.</param>
    /// <returns>The created error.</returns>
    public virtual IdentityError ProfileImageError(string userId)
    {
        return new()
        {
            Code = nameof(ProfileImageError),
            Description = $"Unable to update or remove the profile image of user '{userId}'."
        };
    }
}
