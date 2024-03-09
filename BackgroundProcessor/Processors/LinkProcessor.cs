using System.Data;
using ApplicationData;
using ApplicationData.Services;
using BackgroundProcessor.Templates;
using DataClasses;
using FruityFoundation.Base.Structures;
using SnooBrowser.Browsers;
using SnooBrowser.Models.Comment;
using SnooBrowser.Things;

namespace BackgroundProcessor.Processors;

// ReSharper disable once UnusedType.Global
// it is auto-created
public class LinkProcessor : IBackgroundProcessor
{
	private readonly ILogger<LinkProcessor> _logger;
	private readonly LinkProvider _linkProvider;
	private readonly RedditCommentProvider _commentProvider;
	private readonly CommentBrowser _commentBrowser;
	private readonly UserProvider _userProvider;
	private readonly TemplateCache _templateCache;
	private readonly IDbConnectionFactory _dbConnectionFactory;

	public LinkProcessor(
		ILogger<LinkProcessor> logger,
		LinkProvider linkProvider,
		RedditCommentProvider commentProvider,
		CommentBrowser commentBrowser,
		UserProvider userProvider,
		TemplateCache templateCache,
		IDbConnectionFactory dbConnectionFactory
	)
	{
		_logger = logger;
		_linkProvider = linkProvider;
		_commentProvider = commentProvider;
		_commentBrowser = commentBrowser;
		_userProvider = userProvider;
		_templateCache = templateCache;
		_dbConnectionFactory = dbConnectionFactory;
	}

	/// <inheritdoc />
	public async Task Process(CancellationToken cancellationToken)
	{
		var postIdsWithPendingChanges = await _linkProvider.GetRedditPostIdsWithPendingChanges(cancellationToken)
			.Take(5)
			.ToArrayAsync(cancellationToken);

		foreach (var item in postIdsWithPendingChanges)
		{
			var links = await _linkProvider.GetLinksByRedditPostId(item.RedditPostId);
			var maybeExistingComment = await _commentProvider.FindCommentIdByPostId(item.RedditPostId);

			using var connection = await _dbConnectionFactory.CreateConnection();
			using var lazyTx = LazyDbTransaction.Create(connection, IsolationLevel.Serializable);

			if (!links.Any())
			{
				if (maybeExistingComment.HasValue)
					await _commentBrowser.DeleteComment(CommentThing.CreateFromShortId(maybeExistingComment.Value));
			}
			else
			{
				var message = await BuildComment(links,
					getUsername: async userId => (await _userProvider.FindUserByIdIncludeDeleted(userId)).Value.DisplayUsername,
					cancellationToken);

				if (maybeExistingComment.HasValue)
				{
					var result = await _commentBrowser.EditComment(CommentThing.CreateFromShortId(maybeExistingComment.Value), message);
					await TryDistinguishStickyAndLockLogFailure(result.CommentId);
				}
				else
				{
					var result = await _commentBrowser.SubmitComment(LinkThing.CreateFromShortId(item.RedditPostId), message);
					await TryDistinguishStickyAndLockLogFailure(result.CommentId);

					await RedditCommentProvider.CreateOrUpdateLinkedComment(lazyTx.Tx, item.RedditPostId, result.CommentId.ShortId);
				}
			}

			await LinkProvider.MarkRedditPostIdAsProcessed(lazyTx.Tx, item.QueuedItemId);

			lazyTx.Tx.Commit();
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

	private async Task TryDistinguishStickyAndLockLogFailure(CommentThing comment)
	{
		try
		{
			await _commentBrowser.DistinguishComment(comment, DistinguishType.Moderator, isSticky: true);
			await _commentBrowser.LockComment(comment);
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "An error occurred while trying to distinguish and sticky comment: {CommentId}", comment.FullId);
		}
	}
}
