using System.Net;
using Microsoft.OpenApi.Models;
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
		operation.Responses.Add(HttpStatusCode.Unauthorized.ToString("D"), new OpenApiResponse
		{
			Description = "Unauthorized",
			Content = new Dictionary<string, OpenApiMediaType>
			{
				["application/json"] = new()
				{
					Schema = context.SchemaGenerator.GenerateSchema(typeof(UnauthorizedResponse), context.SchemaRepository)
				}
			}
		});
	}
}
