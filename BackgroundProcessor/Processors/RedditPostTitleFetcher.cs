using ApplicationData.Services;
using SnooBrowser.Browsers;
using SnooBrowser.Things;
using FruityFoundation.Base.Structures;

namespace BackgroundProcessor.Processors;

public class RedditPostTitleFetcher : IBackgroundProcessor
{
	private readonly ILogger<RedditPostTitleFetcher> _logger;
	private readonly RedditPostProvider _redditPostProvider;
	private readonly SubmissionBrowser _submissionBrowser;

	public RedditPostTitleFetcher(
		ILogger<RedditPostTitleFetcher> logger,
		RedditPostProvider redditPostProvider,
		SubmissionBrowser submissionBrowser
	)
	{
		_logger = logger;
		_redditPostProvider = redditPostProvider;
		_submissionBrowser = submissionBrowser;
	}

	/// <inheritdoc />
	public async Task Process(CancellationToken cancellationToken)
	{
		var query = _redditPostProvider.GetPostIdsWithoutTitleFetched(cancellationToken)
			.Take(10)
			.WithCancellation(cancellationToken);

		await foreach (var redditPostId in query)
		{
			try
			{
				var submissionResult = await _submissionBrowser.GetSubmission(LinkThing.CreateFromShortId(redditPostId)));

				if (!submissionResult.Try(out var submission))
				{
					await _redditPostProvider.SetTitleFetchedForPostId(redditPostId, cancellationToken);
					_logger.LogInformation("Submission title couldn't be fetched for {RedditPostId} because the submission has been removed", redditPostId);
					return;
				}

				await _redditPostProvider.SetPostTitle(redditPostId, submission.Title, cancellationToken);
			}
			catch (Exception ex)
			{
				// We'll figure out how to clean these up later
				_logger.LogError(ex, "Error fetching title for {RedditPostId}", redditPostId);
				await _redditPostProvider.SetTitleFetchedForPostId(redditPostId, cancellationToken);
			}
		}
	}
}
