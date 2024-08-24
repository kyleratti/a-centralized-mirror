using FruityFoundation.DataAccess.Abstractions;

namespace WebApi.Scripts;

/// <summary>
/// Create reddit posts table
/// </summary>
public class Script_2024_08_24_01_CreateRedditPostsTable : IDbMaintenanceScript
{
	/// <inheritdoc />
	public async Task Run(IDatabaseConnection<ReadWrite> dbConnection)
	{
		var tableExists = await dbConnection.ExecuteScalar<bool>(
			"SELECT EXISTS (SELECT 1 FROM sqlite_master WHERE type='table' AND name = 'reddit_posts')");

		if (tableExists)
			return;

		await dbConnection.Execute(
			"""
				create table reddit_posts
				(
				   reddit_post_id TEXT not null
				       constraint PK_reddit_posts_reddit_post_id
				           primary key,
				   post_title     TEXT null -- nullable for now because we have several thousand existing rows. tomorrow's problem.
				);
				""");
	}
}
