using Microsoft.AspNetCore.Components;
using MudBlazor.Utilities;

namespace SchulCloud.Frontend.Components.Styles;

public partial class TextTruncate : ComponentBase
{
    protected virtual string ClassNames =>
        new CssBuilder("text-truncate")
            .AddClass(Class)
            .Build();

    [Parameter]
    public string? Class { get; set; }

    [Parameter]
    public RenderFragment? ChildContent { get; set; }
}
