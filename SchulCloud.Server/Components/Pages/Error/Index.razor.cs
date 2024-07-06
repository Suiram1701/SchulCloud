using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components;

namespace SchulCloud.Server.Components.Pages.Error;

[AllowAnonymous]
[Route("/error/{code:int}")]
public sealed partial class Index : ComponentBase
{
    [Parameter]
    public int Code { get; set; }
}
