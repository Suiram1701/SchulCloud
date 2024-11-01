using Microsoft.AspNetCore.Components;

namespace SchulCloud.Web.Components.Pages.Error;

[Route("/error/{code:int}")]
public sealed partial class Error : ComponentBase
{
    [Parameter]
    public int Code { get; set; }
}
