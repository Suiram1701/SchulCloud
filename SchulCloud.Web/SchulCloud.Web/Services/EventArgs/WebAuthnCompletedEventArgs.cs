using System.Diagnostics.CodeAnalysis;

namespace SchulCloud.Web.Services.EventArgs;

/// <summary>
/// Event args of an completed webauthn operation.
/// </summary>
/// <remarks>
/// Creates a new instance.
/// </remarks>
/// <param name="result">The result of the operation.</param>
/// <param name="errorMessage">If failed the error message.</param>
public class WebAuthnCompletedEventArgs<TResult>(TResult? result, string? errorMessage) : System.EventArgs
{
    /// <summary>
    /// The result of the operation.
    /// </summary>
    public TResult? Result { get; } = result;

    /// <summary>
    /// The returned error message.
    /// </summary>
    public string? ErrorMessage { get; } = errorMessage;

    /// <summary>
    /// Indicates whether the operation was successful or not.
    /// </summary>
    [MemberNotNullWhen(true, nameof(Result))]
    public bool Successful => Result is not null;
}