﻿using ApplicationData.Services;
using BackgroundProcessor.Templates;
using Core.DbConnection;
using DataClasses;
using FruityFoundation.Base.Extensions;
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
	private readonly UserCache _userCache;
	private readonly TemplateCache _templateCache;
	private readonly DbTransactionFactory _dbTxFactory;

	public LinkProcessor(
		ILogger<LinkProcessor> logger,
		LinkProvider linkProvider,
		RedditCommentProvider commentProvider,
		CommentBrowser commentBrowser,
		UserCache userCache,
		TemplateCache templateCache,
		DbTransactionFactory dbTxFactory)
	{
		_logger = logger;
		_linkProvider = linkProvider;
		_commentProvider = commentProvider;
		_commentBrowser = commentBrowser;
		_userCache = userCache;
		_templateCache = templateCache;
		_dbTxFactory = dbTxFactory;
	}

	/// <inheritdoc />
	public async Task Process(CancellationToken cancellationToken)
	{
		var postIdsWithPendingChanges = await _linkProvider.GetRedditPostIdsWithPendingChanges(cancellationToken)
			.Take(5)
			.ToArrayAsync(cancellationToken);

		foreach (var item in postIdsWithPendingChanges)
		{
			var links = await _linkProvider.GetLinksByRedditPostId(item.RedditPostId, cancellationToken).ToArrayAsync(cancellationToken);
			var maybeExistingComment = await _commentProvider.FindCommentIdByPostId(item.RedditPostId);
			var tx = await _dbTxFactory.CreateTransaction();

			if (!links.Any())
			{
				if (maybeExistingComment.HasValue)
					await _commentBrowser.DeleteComment(CommentThing.CreateFromShortId(maybeExistingComment.Value));
			}
			else
			{
				var message = await BuildComment(links,
					getUsername: async userId => (await _userCache.FindUser(userId)).Value.DisplayUsername);

				if (maybeExistingComment.HasValue)
				{
					var result = await _commentBrowser.EditComment(CommentThing.CreateFromShortId(maybeExistingComment.Value), message);
					await TryDistinguishAndStickyLogFailure(result.CommentId);
				}
				else
				{
					var result = await _commentBrowser.SubmitComment(LinkThing.CreateFromShortId(item.RedditPostId), message);
					await TryDistinguishAndStickyLogFailure(result.CommentId);

					await RedditCommentProvider.CreateOrUpdateLinkedComment(tx, item.RedditPostId, result.CommentId.ShortId);
				}
			}

			await LinkProvider.MarkRedditPostIdAsProcessed(tx, item.QueuedItemId);

			await tx.CommitAsync();
		}
	}

	private async Task<string> BuildComment(IReadOnlyCollection<Link> links, Func<int, Task<string>> getUsername)
	{
		var (mirrors, downloads) = links
			.OrderBy(x => x.CreatedAt)
			.Aggregate((Mirrors: new List<Link>(), Downloads: new List<Link>()),
			(state, item) =>
			{
				if (item.LinkType.IsMirror)
					state.Mirrors.Add(item);
				else if (item.LinkType.IsDownload)
					state.Downloads.Add(item);
				else
					throw new ArgumentOutOfRangeException(nameof(item.LinkType), item.LinkType.RawValue,
						$"Unhandled {item.LinkType.GetType().FullName}");

				return state;
			});

		var mirrorsSection = await BuildSection(heading: "Mirrors", prefix: "Mirror", mirrors, getUsername).ToArrayAsync();
		var downloadsSection = await BuildSection(heading: "Downloads", prefix: "Download", downloads, getUsername).ToArrayAsync();
		var sections = mirrorsSection
			.ConditionalConcat(mirrorsSection.Any() && downloadsSection.Any(), new[] { Environment.NewLine })
			.Concat(downloadsSection)
			.ToArray();
		// TODO: torrents section

		return string.Format(await _templateCache.GetCommentReply(), string.Join(Environment.NewLine, sections));

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

	private async Task TryDistinguishAndStickyLogFailure(CommentThing comment)
	{
		try
		{
			await _commentBrowser.DistinguishComment(comment, DistinguishType.Moderator, isSticky: true);
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "An error occurred while trying to distinguish and sticky comment: {CommentId}", comment.FullId);
		}
	}
}