using FruityFoundation.DataAccess.Abstractions;

namespace WebApi.Scripts;

/// <summary>
/// Add foreign key to reddit posts
/// </summary>
public class Script_2024_08_24_02_PopulateRedditPostIds : IDbMaintenanceScript
{
	/// <inheritdoc />
	public async Task Run(IDatabaseConnection<ReadWrite> dbConnection)
	{
		await dbConnection.Execute(
			"""
				INSERT INTO reddit_posts (reddit_post_id, post_title)
				SELECT rc.reddit_post_id, NULL
				FROM reddit_comments rc
				WHERE NOT EXISTS (
					SELECT 1
					FROM reddit_posts rp
					WHERE rp.reddit_post_id = rc.reddit_post_id
				)
				""");
	}
}
