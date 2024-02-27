using System.Data;
using Dapper;
using Dapper.Transaction;
using FruityFoundation.Base.Extensions;
using FruityFoundation.Base.Structures;

namespace ApplicationData.Services;

public class RedditCommentProvider
{
	private readonly IDbConnection _db;

	public RedditCommentProvider(IDbConnection db)
	{
		_db = db;
	}

	public async Task<Maybe<string>> FindCommentIdByPostId(string redditPostId)
	{
		var result = await _db.ExecuteScalarAsync<string?>(
			@"SELECT reddit_comment_id FROM app.reddit_comments WHERE reddit_post_id = @redditPostId",
			new { redditPostId });

		return result.ToMaybe();
	}

	public static async Task CreateOrUpdateLinkedComment(IDbTransaction tx, string redditPostId, string redditCommentId) =>
		// FIXME: SQLite issue: ON CONFLICT is not supported
		await tx.ExecuteAsync(
			@"INSERT INTO app.reddit_comments (reddit_post_id, reddit_comment_id, posted_at, last_updated_at)
				VALUES (@redditPostId, @redditCommentId, NOW(), NOW())
				ON CONFLICT (reddit_post_id)
				DO UPDATE
					SET reddit_comment_id = @redditCommentId, last_updated_at = NOW()",
			new { redditPostId, redditCommentId});

	public async Task<int> DeleteLinkedComment(string redditPostId) =>
		await _db.ExecuteAsync(
			@"DELETE FROM app.reddit_comments WHERE reddit_post_id = @redditPostId",
			new { redditPostId });
}
