using FruityFoundation.DataAccess.Abstractions;

namespace WebApi.Scripts;

///Script_2024_08_30_01_SetTitlesAsFetchedIfTitleIsPresent
public class Script_2024_08_30_01_SetTitlesAsFetchedIfTitleIsPresent : IDbMaintenanceScript
{
	/// <inheritdoc />
	public async Task Run(IDatabaseConnection<ReadWrite> dbConnection)
	{
		await dbConnection.Execute("UPDATE reddit_posts SET is_title_fetched = 1 WHERE post_title IS NOT NULL");
	}
}
