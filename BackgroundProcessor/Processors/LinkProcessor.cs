using System.Data;
using ApplicationData;
using ApplicationData.Services;
using BackgroundProcessor.Templates;
using DataClasses;
using FruityFoundation.Base.Structures;
using FruityFoundation.DataAccess.Abstractions;
using SnooBrowser.Browsers;
using SnooBrowser.Models.Comment;
using SnooBrowser.Models.Submission;
using SnooBrowser.Things;

namespace BackgroundProcessor.Processors;

// ReSharper disable once UnusedType.Global
public class LinkProcessor : IBackgroundProcessor
{
	private readonly ILogger<LinkProcessor> _logger;
	private readonly LinkProvider _linkProvider;
	private readonly RedditCommentProvider _commentProvider;
	private readonly CommentBrowser _commentBrowser;
	private readonly UserProvider _userProvider;
	private readonly TemplateCache _templateCache;
	private readonly IDbConnectionFactory _dbConnectionFactory;
	private readonly ResourceAccessManager _resourceAccessManager;
	private readonly SubmissionBrowser _submissionBrowser;
	private readonly SubredditBrowser _subredditBrowser;

	public LinkProcessor(ILogger<LinkProcessor> logger,
		LinkProvider linkProvider,
		RedditCommentProvider commentProvider,
		CommentBrowser commentBrowser,
		UserProvider userProvider,
		TemplateCache templateCache,
		IDbConnectionFactory dbConnectionFactory,
		ResourceAccessManager resourceAccessManager,
		SubmissionBrowser submissionBrowser,
		SubredditBrowser subredditBrowser
	)
	{
		_logger = logger;
		_linkProvider = linkProvider;
		_commentProvider = commentProvider;
		_commentBrowser = commentBrowser;
		_userProvider = userProvider;
		_templateCache = templateCache;
		_dbConnectionFactory = dbConnectionFactory;
		_resourceAccessManager = resourceAccessManager;
		_submissionBrowser = submissionBrowser;
		_subredditBrowser = subredditBrowser;
	}

	/// <inheritdoc />
	public async Task Process(CancellationToken cancellationToken)
	{
		var postIdsWithPendingChanges = await _linkProvider.GetRedditPostIdsWithPendingChanges(cancellationToken)
			.Take(5)
			.ToArrayAsync(cancellationToken);

		foreach (var item in postIdsWithPendingChanges)
		{
			await WithLogging(
				item.RedditPostId,
				item.QueuedItemId,
				async ct =>
				{
					using var redditPostIdLock = await _resourceAccessManager.ObtainExclusiveAccess(item.RedditPostId, cancellationToken);
					await ProcessLink(item.RedditPostId, item.QueuedItemId, ct);
				},
				cancellationToken);
		}
	}

	private async Task WithLogging(string redditPostId, int queuedItemId, Func<CancellationToken, Task> action, CancellationToken cancellationToken)
	{
		using var _ = _logger.BeginScope(new Dictionary<string, object>
		{
			["RedditPostId"] = redditPostId,
			["QueuedItemId"] = queuedItemId,
		});

		try
		{
			await action(cancellationToken);
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "An error occurred while processing link");
		}
	}

	private async Task ProcessLink(string redditPostId, int queuedItemId, CancellationToken cancellationToken)
	{
		var redditPostLinkId = LinkThing.CreateFromShortId(redditPostId);

		var maybeExistingSubmission = await _submissionBrowser.GetSubmission(redditPostLinkId);
		var links = await _linkProvider.GetLinksByRedditPostId(redditPostLinkId.ShortId);
		var maybeExistingComment = await _commentProvider.FindCommentIdByPostId(redditPostLinkId.ShortId);

		await using var connection = _dbConnectionFactory.CreateConnection();
		await using var lazyTx = LazyDbTransaction.CreateFrom(connection, IsolationLevel.Serializable);

		if (!maybeExistingSubmission.Try(out var submission))
		{
			_logger.LogDebug("Submission {RedditPostId} not found, skipping processing", redditPostLinkId);
		}
		else if (links.Count == 0 && maybeExistingComment.HasValue)
		{
			await _commentBrowser.DeleteComment(CommentThing.CreateFromShortId(maybeExistingComment.Value));
		}
		else
		{
			var subreddit = await _subredditBrowser.GetAboutSubreddit(submission.Subreddit);

			await CreateOrEditComment(
				lazyTx,
				submission,
				redditPostLinkId,
				links,
				maybeExistingComment,
				isUserModerator: subreddit.Data.IsUserModerator,
				cancellationToken);
		}

#pragma warning disable IDISP001
		var tx = await lazyTx.GetOrCreateTx(cancellationToken);
#pragma warning restore IDISP001

		await LinkProvider.MarkRedditPostIdAsProcessed(tx, queuedItemId);

		await tx.Commit(cancellationToken);
	}

	private bool IsCommentAbleToBePosted(Submission submission, CancellationToken cancellationToken)
	{
		if (submission.IsLocked)
		{
			_logger.LogDebug("Submission {RedditPostId} is locked, skipping processing", submission.RedditPostId);
			return false;
		}
		else if (submission.IsArchived)
		{
			_logger.LogDebug("Submission {RedditPostId} is archived, skipping processing", submission.RedditPostId);
			return false;
		}

		return true;
	}

	private async Task CreateOrEditComment(
		LazyDbTransaction lazyTx,
		Submission submission,
		LinkThing redditPostId,
		IReadOnlyCollection<Link> links,
		Maybe<string> maybeExistingComment,
		bool isUserModerator,
		CancellationToken cancellationToken
	)
	{
		var message = await BuildComment(links,
			getUsername: async userId => (await _userProvider.FindUserByIdIncludeDeleted(userId)).Value.DisplayUsername,
			cancellationToken);

		if (maybeExistingComment.HasValue)
		{
			var result = await _commentBrowser.EditComment(CommentThing.CreateFromShortId(maybeExistingComment.Value), message);

			if (isUserModerator)
				await TryDistinguishStickyAndLockLogFailure(result.ParentId, result.CommentId);
		}
		else if (IsCommentAbleToBePosted(submission, cancellationToken))
		{
			var result = await _commentBrowser.SubmitComment(redditPostId, message);

			if (isUserModerator)
				await TryDistinguishStickyAndLockLogFailure(result.ParentId, result.CommentId);

#pragma warning disable IDISP001
			var innerTx = await lazyTx.GetOrCreateTx(cancellationToken);
#pragma warning restore IDISP001
			await RedditCommentProvider.CreateOrUpdateLinkedComment(innerTx, redditPostId.ShortId, result.CommentId.ShortId);
		}
	}

	private async Task<string> BuildComment(IReadOnlyCollection<Link> links, Func<int, Task<string>> getUsername, CancellationToken cancellationToken)
	{
		var mirrors = links
			.Where(x => x.LinkType.IsMirror)
			.OrderBy(x => x.CreatedAt)
			.ToArray();

		var downloads = links
			.Where(x => x.LinkType.IsDownload)
			.OrderBy(x => x.CreatedAt)
			.ToArray();

		var mirrorsSection = await BuildSection(heading: "Mirrors", prefix: "Mirror", mirrors, getUsername).ToArrayAsync(cancellationToken);
		var downloadsSection = await BuildSection(heading: "Downloads", prefix: "Download", downloads, getUsername).ToArrayAsync(cancellationToken);
		var sections = mirrorsSection
			.ConditionalConcat(mirrorsSection.Any() && downloadsSection.Any(), new[] { Environment.NewLine })
			.Concat(downloadsSection)
			.ToArray();
		// TODO: torrents section

		var commentReplyTemplate = await _templateCache.GetCommentReplyTemplate(cancellationToken);

		return string.Format(commentReplyTemplate, string.Join(Environment.NewLine, sections));

		static async IAsyncEnumerable<string> BuildSection(string heading, string prefix, IReadOnlyList<Link> links, Func<int, Task<string>> getUsername)
		{
			if (!links.Any())
				yield break;

			yield return $@"**{heading}**";
			yield return Environment.NewLine;

			for (var i = 0; i < links.Count; i++)
			{
				var link = links[i];
				yield return $"* [{prefix} #{i + 1}]({link.LinkUrl}) (provided by /u/{await getUsername(link.OwnerId)})";
			}
		}
	}

	private async Task TryDistinguishStickyAndLockLogFailure(
		OneOf<LinkThing, CommentThing> parent,
		CommentThing comment
	)
	{
		try
		{
			await _commentBrowser.DistinguishComment(comment, DistinguishType.Moderator, isSticky: true);
			await _commentBrowser.LockComment(comment);
		}
		catch (Exception ex)
		{
			var (parentType, parentId) = parent.Map(
				static link => ("link", link.FullId),
				static comment => ("comment", comment.FullId));

			_logger.LogError(ex,
				"An error occurred while trying to distinguish and sticky comment (ParentType={ParentType}, ParentId={ParentId}, CommentId={CommentId})",
				parentType, parentId, comment.FullId);
		}
	}
}
