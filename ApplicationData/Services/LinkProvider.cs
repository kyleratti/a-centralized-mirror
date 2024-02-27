﻿using System.Data;
using System.Runtime.CompilerServices;
using Dapper;
using Dapper.Transaction;
using DataClasses;
using FruityFoundation.Base.Extensions;
using FruityFoundation.Base.Structures;
using ResultMonad;

namespace ApplicationData.Services;

public class LinkProvider
{
	private readonly IDbConnection _db;

	public LinkProvider(
		IDbConnection dbConnection
	)
	{
		_db = dbConnection;
	}

	public async Task<Maybe<Link>> FindLink(int userId, string redditPostId, string linkUrl, LinkKind linkKind)
	{
		using var reader = await _db.ExecuteReaderAsync(
			"""
			SELECT l.link_id, l.reddit_post_id, l.link_url, l.link_type, l.created_at, l.owner
							FROM links l
							WHERE
								l.reddit_post_id = @redditPostId
								AND l.link_url = @linkUrl
								AND l.link_type = @linkType
								AND l.owner = @userId
								AND l.is_deleted = false
			""",
			new
			{
				redditPostId,
				linkUrl,
				linkType = linkKind.RawValue,
				userId,
			});

		if (!reader.Read())
			return Maybe<Link>.Empty();

		return new Link(
			linkId: reader.GetInt32(0),
			redditPostId: reader.GetString(1),
			linkUrl: reader.GetString(2),
			linkType: ParseRawLinkType(reader.GetInt16(3)),
			createdAt: reader.GetDateTime(4),
			ownerId: reader.GetInt32(5)
		);
	}

	public async Task<Maybe<Link>> FindLinkById(Maybe<int> userId, int linkId)
	{
		using var reader = await _db.ExecuteReaderAsync(
			$@"SELECT l.link_id, l.reddit_post_id, l.link_url, l.link_type, l.created_at, l.owner
				FROM links l
				WHERE
					l.link_id = @linkId
					{(userId.HasValue ? "AND l.owner = @userId" : "")}
					AND l.is_deleted = false",
			new
			{
				linkId,
				userId = userId.OrValue(-1),
			});

		if (!reader.Read())
			return Maybe<Link>.Empty();

		return new Link(
			linkId: reader.GetInt32(0),
			redditPostId: reader.GetString(1),
			linkUrl: reader.GetString(2),
			linkType: ParseRawLinkType(reader.GetInt16(3)),
			createdAt: reader.GetDateTime(4),
			ownerId: reader.GetInt32(5)
		);
	}

	public async Task<IReadOnlyCollection<Link>> FindAllLinksByPostId(string redditPostId)
	{
		 var linkQueryResult = await _db.QueryAsync<LinkQueryResult>(
			"""
				SELECT
					l.link_id AS LinkId
					,l.reddit_post_id AS RedditPostId
					,l.link_url AS LinkUrl
					,l.link_type AS LinkType
					,l.created_at AS CreatedAt
					,l.owner AS OwnerId
				FROM links l
				WHERE
					l.reddit_post_id = @redditPostId
					AND l.is_deleted = false
				""",
			new { redditPostId });

		 return linkQueryResult
			 .Select(result => new Link(
				 linkId: result.LinkId,
				 redditPostId: result.RedditPostId,
				 linkUrl: result.LinkUrl,
				 linkType: ParseRawLinkType(result.LinkType),
				 createdAt: result.CreatedAt,
				 ownerId: result.OwnerId
			 ))
			 .ToArray();
	}

	public async Task DeleteLinkById(int linkId)
	{
		using var tx = _db.CreateTransaction(IsolationLevel.Serializable);
		var redditPostId = await tx.ExecuteScalarAsync<string>(
			@"
				UPDATE links SET is_deleted = true WHERE link_id = @linkId AND is_deleted = false
				RETURNING reddit_post_id",
			new { linkId });

		await QueueRedditPostIdForUpdate(tx, redditPostId);

		tx.Commit();
	}

	public abstract record CreateLinkError
	{
		public T Switch<T>(Func<T> linkAlreadyExists) => this switch
		{
			LinkAlreadyExists => linkAlreadyExists(),
			_ => throw new NotImplementedException($"Type not implemented: {GetType().FullName}")
		};
	}

	public record LinkAlreadyExists : CreateLinkError;

	public async Task<Result<int, LinkProvider.CreateLinkError>> CreateLink(NewLink link)
	{
		using var tx = _db.CreateTransaction(IsolationLevel.Serializable);

		var existingLink = await tx.ExecuteScalarAsync<bool?>(
			"""
				SELECT 1 FROM links
				WHERE
					reddit_post_id = @redditPostId
					AND link_url = @linkUrl
					AND link_type = @linkType
					AND owner = @userId
					AND is_deleted = false
			""",
			new
			{
				redditPostId = link.RedditPostId,
				linkUrl = link.LinkUrl,
				linkType = link.LinkType.RawValue,
				userId = link.OwnerUserId,
			});

		if (existingLink is true)
			return Result.Fail<int, CreateLinkError>(new LinkAlreadyExists());

		var linkIdResult = await tx.ExecuteScalarAsync<int?>("""
		INSERT INTO links (reddit_post_id, link_url, link_type, created_at, is_deleted, owner)
		VALUES (@redditPostId, @linkUrl, @linkType, CURRENT_TIMESTAMP, false, @userId)
		RETURNING link_id
		""",
			new
			{
				redditPostId = link.RedditPostId,
				linkUrl = link.LinkUrl,
				linkType = link.LinkType.RawValue,
				userId = link.OwnerUserId,
			});

		if (!linkIdResult.ToMaybe().Try(out var linkId))
			throw new ApplicationException("Failed to create link");

		await QueueRedditPostIdForUpdate(tx, link.RedditPostId);

		tx.Commit();

		return Result.Ok<int, CreateLinkError>(linkId);
	}

	public async Task<IReadOnlyCollection<Link>> GetAllLinksByUserId(int userId)
	{
		var queryResult = await _db.QueryAsync<LinkQueryResult>(
			"""
				SELECT
					l.link_id AS LinkId
					,l.reddit_post_id AS RedditPostId
					,l.link_url AS LinkUrl
					,l.link_type AS LinkType
					,l.created_at AS CreatedAt
					,l.owner AS OwnerId
				FROM links l
				WHERE
					l.owner = @userId
					AND l.is_deleted = false
			""", new { userId });

		return queryResult
			.Select(result => new Link(
				linkId: result.LinkId,
				redditPostId: result.RedditPostId,
				linkUrl: result.LinkUrl,
				linkType: ParseRawLinkType(result.LinkType),
				createdAt: result.CreatedAt,
				ownerId: result.OwnerId))
			.ToArray();
	}

	public async IAsyncEnumerable<(int QueuedItemId, DateTime QueuedAt, string RedditPostId)> GetRedditPostIdsWithPendingChanges(
		[EnumeratorCancellation] CancellationToken cancellationToken
	)
	{
		var reader = await _db.QueryAsync<(int QueuedItemId, DateTime QueuedAt, string RedditPostId)>(
			@"SELECT queued_item_id, queued_at, reddit_post_id
				FROM link_queue
				WHERE processed_at IS NULL
				ORDER BY queued_at ASC");

		foreach (var (queuedItemId, queuedAt, redditPostId) in reader)
			yield return (QueuedItemId: queuedItemId, QueuedAt: queuedAt, RedditPostId: redditPostId);
	}

	public static async Task MarkRedditPostIdAsProcessed(IDbTransaction tx, int itemId) =>
		await tx.ExecuteAsync(
			@"UPDATE link_queue SET processed_at = CURRENT_TIMESTAMP WHERE queued_item_id = @itemId",
			new { itemId });

	public async Task<IReadOnlyCollection<Link>> GetLinksByRedditPostId(string redditPostId)
	{
		var queryResult = await _db.QueryAsync<LinkQueryResult>(
			"""
				SELECT
					l.link_id AS LinkId
					,l.reddit_post_id AS RedditPostId
					,l.link_url AS LinkUrl
					,l.link_type AS LinkType
					,l.created_at AS CreatedAt
					,l.owner AS OwnerId
				FROM links l
				WHERE
					l.reddit_post_id = @redditPostId
					AND l.is_deleted = false
			""", new { redditPostId });

		return queryResult
			.Select(result => new Link(
				linkId: result.LinkId,
				redditPostId: result.RedditPostId,
				linkUrl: result.LinkUrl,
				linkType: ParseRawLinkType(result.LinkType),
				createdAt: result.CreatedAt,
				ownerId: result.OwnerId))
			.ToArray();
	}

	private static LinkKind ParseRawLinkType(short rawLinkType) => rawLinkType switch
	{
		1 => LinkKind.Mirror,
		2 => LinkKind.Download,
		_ => throw new ArgumentOutOfRangeException(nameof(rawLinkType), rawLinkType, "Unknown link type")
	};

	// TODO: this should be a much better system
	private static async Task QueueRedditPostIdForUpdate(IDbTransaction tx, string redditPostId)
	{
		// There is certainly a better way to do this. Oh well!
		var hasPendingUpdate = await tx.ExecuteScalarAsync<int?>(
			@"SELECT 1 FROM link_queue WHERE reddit_post_id = @redditPostId AND processed_at IS NULL LIMIT 1",
			new { redditPostId }) == 1;

		if (!hasPendingUpdate)
		{
			await tx.ExecuteAsync(
				@"INSERT INTO link_queue (reddit_post_id, queued_at) VALUES (@redditPostId, CURRENT_TIMESTAMP)",
				new { redditPostId });
		}
	}

	private class LinkQueryResult
	{
		public required int LinkId { get; init; }
		public required string RedditPostId { get; init; }
		public required string LinkUrl { get; init; }
		public required short LinkType { get; init; }
		public required DateTime CreatedAt { get; init; }
		public required int OwnerId { get; init; }
	}
}
