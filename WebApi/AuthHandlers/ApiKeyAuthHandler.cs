using System.Net;
using System.Security.Claims;
using System.Security.Principal;
using System.Text.Encodings.Web;
using System.Text.RegularExpressions;
using ApplicationData.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;
using WebApi.Middleware;
using WebApi.Util;

// Disable "Missing XML comment for publicly visible type or member..."
#pragma warning disable CS1591

namespace WebApi.AuthHandlers;

public class ApiKeyAuthSchemeOptions : AuthenticationSchemeOptions;

public partial class ApiKeyAuthHandler : AuthenticationHandler<ApiKeyAuthSchemeOptions>
{
	private readonly ILogger<ApiKeyAuthHandler> _logger;
	private readonly ApiKeyProvider _apiKeyProvider;
	private readonly Regex _apiKeyMatch = ApiKeyRegex();

	/// <inheritdoc />
	public ApiKeyAuthHandler(
		ILogger<ApiKeyAuthHandler> logger,
		IOptionsMonitor<ApiKeyAuthSchemeOptions> options,
		ILoggerFactory loggerFactory,
		UrlEncoder encoder,
		ApiKeyProvider apiKeyProvider
	) : base(options, loggerFactory, encoder)
	{
		_logger = logger;
		_apiKeyProvider = apiKeyProvider;
	}

	protected override async Task<AuthenticateResult> HandleAuthenticateAsync() =>
		await JsonExceptionMiddleware.InvokeWithExceptionHandler(_logger, Request.HttpContext, async () =>
		{
			// For endpoints with IAllowAnonymous, skip the authentication check.
			if (Context.GetEndpoint()?.Metadata.GetMetadata<IAllowAnonymous>() != null)
				return AuthenticateResult.NoResult();

			if (!Request.Headers.TryGetValue("Authorization", out var authorization))
				return AuthenticateResult.Fail("No authorization key provided");

			var authorizationString = authorization.ToString();

			if (string.IsNullOrEmpty(authorizationString))
				return AuthenticateResult.Fail("No authorization key provided");

			var result = _apiKeyMatch.Match(authorizationString);

			if (!result.Success)
				return AuthenticateResult.Fail("Invalid authorization format provided");

			if (!(await _apiKeyProvider.FindUserByApiKey(result.Groups["key"].Value)).Try(out var user))
				return AuthenticateResult.Fail("Unable to authenticate with provided key");

			var identity = new GenericIdentity("WebApi");
			identity.AddClaim(new Claim(ClaimTypes.NameIdentifier, user.UserId.ToString()));

			var roles = user switch
			{
				{ IsAdministrator: true } => new[] { UserRoles.AdministratorRole },
				_ => [],
			};

			var principal = new GenericPrincipal(identity, roles: roles);
			var ticket = new AuthenticationTicket(principal, Scheme.Name);

			return AuthenticateResult.Success(ticket);
		});

	/// <inheritdoc />
	protected override async Task HandleChallengeAsync(AuthenticationProperties properties)
	{
		if (!Request.HttpContext.User.Identity?.IsAuthenticated ?? false)
		{
			Response.StatusCode = (int)HttpStatusCode.Unauthorized;
			Response.ContentType = "application/json";
			await Response.WriteAsJsonAsync(new
			{
				message = "Unauthorized",
			});
		}
	}

	[GeneratedRegex(@"^Key (?<key>.*)$", RegexOptions.Compiled)]
	private static partial Regex ApiKeyRegex();
}
