namespace SchulCloud.ServiceDefaults.Services;

/// <summary>
/// An interface that provides managing functionalities for the data source used for the application.
/// </summary>
public interface IDataManager
{
    /// <summary>
    /// Initializes the data source used.
    /// </summary>
    /// <param name="ct">Cancellation token.</param>
    public Task InitializeDataSourceAsync(CancellationToken ct = default);

    /// <summary>
    /// Removes all the data this source stores.
    /// </summary>
    /// <param name="ct"></param>
    /// <returns></returns>
    public Task RemoveDataSourceAsync(CancellationToken ct = default);

    /// <summary>
    /// Removes expired API keys from the data source.
    /// </summary>
    /// <remarks>
    /// If the store doesn't supports API keys 0 will be returned.
    /// </remarks>
    /// <param name="ct">Cancellation token</param>
    /// <returns>The count of removed keys.</returns>
    public Task<int> RemoveObsoleteAPIKeysAsync(CancellationToken ct = default);

    /// <summary>
    /// Removes login attempts if too many of them are stored per user.
    /// </summary>
    /// <remarks>
    /// If the store doesn't supports login attempts 0 will be returned.
    /// </remarks>
    /// <param name="maxAttempts">The maximum amount of attempts to store per user before removing them.</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>The total amount of removed attempts.</returns>
    public Task<int> RemoveOldLoginAttemptsAsync(int maxAttempts = -1, CancellationToken ct = default);
}
