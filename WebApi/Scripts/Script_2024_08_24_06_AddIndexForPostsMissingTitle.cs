using FruityFoundation.DataAccess.Abstractions;

namespace WebApi.Scripts;

/// <summary>
/// Fill reddit_posts.is_title_fetched = 0 where it is null
/// </summary>
public class Script_2024_08_24_06_AddIndexForPostsMissingTitle : IDbMaintenanceScript
{
	/// <inheritdoc />
	public async Task Run(IDatabaseConnection<ReadWrite> dbConnection)
	{
		var indexExists = await dbConnection.ExecuteScalar<bool>(
			"SELECT EXISTS (SELECT 1 FROM pragma_index_list('reddit_posts') WHERE name = 'IX_reddit_posts_is_title_fetched_false')");

		await dbConnection.Execute(
			"""
				create index IX_reddit_posts_is_title_fetched_false
				on reddit_posts (is_title_fetched)
				where reddit_posts.is_title_fetched = 0;
				
				""");
	}
}
