using Fido2NetLib.Objects;

namespace SchulCloud.RestApi.Models;

/// <summary>
/// A FIDO2 credential used by a user on the site.
/// </summary>
/// <param name="Name">The user defined name of this credential.</param>
/// <param name="IsPasskey">Indicates whether this key is be able to perform a username less sign in.</param>
/// <param name="SignCount">The count of signatures created with this credential.</param>
/// <param name="Transports">Flags that provides information about the used security key.</param>
/// <param name="RegDate">The UTC date time where this credential were added.</param>
/// <param name="AaGuid">The AaGuid (authenticator attestion guid) of the used key.</param>
public record Fido2Credential(string? Name, bool IsPasskey, uint SignCount, AuthenticatorTransport[] Transports, DateTime RegDate, Guid AaGuid); 