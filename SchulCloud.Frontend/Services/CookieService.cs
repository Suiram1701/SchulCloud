using Microsoft.JSInterop;
using Microsoft.Net.Http.Headers;
using MudBlazor;
using SchulCloud.Frontend.Extensions;

namespace SchulCloud.Frontend.Services;

/// <summary>
/// A service that provides access to user's cookies.
/// </summary>
public partial class CookieService(ILogger<CookieService> logger, IHttpContextAccessor contextAccessor, IJSRuntime jsRuntime)
{
    private readonly ILogger _logger = logger;
    private readonly IHttpContextAccessor _contextAccessor = contextAccessor;
    private readonly IJSRuntime _jSRuntime = jsRuntime;

    /// <summary>
    /// Sets a cookie.
    /// </summary>
    /// <param name="name">The name of the cookie to create or modify.</param>
    /// <param name="value">The value of the cookie</param>
    /// <param name="options">Options for the cookie.</param>
    /// <param name="ct">Cancellation token</param>
    public async ValueTask SetCookieAsync(string name, string value, CookieOptions options, CancellationToken ct = default)
    {
        if (_contextAccessor.HttpContext.IsContextValid())
        {
            _contextAccessor.HttpContext.Response.Cookies.Append(name, value, options);
        }
        else
        {
            SetCookieHeaderValue headerValue = options.CreateCookieHeader(name, value);

            bool success = await _jSRuntime.InvokeVoidAsyncWithErrorHandling("eval", ct, $"document.cookie = \"{headerValue}\"");
            if (!success)
            {
                LogSetFailed(name);
            }
        }
    }

    /// <summary>
    /// Gets the value of a cookie with a specified name.
    /// </summary>
    /// <param name="name">The name of the cookie.</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>The value of the cookie. If <c>null</c> there isn't such a cookie.</returns>
    public async ValueTask<string?> GetCookieAsync(string name, CancellationToken ct = default)
    {
        if (_contextAccessor.HttpContext.IsContextValid())
        {
            return _contextAccessor.HttpContext.Request.Cookies[name];
        }
        else
        {
            IEnumerable<KeyValuePair<string, string>> cookies = await GetCookiesAsync();
            return cookies.FirstOrDefault(kv => kv.Key.Equals(name)).Value;
        }
    }

    /// <summary>
    /// Gets all cookies.
    /// </summary>
    /// <param name="ct">Cancellation token</param>
    /// <returns>The cookies.</returns>
    public async ValueTask<IEnumerable<KeyValuePair<string, string>>> GetCookiesAsync(CancellationToken ct = default)
    {
        if (_contextAccessor.HttpContext.IsContextValid())
        {
            return _contextAccessor.HttpContext.Request.Cookies;
        }
        else
        {
            (bool success, string cookiesString) = await _jSRuntime.InvokeAsyncWithErrorHandling<string>(null!, "eval", ct, "document.cookie");
            if (!success)
            {
                LogGetFailed();
                return [];
            }

            return cookiesString.Split(';')
                .Select(cookie => cookie.Trim().Split('=', 2))
                .Select(parts => new KeyValuePair<string, string>(parts[0], parts[1]));
        }
    }

    /// <summary>
    /// Removes a cookie by a name.
    /// </summary>
    /// <param name="name">The name of the cookie to remove.</param>
    /// <param name="path">The path of the cookie.</param>
    /// <param name="ct">Cancellation token</param>
    public async ValueTask RemoveCookieAsync(string name, string? path = null, CancellationToken ct = default)
    {
        path ??= "/";

        if (_contextAccessor.HttpContext.IsContextValid())
        {
            _contextAccessor.HttpContext.Response.Cookies.Delete(name, new() { Path = path });
        }
        else
        {
            string cookieString = $"{name}=; path={path}; Max-Age=0;";

            bool success = await _jSRuntime.InvokeVoidAsyncWithErrorHandling("eval", ct, $"document.cookie = \"{cookieString}\"");
            if (!success)
            {
                LogRemoveFailed(name);
            }
        }
    }

    [LoggerMessage(LogLevel.Debug, "An error occurred by setting cookie '{name}' with js runtime")]
    private partial void LogSetFailed(string name);

    [LoggerMessage(LogLevel.Debug, "An error occurred by getting cookies with js runtime")]
    private partial void LogGetFailed();

    [LoggerMessage(LogLevel.Debug, "An error occurred by removing cookie '{name}' with js runtime")]
    private partial void LogRemoveFailed(string name);
}
