namespace SchulCloud.FileStorage.Abstractions;

/// <summary>
/// A file store interface providing functionality to get and set users' profile images.
/// </summary>
/// <typeparam name="TUser">The type of user.</typeparam>
public interface IProfileImageStore<TUser>
    where TUser : class
{
    /// <summary>
    /// Retrieves the profile image of a certain user.
    /// </summary>
    /// <param name="user">The user to get the image of.</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>The image. If <c>null</c> the user doesn't have an image.</returns>
    public Task<Stream?> GetImageAsync(TUser user, CancellationToken ct);

    /// <summary>
    /// Updates the profile image of a certain user.
    /// </summary>
    /// <param name="user">The user to update the image of.</param>
    /// <param name="image">The new image set.</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Indicates whether the update was successful.</returns>
    public Task<bool> UpdateImageAsync(TUser user, Stream image, CancellationToken ct);

    /// <summary>
    /// Removes the profile image of a certain user.
    /// </summary>
    /// <param name="user">The user to remove the image of.</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Indicates whether the removal was successful.</returns>
    public Task<bool> RemoveImageAsync(TUser user, CancellationToken ct);
}