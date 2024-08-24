using Microsoft.AspNetCore.Components;

namespace SchulCloud.Web.Components.Pages.Error;

[Route("/error/{code:int}")]
public sealed partial class Index : ComponentBase
{
    [Parameter]
    public int Code { get; set; }
}
