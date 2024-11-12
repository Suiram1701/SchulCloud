using Microsoft.AspNetCore.Components;

namespace SchulCloud.Frontend.Components.Pages.Account.Security;

[Route("/account/security/apiKey/{apiKeyId}")]
public sealed partial class ApiKeyDetails
{
    [Parameter]
    public string ApiKeyId { get; set; } = default!;
}
