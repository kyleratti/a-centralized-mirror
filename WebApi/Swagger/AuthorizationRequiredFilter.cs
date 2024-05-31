using Microsoft.AspNetCore.Authorization;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace WebApi.Swagger;

/// <summary>
/// Authorization required endpoint filter for Swagger.
/// </summary>
public class AuthorizationRequiredFilter : IOperationFilter
{
	/// <summary>
	/// API Key required scheme
	/// </summary>
	public static readonly OpenApiSecurityScheme ApiKeyScheme = new()
	{
		Name = "Authorization",
		Type = SecuritySchemeType.ApiKey,
		In = ParameterLocation.Header,
		Scheme = "Key",
		Description = """
		              API key using the Key scheme.
		              Enter 'Key' [space] and then your API key in the input below.
		              Example: Key 1234567890abcdef
		              """,
		Reference = new OpenApiReference
		{
			Id = "Key",
			Type = ReferenceType.SecurityScheme,
		},
	};

	/// <inheritdoc />
	public void Apply(OpenApiOperation operation, OperationFilterContext context)
	{
		var noAuthRequired = context.ApiDescription.CustomAttributes().Any(attr => attr.GetType() == typeof(AllowAnonymousAttribute));

		if (noAuthRequired)
			return;

		operation.Security =
		[
			new OpenApiSecurityRequirement
			{
				{ ApiKeyScheme, Array.Empty<string>() },
			}
		];
	}
}
