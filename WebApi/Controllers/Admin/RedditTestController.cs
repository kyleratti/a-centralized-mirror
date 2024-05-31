using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SnooBrowser.Browsers;
using WebApi.Util;

#pragma warning disable CS1591
namespace WebApi.Controllers.Admin;

[Authorize(Roles = UserRoles.AdministratorRole)]
[ApiExplorerSettings(IgnoreApi = true)]
[ApiController]
[Route("v1/admin/reddit-test")]
public class RedditTestController : Controller
{
	private readonly MeBrowser _meBrowser;

	/// <inheritdoc />
	public RedditTestController(
		MeBrowser meBrowser
	)
	{
		_meBrowser = meBrowser;
	}

	[HttpGet]
	[Route("get-reddit-user")]
	public async Task<object> GetRedditUser()
	{
		var result = await _meBrowser.GetMe();

		return new
		{
			username = result.Username,
			isMod = result.IsMod
		};
	}
}
