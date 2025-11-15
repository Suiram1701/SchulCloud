using Microsoft.AspNetCore.WebUtilities;
using Swashbuckle.AspNetCore.SwaggerGen;
using System.Text.Json.Nodes;
using Microsoft.OpenApi;

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
        foreach ((string status, IOpenApiResponse response) in operation.Responses ?? new OpenApiResponses())
        {
            if (!(int.TryParse(status, out int statusCode) && statusCode >= 400))
                continue;
            if (response.Content is null)
                continue;
            
            foreach ((_, OpenApiMediaType mediaType) in response.Content.Where(kvp => kvp.Key == Application.ProblemJson))
            {
                string typeSection = statusCode < 500
                    ? $"5.{statusCode - 399}"
                    : $"6.{statusCode - 499}";
                string exampleResponse = string.Format(
                    format: _problemDetailResponse,
                    arg0: typeSection,
                    arg1: ReasonPhrases.GetReasonPhrase(statusCode),
                    arg2: statusCode);

                mediaType.Example = JsonNode.Parse(exampleResponse);
            }
        }
    }
}
