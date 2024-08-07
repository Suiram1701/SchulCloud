namespace SchulCloud.Web.Utils.Interfaces;

/// <summary>
/// An interface that provides a utility to use values through a whole request.
/// </summary>
public interface IRequestState : IDictionary<string, object>
{
    /// <summary>
    /// Gets the value of the specified key.
    /// </summary>
    /// <param name="key">The key.</param>
    /// <returns>The value. When the key couldn't found <c>null</c> will be returned.</returns>
    public object? GetValue(string key);
}
