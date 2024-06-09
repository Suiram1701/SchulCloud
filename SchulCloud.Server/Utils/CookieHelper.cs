using Microsoft.AspNetCore.Http;
using Microsoft.JSInterop;
using Microsoft.Net.Http.Headers;
using SchulCloud.Server.Utils.Interfaces;
using System.Collections.Immutable;
using System.Text;

namespace SchulCloud.Server.Utils;

/// <summary>
/// A helper utility to use cookies.
/// </summary>
public class CookieHelper(IJSRuntime jS) : ICookieHelper
{
    private readonly IJSRuntime _jSRuntime = jS;

    public async ValueTask SetCookieAsync(string name, string value, CookieOptions options)
    {
        SetCookieHeaderValue headerValue = options.CreateCookieHeader(name, value);

        await _jSRuntime.InvokeVoidAsync("eval", $"document.cookie = \"{headerValue}\"");
    }

    public async ValueTask<string?> GetCookieAsync(string name)
    {
        ImmutableDictionary<string, string> cookies = await GetCookiesAsync();
        
        if (!cookies.TryGetValue(name, out var cookie))
        {
            return null;
        }

        return cookie;
    }

    public async ValueTask<ImmutableDictionary<string, string>> GetCookiesAsync()
    {
        string cookiesString = await _jSRuntime.InvokeAsync<string>("eval", "document.cookie");

        return cookiesString.Split(';')
            .Select(cookie => cookie.Trim().Split('=', 2))
            .ToImmutableDictionary(cookie => cookie.First(), cookie => cookie.Last());
    }

    public async ValueTask RemoveCookieAsync(string name)
    {
        string cookieString = $"{name}=; Max-Age=0;";
        await _jSRuntime.InvokeVoidAsync("eval", $"document.cookie = \"{cookieString}\"");
    }
}
