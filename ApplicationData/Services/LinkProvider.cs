using System.Runtime.CompilerServices;
using Core.DbConnection;
using DataClasses;
using FruityFoundation.Base.Extensions;
using FruityFoundation.Base.Structures;
using Npgsql;
using ResultMonad;

namespace ApplicationData.Services;

public class LinkProvider
{
	private readonly DbConnection _db;

	public LinkProvider(
		DbConnection dbConnection
	)
	{
		_db = dbConnection;
	}

	public async Task<Maybe<Link>> FindLink(int userId, string redditPostId, string linkUrl, LinkKind linkKind)
	{
		await using var reader = await _db.ExecuteReaderAsync(
			@"SELECT l.link_id, l.reddit_post_id, l.link_url, l.link_type, l.created_at, l.owner
				FROM app.links l
				WHERE
					l.reddit_post_id = @redditPostId
					AND l.link_url = @linkUrl
					AND l.link_type = @linkType
					AND l.owner = @userId
					AND l.is_deleted = false",
			new NpgsqlParameter[]
			{
				new("@redditPostId", redditPostId),
				new("@linkUrl", linkUrl),
				new("@linkType", linkKind.RawValue),
				new("@userId", userId),
			});

		if (!await reader.ReadAsync())
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
		await using var reader = await _db.ExecuteReaderAsync(
			$@"SELECT l.link_id, l.reddit_post_id, l.link_url, l.link_type, l.created_at, l.owner
				FROM app.links l
				WHERE
					l.link_id = @linkId
					{(userId.HasValue ? "AND l.owner = @userId" : "")}
					AND l.is_deleted = false",
			new NpgsqlParameter[]
			{
				new("@linkId", linkId),
				new("@userId", userId.OrValue(-1))
			});

		if (!await reader.ReadAsync())
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

	public async Task DeleteLinkById(int linkId)
	{
		await using var tx = await _db.CreateTransactionAsync(System.Data.IsolationLevel.Serializable);
		var redditPostId = (string)await tx.ExecuteSqlScalarAsync(
			@"
				UPDATE app.links SET is_deleted = true WHERE link_id = @linkId AND is_deleted = false
				RETURNING reddit_post_id",
			new[] { new NpgsqlParameter("@linkId", linkId) });

		await QueueRedditPostIdForUpdate(tx, redditPostId);

		await tx.CommitAsync();
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

	public async Task<Result<int, CreateLinkError>> CreateLink(NewLink link)
	{
		try
		{
			await using var tx = await _db.CreateTransactionAsync(System.Data.IsolationLevel.Serializable);
			var linkIdResult = (int?)await tx.ExecuteSqlScalarAsync("""
			INSERT INTO app.links (reddit_post_id, link_url, link_type, created_at, is_deleted, owner)
			VALUES (@redditPostId, @linkUrl, @linkType, NOW(), false, @userId)
			RETURNING link_id
			""",
				new NpgsqlParameter[]
				{
					new("@redditPostId", link.RedditPostId),
					new("@linkUrl", link.LinkUrl),
					new("@linkType", link.LinkType.RawValue),
					new("@userId", link.OwnerUserId)
				});

			if (!linkIdResult.ToMaybe().Try(out var linkId))
				throw new ApplicationException("Failed to create link");

			await QueueRedditPostIdForUpdate(tx, link.RedditPostId);

			await tx.CommitAsync();

			return Result.Ok<int, CreateLinkError>(linkId);
		}
		catch (PostgresException ex) when (ex is { SqlState: PostgresErrorCodes.UniqueViolation, ConstraintName: "UQ_reddit_post_id_link_url_link_type_is_deleted_owner" })
		{
			return Result.Fail<int, CreateLinkError>(new LinkAlreadyExists());
		}
	}

	public async Task<IReadOnlyCollection<Link>> GetAllLinksByUserId(int userId)
	{
		await using var reader = await _db.ExecuteReaderAsync(
			@"SELECT l.link_id, l.reddit_post_id, l.link_url, l.link_type, l.created_at
				FROM app.links l
				WHERE
					l.owner = @userId
					AND l.is_deleted = false",
			new NpgsqlParameter[]
			{
				new("@userId", userId)
			});

		var results = new List<Link>();

		while (await reader.ReadAsync())
			results.Add(new Link(
				linkId: reader.GetInt32(0),
				redditPostId: reader.GetString(1),
				linkUrl: reader.GetString(2),
				linkType: ParseRawLinkType(reader.GetInt16(3)),
				createdAt: reader.GetDateTime(4),
				ownerId: userId
			));

		return results;
	}

	public async IAsyncEnumerable<(int QueuedItemId, DateTime QueuedAt, string RedditPostId)> GetRedditPostIdsWithPendingChanges(
		[EnumeratorCancellation] CancellationToken cancellationToken
	)
	{
		await using var reader = await _db.ExecuteReaderAsync(
			@"SELECT queued_item_id, queued_at, reddit_post_id
				FROM app.link_queue
				WHERE processed_at IS NULL
				ORDER BY queued_at ASC", Array.Empty<NpgsqlParameter>());

		while (await reader.ReadAsync(cancellationToken))
			yield return (reader.GetInt32(0), reader.GetDateTime(1), reader.GetString(2));
	}

	public static async Task MarkRedditPostIdAsProcessed(IDbConnection tx, int itemId) =>
		await tx.ExecuteSqlNonQuery(
			@"UPDATE app.link_queue SET processed_at = NOW() WHERE queued_item_id = @itemId",
			new NpgsqlParameter[] { new("@itemId", itemId) });

	public async IAsyncEnumerable<Link> GetLinksByRedditPostId(string redditPostId, [EnumeratorCancellation] CancellationToken cancellationToken)
	{
		await using var reader = await _db.ExecuteReaderAsync(
			@"SELECT l.link_id, l.reddit_post_id, l.link_url, l.link_type, l.created_at, l.owner
				FROM app.links l
				WHERE
					l.reddit_post_id = @redditPostId
					AND l.is_deleted = false",
			new NpgsqlParameter[]
			{
				new("@redditPostId", redditPostId)
			});

		while (await reader.ReadAsync(cancellationToken))
			yield return new Link(
				linkId: reader.GetInt32(0),
				redditPostId: reader.GetString(1),
				linkUrl: reader.GetString(2),
				linkType: ParseRawLinkType(reader.GetInt16(3)),
				createdAt: reader.GetDateTime(4),
				ownerId: reader.GetInt32(5)
			);
	}

	private static LinkKind ParseRawLinkType(short rawLinkType) => rawLinkType switch
	{
		1 => LinkKind.Mirror,
		2 => LinkKind.Download,
		// ReSharper disable once NotResolvedInText
		_ => throw new ArgumentOutOfRangeException("link_type", rawLinkType, "Unknown link type")
	};

	// TODO: this should be a much better system
	private static async Task QueueRedditPostIdForUpdate(DbTransactionWrapper tx, string redditPostId)
	{
		// There is certainly a better way to do this. Oh well!
		var hasPendingUpdate = (int?)await tx.ExecuteSqlScalarAsync(
			@"SELECT 1 FROM app.link_queue WHERE reddit_post_id = @redditPostId AND processed_at IS NULL LIMIT 1",
			new[] { new NpgsqlParameter("@redditPostId", redditPostId) }) == 1;

		if (!hasPendingUpdate)
		{
			await tx.ExecuteSqlNonQueryAsync(
				@"INSERT INTO app.link_queue (reddit_post_id, queued_at) VALUES (@redditPostId, NOW())",
				new[] { new NpgsqlParameter("@redditPostId", redditPostId) });
		}
	}
}