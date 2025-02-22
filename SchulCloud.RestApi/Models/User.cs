namespace SchulCloud.RestApi.Models;

/// <summary>
/// A user of the application.
/// </summary>
/// <param name="Id">The unique identifier of the user.</param>
/// <param name="UserName">The unique name of the user.</param>
/// <param name="Email">The email of the user. This field will only be returned if the request was made with the permission **Users** at the level **Read** or greater.</param>
/// <param name="PhoneNumber">The phone number of this user. This field will only be returned if the request was made with the permission **Users** at the level **Read** or greater.</param>
public record User(string Id, string UserName, string? Email, string? PhoneNumber);