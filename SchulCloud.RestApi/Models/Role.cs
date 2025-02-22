namespace SchulCloud.RestApi.Models;

/// <summary>
/// A role users can have.
/// </summary>
/// <param name="Id">The unique identifier of the role.</param>
/// <param name="Name">The unique name of the role</param>
/// <param name="ArgbColor">The 32-Bit ARGB color this role has.</param>
public record Role(string Id, string Name, int? ArgbColor);