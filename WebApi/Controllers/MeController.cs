using ApplicationData.Services;
using Microsoft.AspNetCore.Mvc;
using WebApi.Models;

namespace WebApi.Controllers;

/// <summary>
/// Information about the logged in user
/// </summary>
[ApiController]
[Route("v1/[controller]")]
public class MeController(
	UserProvider _userProvider,
	IHttpContextAccessor httpContextAccessor
) : ApiController(httpContextAccessor)
{
	/// <summary>
	/// Retrieve information about the current API user.
	/// </summary>
	/// <returns></returns>
	[HttpGet]
	[Route("")]
	public async Task<AboutMe> GetAboutMe()
	{
		if (!(await _userProvider.FindUserByIdIncludeDeleted(UserId)).Try(out var user))
			throw new InvalidOperationException($"Unable to find user {UserId}");

		return new AboutMe(
			DisplayName: user.DisplayUsername,
			DeveloperName: user.DeveloperUsername,
			CreatedAt: user.CreatedAt);
	}
}
