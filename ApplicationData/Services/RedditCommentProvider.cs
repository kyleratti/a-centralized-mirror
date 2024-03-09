using System.Data;
using Dapper;
using Dapper.Transaction;
using FruityFoundation.Base.Extensions;
using FruityFoundation.Base.Structures;

namespace ApplicationData.Services;

public class RedditCommentProvider
{
	private readonly IDbConnectionFactory _dbConnectionFactory;

	public RedditCommentProvider(IDbConnectionFactory dbConnectionFactory)
	{
		_dbConnectionFactory = dbConnectionFactory;
	}

	public async Task<Maybe<string>> FindCommentIdByPostId(string redditPostId)
	{
		using var connection = await _dbConnectionFactory.CreateReadOnlyConnection();
		var result = await connection.ExecuteScalarAsync<string?>(
			@"SELECT reddit_comment_id FROM reddit_comments WHERE reddit_post_id = @redditPostId",
			new { redditPostId });

		return result.ToMaybe();
	}

	public static async Task CreateOrUpdateLinkedComment(IDbTransaction tx, string redditPostId, string redditCommentId) =>
		await tx.ExecuteAsync(
			@"INSERT INTO reddit_comments (reddit_post_id, reddit_comment_id, posted_at, last_updated_at)
				VALUES (@redditPostId, @redditCommentId, CURRENT_TIMESTAMP, CURRENT_TIMESTAMP)
				ON CONFLICT (reddit_post_id)
				DO UPDATE
					SET reddit_comment_id = @redditCommentId, last_updated_at = CURRENT_TIMESTAMP",
			new { redditPostId, redditCommentId});
}
