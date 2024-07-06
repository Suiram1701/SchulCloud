using BlazorBootstrap;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Components;
using SchulCloud.Server.Constants;

namespace SchulCloud.Server.Components.Bootstrap.Forms;

/// <summary>
/// An input group that can be wrap over other bootstrap form components.
/// </summary>
public partial class InputGroup : BlazorBootstrapComponentBase
{
    [Parameter]
    public required RenderFragment ChildContent { get; set; }

    [CascadingParameter]
    private EditContext? EditContext { get; set; }

    protected override string ClassNames =>
        BuildClassNames(
            Class,
            (ExtendedBootstrapClass.InputGroup, true),
            (ExtendedBootstrapClass.HasValidation, EditContext is not null));
}
