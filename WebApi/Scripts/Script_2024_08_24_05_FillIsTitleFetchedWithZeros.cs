using FruityFoundation.DataAccess.Abstractions;

namespace WebApi.Scripts;

/// <summary>
/// Fill reddit_posts.is_title_fetched = 0 where it is null
/// </summary>
public class Script_2024_08_24_05_FillIsTitleFetchedWithZeros : IDbMaintenanceScript
{
	/// <inheritdoc />
	public async Task Run(IDatabaseConnection<ReadWrite> dbConnection)
	{
		await dbConnection.Execute(
			"""
				UPDATE reddit_posts
				SET is_title_fetched = 0
				WHERE is_title_fetched IS NULL;
				""");
	}
}
