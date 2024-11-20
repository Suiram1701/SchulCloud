using Microsoft.OpenApi.Models;

namespace SchulCloud.RestApi.Options;

/// <summary>
/// Provides information about an api that supports open api.
/// </summary>
internal class OpenApiOptions
{
    /// <summary>
    /// The name of the platform that provides the api.
    /// </summary>
    public string Title { get; set; } = "SchulCloud";

    /// <summary>
    /// A short description of the platform.
    /// </summary>
    public string Description { get; set; } = "A cloud application for students and teachers";

    /// <summary>
    /// An uri that provides a link to the terms of service.
    /// </summary>
    public Uri? TermsOfService { get; set; }

    /// <summary>
    /// The name of the person or organization that provides this api.
    /// </summary>
    public string? ContactName { get; set; }

    /// <summary>
    /// An email address to contact the person or organization.
    /// </summary>
    public string? ContactEmail { get; set; }

    /// <summary>
    /// An url to the website of the person or organization.
    /// </summary>
    public Uri? ContactUrl { get; set; }

    /// <summary>
    /// The name of the license this api uses.
    /// </summary>
    public string? LicenseName { get; set; }

    /// <summary>
    /// An url pointing to the license of this api.
    /// </summary>
    public Uri? LicenseUrl { get; set; }

    /// <summary>
    /// Creates an instance of <see cref="OpenApiInfo"/> using this instance.
    /// </summary>
    /// <returns>The instance created.</returns>
    public OpenApiInfo CreateOpenApiInfo()
    {
        OpenApiInfo info = new()
        {
            Title = Title,
            Description = Description,
            TermsOfService = TermsOfService,
        };

        if (ContactName is not null)
        {
            info.Contact = new()
            {
                Name = ContactName,
                Email = ContactEmail,
                Url = ContactUrl
            };
        }

        if (LicenseName is not null)
        {
            info.License = new()
            {
                Name = LicenseName,
                Url = LicenseUrl
            };
        }

        return info;
    }
}
