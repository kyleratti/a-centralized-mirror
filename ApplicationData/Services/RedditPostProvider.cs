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
}
