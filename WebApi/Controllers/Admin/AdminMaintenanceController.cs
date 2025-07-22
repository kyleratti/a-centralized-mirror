using FruityFoundation.DataAccess.Abstractions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebApi.Util;

namespace WebApi.Controllers.Admin;

/// <summary>
/// Admin Maintenance Controller
/// </summary>
[Authorize(Roles = UserRoles.AdministratorRole)]
[ApiExplorerSettings(IgnoreApi = true)]
[ApiController]
[Route("v1/admin/maintenance")]
public class AdminMaintenanceController : Controller
{
	private readonly IDbConnectionFactory _dbConnectionFactory;

	/// <summary>
	/// C'tor
	/// </summary>
	/// <param name="dbConnectionFactory"></param>
	public AdminMaintenanceController(IDbConnectionFactory dbConnectionFactory)
	{
		_dbConnectionFactory = dbConnectionFactory;
	}

	/// <summary>
	/// Remove all comments, queued items, and links related to the specified reddit post id.
	/// This is a hard purge; if a comment has already been posted to reddit, it will not be cleaned up.
	/// </summary>
	[HttpDelete]
	[Route("reddit-post/{redditPostId}/clear")]
	public async Task<IActionResult> ClearRedditPost(string redditPostId)
	{
		await using var db = _dbConnectionFactory.CreateConnection();

		await db.Execute(
			"""
			BEGIN TRANSACTION;
			DELETE FROM reddit_comments WHERE reddit_post_id = @redditPostId;
			DELETE FROM link_queue WHERE reddit_post_id = @redditPostId;
			DELETE FROM links WHERE reddit_post_id = @redditPostId;
			COMMIT;
			""", new { redditPostId }, CancellationToken.None);

		return Ok();
	}
}
