using System.Net;
using Microsoft.OpenApi.Models;
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
		operation.Responses.Add(HttpStatusCode.InternalServerError.ToString("D"), new OpenApiResponse
		{
			Description = "Internal Server Error",
			Content = new Dictionary<string, OpenApiMediaType>
			{
				["application/json"] = new()
				{
					Schema = context.SchemaGenerator.GenerateSchema(typeof(CapturedException), context.SchemaRepository)
				}
			}
		});
	}
}
