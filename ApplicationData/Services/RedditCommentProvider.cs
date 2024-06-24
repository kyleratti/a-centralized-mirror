using FruityFoundation.Base.Structures;
using FruityFoundation.DataAccess.Abstractions;

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
		await using var connection = _dbConnectionFactory.CreateReadOnlyConnection();
		var result = await connection.ExecuteScalar<string?>(
			@"SELECT reddit_comment_id FROM reddit_comments WHERE reddit_post_id = @redditPostId",
			new { redditPostId });

		return result.AsMaybe();
	}

	public static async Task CreateOrUpdateLinkedComment(IDatabaseTransactionConnection<ReadWrite> tx, string redditPostId, string redditCommentId) =>
		await tx.Execute(
			@"INSERT INTO reddit_comments (reddit_post_id, reddit_comment_id, posted_at, last_updated_at)
				VALUES (@redditPostId, @redditCommentId, CURRENT_TIMESTAMP, CURRENT_TIMESTAMP)
				ON CONFLICT (reddit_post_id)
				DO UPDATE
					SET reddit_comment_id = @redditCommentId, last_updated_at = CURRENT_TIMESTAMP",
			new { redditPostId, redditCommentId});
}
