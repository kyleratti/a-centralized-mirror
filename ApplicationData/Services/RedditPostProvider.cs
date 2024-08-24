using System.Runtime.CompilerServices;
using FruityFoundation.Base.Structures;
using FruityFoundation.DataAccess.Abstractions;

namespace ApplicationData.Services;

public class RedditPostProvider
{
	private readonly IDbConnectionFactory _dbConnectionFactory;

	public RedditPostProvider(IDbConnectionFactory dbConnectionFactory)
	{
		_dbConnectionFactory = dbConnectionFactory;
	}

	public async Task<Maybe<string>> GetPostTitleByPostId(string redditPostId, CancellationToken cancellationToken)
	{
		await using var connection = _dbConnectionFactory.CreateReadOnlyConnection();
		await using var reader = await connection.ExecuteReader(
			"SELECT post_title FROM reddit_posts WHERE reddit_post_id = @redditPostId",
			new { redditPostId }, cancellationToken);

		if (!await reader.ReadAsync(cancellationToken))
			return Maybe.Empty<string>();

		return reader.TryGetString(0);
	}

	public async IAsyncEnumerable<string> GetPostIdsWithoutTitleFetched([EnumeratorCancellation] CancellationToken cancellationToken)
	{
		await using var connection = _dbConnectionFactory.CreateReadOnlyConnection();
		await using var reader = await connection.ExecuteReader(
			"SELECT reddit_post_id FROM reddit_posts WHERE post_title IS NULL AND is_title_fetched = 0",
			cancellationToken: cancellationToken);

		while (await reader.ReadAsync(cancellationToken))
		{
			yield return reader.GetString(0);
		}
	}

	public async Task SetPostIdAsTitleFetched(string redditPostId, CancellationToken cancellationToken)
	{
		await using var connection = _dbConnectionFactory.CreateConnection();
		await connection.Execute(
			"UPDATE reddit_posts SET is_title_fetched = 1 WHERE reddit_post_id = @redditPostId",
			new { redditPostId }, cancellationToken);
	}

	public async Task SetPostTitle(string redditPostId, string title, CancellationToken cancellationToken)
	{
		await using var connection = _dbConnectionFactory.CreateConnection();
		await connection.Execute(
			"""
			UPDATE reddit_posts
			SET
				post_title = @title
				,is_title_fetched = 1
			WHERE reddit_post_id = @redditPostId
			""", new { redditPostId, title }, cancellationToken);
	}
}
