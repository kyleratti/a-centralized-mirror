using System.Reflection;
using ApplicationData;
using ApplicationData.Services;
using BackgroundProcessor;
using BackgroundProcessor.Templates;
using Core.AppSettings;
using Core.DbConnection;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SnooBrowser.Extensions.DependencyInjection;
using Swashbuckle.AspNetCore.SwaggerUI;
using WebApi.AuthHandlers;
using WebApi.Middleware;
using WebApi.Models.Swagger;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
ConfigureServices(builder.Services);

builder.Services
	.AddAuthentication("ApiKey")
	.AddScheme<ApiKeyAuthSchemeOptions, ApiKeyAuthHandler>("ApiKey", _ => { });

builder.Services.AddControllers(c =>
{
	c.Filters.Add(new ProducesAttribute("application/json")); // Only show application/json as the available Media Types in Swagger. 
});

// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(opts =>
{
	var xmlFilename = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
	opts.IncludeXmlComments(Path.Combine(AppContext.BaseDirectory, xmlFilename));
	opts.SupportNonNullableReferenceTypes();

	opts.OperationFilter<JsonExceptionResponseOperationFilter>();
	opts.OperationFilter<UnauthorizedResponseOperationFilter>();
	opts.EnableAnnotations();
	opts.UseInlineDefinitionsForEnums();
});

var app = builder.Build();

// Configure the HTTP request pipeline.
app.UseSwagger(c =>
{
	c.RouteTemplate = "/{documentname}/swagger.json";
});

app.UseSwaggerUI(opts =>
{
	opts.SwaggerEndpoint("/v1/swagger.json", "A Centralized Mirror API: v1");
	opts.SupportedSubmitMethods(Array.Empty<SubmitMethod>()); // Disable the 'Try it out' button. All endpoints require API keys so it's useless anyway.
	opts.RoutePrefix = "docs";
	opts.DocumentTitle = "A Centralized Mirror API Documentation";
	opts.EnableDeepLinking();
	opts.InjectStylesheet("/css/swagger.css");
	opts.IndexStream = () => Assembly.GetExecutingAssembly().GetManifestResourceStream("WebApi.Resources.DocsTemplate.html");
});

app.UseStaticFiles();

app.UseHttpsRedirection();

app.UseJsonExceptionHandler();

app.MapControllers();

// These MUST be the last thing added, and authentication MUST come first.
// If these are higher up in the file, the Swagger UI will be behind authorization - which we do not want.
app.UseAuthentication();
app.UseAuthorization();

app.Run();

void ConfigureServices(IServiceCollection services)
{
	services.AddAuthorization(opts =>
	{
		var policy = new AuthorizationPolicyBuilder()
			.RequireClaim("UserId")
			//.RequireAuthenticatedUser()
			.Build();
		opts.FallbackPolicy = policy;
	});

	/*services.AddAuthentication(opts =>
	{
		opts.DefaultAuthenticateScheme = "";
		opts.DefaultChallengeScheme = "";
	});*/
	
	ConfigureAppSettings<RawDbSettings, DbSettings>(services, "DbSettings");
	ConfigureAppSettings<RawRedditSettings, RedditSettings>(services, "RedditSettings");

	services.AddMemoryCache();

	services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();

	services.AddSnooBrowserClient<RedditAuthParameterProvider, RedditAccessTokenProvider>();

	services.AddScoped<DbConnection>();
	services.AddScoped<DbTransactionFactory>();
	services.AddScoped<ApiKeyProvider>();
	services.AddScoped<LinkProvider>();
	services.AddScoped<UserProvider>();
	services.AddScoped<RedditCommentProvider>();

	services.AddScoped<UserCache>();

	services.AddSingleton<GlobalMemoryCache>();
	services.AddSingleton<TemplateCache>();
	services.AddHostedService<BackgroundServiceWorker>();
}

void ConfigureAppSettings<TRaw, TService>(IServiceCollection services, string sectionName)
	where TRaw : class
	where TService : class
{
	services.AddOptions<TRaw>()
		.Bind(builder.Configuration.GetSection(sectionName))
		.ValidateDataAnnotations()
		.ValidateOnStart();

	services.AddScoped<TService>();
}