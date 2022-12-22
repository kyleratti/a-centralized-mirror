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

public class ApiKeyAuthSchemeOptions : AuthenticationSchemeOptions
{ }

public class ApiKeyAuthHandler : AuthenticationHandler<ApiKeyAuthSchemeOptions>
{
	private readonly ApiKeyProvider _apiKeyProvider;
	private readonly UserCache _userCache;
	private readonly Regex _apiKeyMatch = new(@"^Key (?<key>.*)$", RegexOptions.Compiled);

	/// <inheritdoc />
	public ApiKeyAuthHandler(
		IOptionsMonitor<ApiKeyAuthSchemeOptions> options,
		ILoggerFactory logger,
		UrlEncoder encoder,
		ISystemClock clock,
		ApiKeyProvider apiKeyProvider,
		UserCache userCache
	) : base(options, logger, encoder, clock)
	{
		_apiKeyProvider = apiKeyProvider;
		_userCache = userCache;
	}

	protected override async Task<AuthenticateResult> HandleAuthenticateAsync() =>
		await JsonExceptionMiddleware.InvokeWithExceptionHandler(Request.HttpContext, async () =>
		{
			// For endpoints with IAllowAnonymous, skip the authentication check.
			if (Context.GetEndpoint()?.Metadata.GetMetadata<IAllowAnonymous>() != null)
				return AuthenticateResult.NoResult();

			if (!Request.Headers.TryGetValue("Authorization", out var authorization))
				return AuthenticateResult.Fail("No authorization key provided");

			var result = _apiKeyMatch.Match(authorization);

			if (!result.Success)
				return AuthenticateResult.Fail("Invalid authorization format provided");

			if (!(await _apiKeyProvider.FindUserByApiKey(result.Groups["key"].Value)).Try(out var user))
				return AuthenticateResult.Fail("Unable to authenticate with provided key");

			var identity = new GenericIdentity("WebApi");
			identity.AddClaim(new Claim("UserId", user.UserId.ToString()));

			var roles = new List<string>();

			if (user.IsAdministrator)
				roles.Add(UserRoles.AdministratorRole);

			var principal = new GenericPrincipal(identity, roles.ToArray());
			_userCache.AddOrUpdateUser(user);

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
				message = "Unauthorized"
			});
		}
	}
}