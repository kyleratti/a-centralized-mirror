using Microsoft.OpenApi;
using Swashbuckle.AspNetCore.SwaggerGen;
using WebApi.Middleware;

namespace WebApi.Swagger;

/// <summary>
/// JsonExceptionResponseOperationFilter
/// </summary>
public class JsonExceptionResponseOperationFilter : IOperationFilter
{
	/// <inheritdoc />
	public void Apply(OpenApiOperation operation, OperationFilterContext context)
	{
		operation.Responses ??= new OpenApiResponses();
		operation.Responses.Add(StatusCodes.Status500InternalServerError.ToString("D"), new OpenApiResponse
		{
			Description = "Internal Server Error",
			Content = new Dictionary<string, IOpenApiMediaType>
			{
				["application/json"] = new OpenApiMediaType
				{
					Schema = context.SchemaGenerator.GenerateSchema(typeof(CapturedException), context.SchemaRepository)
				}
			}
		});
	}
}
