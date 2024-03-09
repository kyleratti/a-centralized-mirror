using System.Data;
using System.Reflection;
using ApplicationData;
using ApplicationData.Services;
using BackgroundProcessor;
using BackgroundProcessor.Templates;
using Core.AppSettings;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.DataProtection.AuthenticatedEncryption;
using Microsoft.AspNetCore.DataProtection.AuthenticatedEncryption.ConfigurationModel;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration.AzureAppConfiguration;
using Microsoft.Extensions.Options;
using SnooBrowser.Extensions.DependencyInjection;
using Swashbuckle.AspNetCore.SwaggerUI;
using WebApi.AuthHandlers;
using WebApi.HostedServices;
using WebApi.Middleware;
using WebApi.Models.Swagger;
using WebApi.Options;
using WebApi.Services;

var builder = WebApplication.CreateBuilder(args);

const string PROJECT_ID = "ACM";

// Add services to the container.
builder.Configuration
	.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
	.AddJsonFile($"appsettings.{builder.Environment.EnvironmentName}.json", optional: true, reloadOnChange: true)
	.AddAzureAppConfiguration(options =>
	{
		var azureAppConfigConnectionString = builder.Configuration.GetConnectionString("AzureAppConfig");
		options.Connect(azureAppConfigConnectionString)
			.Select(keyFilter: $"{PROJECT_ID}:*", LabelFilter.Null)
			.Select(keyFilter: $"{PROJECT_ID}:*", labelFilter: builder.Environment.EnvironmentName)
			.ConfigureRefresh(x => x
				.Register($"{PROJECT_ID}:AzureAppConfig:Sentinel", refreshAll: true)
				.SetCacheExpiration(TimeSpan.FromHours(1)))
			.TrimKeyPrefix($"{PROJECT_ID}:");
	});

if (builder.Environment.IsDevelopment())
	builder.Configuration.AddUserSecrets<Program>(optional: true, reloadOnChange: true);

builder.WebHost.UseSentry();

ConfigureOptions(builder.Services, builder.Configuration);
ConfigureServices(builder.Services, builder.Configuration);
ConfigureDataAccess(builder.Services, builder.Configuration);

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

app.UseForwardedHeaders();

// Configure the HTTP request pipeline.
app.UseSwagger(c =>
{
	c.RouteTemplate = "/{documentname}/swagger.json";
});

app.UseSwaggerUI(opts =>
{
	opts.SwaggerEndpoint("/v1/swagger.json", $"A Centralized Mirror API: v1 (build {Assembly.GetExecutingAssembly().GetName().Version?.ToString(3) ?? "unknown"})");
	opts.SupportedSubmitMethods(Array.Empty<SubmitMethod>()); // Disable the 'Try it out' button. All endpoints require API keys so it's useless anyway.
	opts.RoutePrefix = "docs";
	opts.DocumentTitle = "A Centralized Mirror API Documentation";
	opts.EnableDeepLinking();
	opts.InjectStylesheet("/css/swagger.css");
	opts.IndexStream = () => Assembly.GetExecutingAssembly().GetManifestResourceStream("WebApi.Resources.DocsTemplate.html");
});

app.UseStaticFiles();

var hostingOptions = app.Services.GetRequiredService<IOptions<HostingOptions>>();

if (hostingOptions.Value.EnableHttpsRedirect)
	app.UseHttpsRedirection();

app.UseJsonExceptionHandler();

app.MapControllers();

// These MUST be the last thing added, and authentication MUST come first.
// If these are higher up in the file, the Swagger UI will be behind authorization - which we do not want.
app.UseAuthentication();
app.UseAuthorization();

await app.RunAsync();

return;

static void ConfigureOptions(IServiceCollection services, IConfiguration configuration)
{
	services.Configure<RedditSettings>(configuration.GetSection("RedditSettings"));
	services.Configure<HostingOptions>(configuration.GetSection("Hosting"));

	services.Configure<ForwardedHeadersOptions>(options =>
	{
		options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
	});
}

static void ConfigureServices(IServiceCollection services, IConfiguration configuration)
{
	services.AddAuthorization(opts =>
	{
		var policy = new AuthorizationPolicyBuilder()
			.RequireClaim("UserId")
			//.RequireAuthenticatedUser()
			.Build();
		opts.FallbackPolicy = policy;
	});

	var hostingOptions = configuration.GetSection("Hosting").Get<HostingOptions>();

	if (!string.IsNullOrEmpty(hostingOptions?.DataProtectionKeysPath))
	{
		services.AddDataProtection()
			.PersistKeysToFileSystem(new DirectoryInfo(hostingOptions.DataProtectionKeysPath))
			.UseCryptographicAlgorithms(new AuthenticatedEncryptorConfiguration
			{
				EncryptionAlgorithm = EncryptionAlgorithm.AES_256_CBC,
				ValidationAlgorithm = ValidationAlgorithm.HMACSHA512,
			});
	}

	services.AddMemoryCache();

	services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();

	services.AddSnooBrowserClient<RedditAuthParameterProvider, RedditAccessTokenProvider>();

	services.AddScoped<ApiKeyProvider>();
	services.AddScoped<LinkProvider>();
	services.AddScoped<UserProvider>();
	services.AddScoped<RedditCommentProvider>();

	services.AddScoped<UserCache>();

	services.AddSingleton<GlobalMemoryCache>();
	services.AddSingleton<TemplateCache>();
	services.AddHostedService<DbMaintenanceService>();
	services.AddHostedService<BackgroundServiceWorker>();
}

static void ConfigureDataAccess(IServiceCollection services, IConfiguration configuration)
{
	services.AddSingleton<IDbConnectionFactory, DbConnectionFactory>();
}
