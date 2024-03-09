using ApplicationData.Services;
using Microsoft.AspNetCore.Mvc;

namespace WebApi.Controllers.Admin;

/// <summary>
/// Admin Queue controller
/// </summary>
[Route("v1/admin/queue")]
public class AdminQueueController : AdminApiController
{
	private readonly LinkProvider _linkProvider;

	/// <inheritdoc />
	public AdminQueueController(
		IHttpContextAccessor httpContextAccessor,
		LinkProvider linkProvider
	) : base(httpContextAccessor)
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
