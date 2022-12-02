using Microsoft.AspNetCore.Mvc;
using SnooBrowser.Browsers;

#pragma warning disable CS1591
namespace WebApi.Controllers.Admin;

[Route("v1/admin/reddit-test")]
public class RedditTestController : AdminApiController
{
	private readonly MeBrowser _meBrowser;

	/// <inheritdoc />
	public RedditTestController(IServiceProvider serviceProvider, MeBrowser meBrowser) : base(serviceProvider)
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