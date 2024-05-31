using ApplicationData.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebApi.Util;

namespace WebApi.Controllers.Admin;

/// <summary>
/// Admin Queue controller
/// </summary>
[Authorize(Roles = UserRoles.AdministratorRole)]
[ApiExplorerSettings(IgnoreApi = true)]
[ApiController]
[Route("v1/admin/queue")]
public class AdminQueueController : Controller
{
	private readonly LinkProvider _linkProvider;

	/// <inheritdoc />
	public AdminQueueController(
		LinkProvider linkProvider
	)
	{
		_linkProvider = linkProvider;
	}

	/// <summary>
	/// Retrieve the processing queue
	/// </summary>
	[HttpGet]
	[Route("")]
	public async Task<IActionResult> GetProcessingQueue(CancellationToken cancellationToken)
	{
		var postIdsWithPendingChanges = await _linkProvider.GetRedditPostIdsWithPendingChanges(cancellationToken)
			.Select(x => new
			{
				queuedItemId = x.QueuedItemId,
				redditPostId = x.RedditPostId,
				queuedAt = x.QueuedAt,
			})
			.ToArrayAsync(cancellationToken);

		return new OkObjectResult(new
		{
			itemCount = postIdsWithPendingChanges.Length,
			queuedItems = postIdsWithPendingChanges,
		});
	}
}
