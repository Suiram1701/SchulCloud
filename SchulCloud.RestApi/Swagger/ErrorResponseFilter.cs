using Microsoft.AspNetCore.WebUtilities;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;
using System.Net.Mime;

namespace SchulCloud.RestApi.Swagger;

internal class ErrorResponseFilter : IOperationFilter
{
    private const string _problemDetailResponse = """
        {{
            "type": "https://tools.ietf.org/html/rfc9110#section-15.{0}",
            "title": "{1}",
            "status": {2},
            "detail": "",
            "traceId": ""
        }}
        """;

    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        foreach ((string status, OpenApiResponse response) in operation.Responses)
        {
            if (!(int.TryParse(status, out int statusCode) && statusCode >= 400))
            {
                continue;
            }

            foreach ((_, OpenApiMediaType mediaType) in response.Content.Where(kvp => kvp.Key == MediaTypeNames.Application.ProblemJson))
            {
                string typeSection = statusCode < 500
                    ? $"5.{statusCode - 399}"
                    : $"6.{statusCode - 499}";
                string exampleResponse = string.Format(
                    format: _problemDetailResponse,
                    arg0: typeSection,
                    arg1: ReasonPhrases.GetReasonPhrase(statusCode),
                    arg2: statusCode);

                mediaType.Example = OpenApiAnyFactory.CreateFromJson(exampleResponse);
            }
        }
    }
}
