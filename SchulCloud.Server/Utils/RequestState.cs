using SchulCloud.Server.Utils.Interfaces;

namespace SchulCloud.Server.Utils;

/// <summary>
/// A utility to use values through a whole request.
/// </summary>
public class RequestState : Dictionary<string, object>, IRequestState
{
    public object? GetValue(string key)
    {
        if (TryGetValue(key, out object? value))
        {
            return value;
        }

        return null;
    }
}
