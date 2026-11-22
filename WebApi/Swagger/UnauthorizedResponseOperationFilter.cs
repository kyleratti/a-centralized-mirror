using Microsoft.OpenApi;
using Swashbuckle.AspNetCore.SwaggerGen;
using WebApi.Models;

namespace WebApi.Swagger;

/// <summary>
/// UnauthorizedResponseOperationFilter
/// </summary>
public class UnauthorizedResponseOperationFilter : IOperationFilter
{
	/// <inheritdoc />
	public void Apply(OpenApiOperation operation, OperationFilterContext context)
	{
		operation.Responses ??= new OpenApiResponses();
		operation.Responses.Add(StatusCodes.Status401Unauthorized.ToString("D"), new OpenApiResponse
		{
			Description = "Unauthorized",
			Content = new Dictionary<string, IOpenApiMediaType>
			{
				["application/json"] = new OpenApiMediaType
				{
					Schema = context.SchemaGenerator.GenerateSchema(typeof(UnauthorizedResponse), context.SchemaRepository)
				}
			}
		});
	}
}
