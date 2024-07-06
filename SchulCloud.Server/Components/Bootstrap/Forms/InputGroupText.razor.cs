using BlazorBootstrap;
using Microsoft.AspNetCore.Components;
using SchulCloud.Server.Constants;

namespace SchulCloud.Server.Components.Bootstrap.Forms;

/// <summary>
/// A text component that can be placed inside of an <see cref="InputGroup"/>
/// </summary>
public partial class InputGroupText : BlazorBootstrapComponentBase
{
    /// <summary>
    /// The content that should be rendered into this component
    /// </summary>
    [Parameter]
    public required RenderFragment ChildContent { get; set; }

    protected override string ClassNames => BuildClassNames(Class, (ExtendedBootstrapClass.InputGroupText, true));
}
