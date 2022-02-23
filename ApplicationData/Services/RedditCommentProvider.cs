using Core.DbConnection;
using FruityFoundation.Base.Extensions;
using FruityFoundation.Base.Structures;
using Npgsql;

namespace ApplicationData.Services;

public class RedditCommentProvider
{
	private readonly DbConnection _db;

	public RedditCommentProvider(DbConnection db)
	{
		_db = db;
	}

	public async Task<Maybe<string>> FindCommentIdByPostId(string redditPostId)
	{
		var result = (string?)await _db.ExecuteScalarAsync(
			@"SELECT reddit_comment_id FROM app.reddit_comments WHERE reddit_post_id = @redditPostId",
			new NpgsqlParameter[]
			{
				new("@redditPostId", redditPostId)
			});

		return result.ToMaybe();
	}

	public static async Task CreateOrUpdateLinkedComment(IDbConnection tx, string redditPostId, string redditCommentId) =>
		await tx.ExecuteSqlNonQuery(
			@"INSERT INTO app.reddit_comments (reddit_post_id, reddit_comment_id, posted_at, last_updated_at)
				VALUES (@redditPostId, @redditCommentId, NOW(), NOW())
				ON CONFLICT (reddit_post_id)
				DO UPDATE
					SET reddit_comment_id = @redditCommentId, last_updated_at = NOW()",
			new NpgsqlParameter[]
			{
				new("@redditPostId", redditPostId),
				new("@redditCommentId", redditCommentId)
			});

	public async Task<int> DeleteLinkedComment(string redditPostId) =>
		await _db.ExecuteSqlNonQueryAsync(
			@"DELETE FROM app.reddit_comments WHERE reddit_post_id = @redditPostId",
			new NpgsqlParameter[]
			{
				new("@redditPostId", redditPostId)
			});
}