using ApplicationData.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebApi.Models;
using WebApi.Util;

namespace WebApi.Controllers;

/// <summary>
/// Information about the logged in user
/// </summary>
[ApiController]
[Route("v1/[controller]")]
[Authorize]
public class MeController : Controller
{
	private readonly UserProvider _userProvider;

	/// <summary>
	/// Information about the logged in user
	/// </summary>
	public MeController(UserProvider userProvider)
	{
		_userProvider = userProvider;
	}

	/// <summary>
	/// Retrieve information about the current API user.
	/// </summary>
	/// <returns></returns>
	[HttpGet]
	[Route("")]
	public async Task<AboutMe> GetAboutMe()
	{
		if (!User.GetUserId().Try(out var loggedInUserId))
			throw new UnauthorizedAccessException("User is not logged in");

		if (!(await _userProvider.FindUserByIdIncludeDeleted(loggedInUserId)).Try(out var user))
			throw new InvalidOperationException($"Unable to find user {loggedInUserId}");

		return new AboutMe(
			DisplayName: user.DisplayUsername,
			DeveloperName: user.DeveloperUsername,
			CreatedAt: user.CreatedAt);
	}
}
