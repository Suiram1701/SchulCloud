namespace SchulCloud.Frontend.Services.Exceptions;

/// <summary>
/// An exception that is thrown when a web authn operation fails.
/// </summary>
public class WebAuthnException : Exception
{
    public WebAuthnException() : base(null)
    {
    }

    public WebAuthnException(string? message) : base(message)
    {
    }
}
