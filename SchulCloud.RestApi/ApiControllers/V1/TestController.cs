using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;

namespace SchulCloud.RestApi.ApiControllers.V1;

/// <summary>
/// A simple test.
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route($"{VersionPrefix}/tests")]
public sealed class TestController : ControllerBase
{
    /// <summary>
    /// A simple test and preview of a api method.
    /// </summary>
    [HttpGet]
    public IActionResult Get() => Ok("Works");
}
