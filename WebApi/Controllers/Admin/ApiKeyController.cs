using System.Security.Cryptography;
using ApplicationData.Services;
using Microsoft.AspNetCore.Mvc;
using WebApi.Models.Admin;
#pragma warning disable CS1591

namespace WebApi.Controllers.Admin;

[Route("v1/admin/apikey")]
public class ApiKeyController : AdminApiController
{
	private readonly ApiKeyProvider _apiKeyProvider;
	private readonly UserProvider _userProvider;

	/// <inheritdoc />
	public ApiKeyController(
		IHttpContextAccessor httpContextAccessor,
		ApiKeyProvider apiKeyProvider,
		UserProvider userProvider
	) : base(httpContextAccessor)
	{
		_apiKeyProvider = apiKeyProvider;
		_userProvider = userProvider;
	}

	[HttpPost]
	[Route("")]
	public async Task<IActionResult> CreateApiKeyAsync(SubmitApiKeyModel newApiKey)
	{
		if (!newApiKey.UserId.HasValue)
			return new BadRequestObjectResult(new
			{
				message = "The body was missing the following properties: userId"
			});

		if (!(await _userProvider.FindUserByIdIncludeDeleted(newApiKey.UserId!.Value)).Try(out var user))
			return new NotFoundObjectResult(new
			{
				message = $"A user with this ID was not found: {newApiKey.UserId.Value}"
			});

		var rawKey = RandomNumberGenerator.GetBytes(64);
		var key = BitConverter.ToString(rawKey).Replace("-", "");

		await _apiKeyProvider.CreateApiKeyForUser(user.UserId, key);

		return new OkObjectResult(new
		{
			apiKey = key
		});
	}
}
