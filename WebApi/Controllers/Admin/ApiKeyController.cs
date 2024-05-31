using System.Security.Cryptography;
using ApplicationData.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebApi.Models.Admin;
using WebApi.Util;

#pragma warning disable CS1591

namespace WebApi.Controllers.Admin;

[Authorize(Roles = UserRoles.AdministratorRole)]
[ApiExplorerSettings(IgnoreApi = true)]
[ApiController]
[Route("v1/admin/apikey")]
public class ApiKeyController : Controller
{
	private readonly ApiKeyProvider _apiKeyProvider;
	private readonly UserProvider _userProvider;

	/// <inheritdoc />
	public ApiKeyController(
		ApiKeyProvider apiKeyProvider,
		UserProvider userProvider
	)
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
