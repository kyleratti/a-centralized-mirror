using System.Data;
using System.Runtime.CompilerServices;
using DataClasses;
using FruityFoundation.Base.Structures;
using FruityFoundation.DataAccess.Abstractions;

namespace ApplicationData.Services;

public class LinkProvider
{
	private readonly IDbConnectionFactory _dbConnectionFactory;

	public LinkProvider(
		IDbConnectionFactory dbConnectionFactory
	)
	{
		_dbConnectionFactory = dbConnectionFactory;
	}

	public async Task<Maybe<Link>> FindLink(int userId, string redditPostId, string linkUrl, LinkKind linkKind)
	{
		await using var connection = _dbConnectionFactory.CreateReadOnlyConnection();
		using var reader = await connection.ExecuteReader(
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

		if (!await reader.ReadAsync())
			return Maybe.Empty<Link>();

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
		await using var connection = _dbConnectionFactory.CreateReadOnlyConnection();
		await using var reader = await connection.ExecuteReader(
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

		if (!await reader.ReadAsync())
			return Maybe.Empty<Link>();

		return new Link(
			linkId: reader.GetInt32(0),
			redditPostId: reader.GetString(1),
			linkUrl: reader.GetString(2),
			linkType: ParseRawLinkType(reader.GetInt16(3)),
			createdAt: reader.GetDateTime(4),
			ownerId: reader.GetInt32(5)
		);
	}

	public async IAsyncEnumerable<Link> FindAllLinksByPostId(string redditPostId, [EnumeratorCancellation] CancellationToken cancellationToken)
	{
		await using var connection = _dbConnectionFactory.CreateReadOnlyConnection();
		var linkQueryResult = connection.QueryUnbuffered<LinkQueryResult>(
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
			new { redditPostId }, cancellationToken);

		await foreach (var result in linkQueryResult)
		{
			yield return new Link(
				linkId: result.LinkId,
				redditPostId: result.RedditPostId,
				linkUrl: result.LinkUrl,
				linkType: ParseRawLinkType(result.LinkType),
				createdAt: result.CreatedAt,
				ownerId: result.OwnerId
			);
		}
	}

	public async Task DeleteLinkById(int linkId)
	{
		await using var connection = _dbConnectionFactory.CreateConnection();
		await using var tx = await connection.CreateTransaction(IsolationLevel.Serializable, CancellationToken.None);
		var redditPostId = await tx.ExecuteScalar<string>(
			@"
				UPDATE links SET is_deleted = true WHERE link_id = @linkId AND is_deleted = false
				RETURNING reddit_post_id",
			new { linkId });

		await QueueRedditPostIdForUpdate(tx, redditPostId);

		await tx.Commit(CancellationToken.None);
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
		await using var connection = _dbConnectionFactory.CreateConnection();

		var existingLink = await connection.ExecuteScalar<bool?>(
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
			return Result<int, CreateLinkError>.CreateFailure(new LinkAlreadyExists());

		await using var tx = await connection.CreateTransaction(IsolationLevel.Serializable, CancellationToken.None);

		await tx.Execute("""
			INSERT INTO reddit_posts (reddit_post_id, post_title)
			VALUES (@redditPostId, @postTitle)
			ON CONFLICT DO NOTHING
			""", new { redditPostId = link.RedditPostId, postTitle = link.RedditPostTitle });

		var linkIdResult = await tx.ExecuteScalar<int?>("""
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

		await tx.Commit(CancellationToken.None);

		return Result<int, CreateLinkError>.CreateSuccess(linkId);
	}

	public async Task<IReadOnlyCollection<Link>> GetAllLinksByUserId(int userId)
	{
		await using var connection = _dbConnectionFactory.CreateReadOnlyConnection();
		var queryResult = await connection.Query<LinkQueryResult>(
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
		await using var connection = _dbConnectionFactory.CreateReadOnlyConnection();
		var reader = await connection.Query<(int QueuedItemId, DateTime QueuedAt, string RedditPostId)>(
			@"SELECT queued_item_id, queued_at, reddit_post_id
				FROM link_queue
				WHERE processed_at IS NULL
				ORDER BY queued_at ASC");

		foreach (var (queuedItemId, queuedAt, redditPostId) in reader)
			yield return (QueuedItemId: queuedItemId, QueuedAt: queuedAt, RedditPostId: redditPostId);
	}

	public static async Task MarkRedditPostIdAsProcessed(IDatabaseTransactionConnection<ReadWrite> tx, int itemId) =>
		await tx.Execute(
			@"UPDATE link_queue SET processed_at = CURRENT_TIMESTAMP WHERE queued_item_id = @itemId",
			new { itemId });

	public async Task<IReadOnlyCollection<Link>> GetLinksByRedditPostId(string redditPostId)
	{
		await using var connection = _dbConnectionFactory.CreateReadOnlyConnection();
		var queryResult = await connection.Query<LinkQueryResult>(
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
	private static async Task QueueRedditPostIdForUpdate(IDatabaseTransactionConnection<ReadWrite> tx, string redditPostId)
	{
		// There is certainly a better way to do this. Oh well!
		var hasPendingUpdate = await tx.ExecuteScalar<bool>(
			@"SELECT EXISTS(SELECT 1 FROM link_queue WHERE reddit_post_id = @redditPostId AND processed_at IS NULL LIMIT 1)",
			new { redditPostId });

		if (!hasPendingUpdate)
		{
			await tx.Execute(
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
