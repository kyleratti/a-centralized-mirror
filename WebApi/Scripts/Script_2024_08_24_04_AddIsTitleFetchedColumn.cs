using FruityFoundation.DataAccess.Abstractions;

namespace WebApi.Scripts;

/// <summary>
/// Add foreign key to reddit posts
/// </summary>
public class Script_2024_08_24_04_AddIsTitleFetchedColumn : IDbMaintenanceScript
{
	/// <inheritdoc />
	public async Task Run(IDatabaseConnection<ReadWrite> dbConnection)
	{
		var columnExists = await dbConnection.ExecuteScalar<bool>(
			"SELECT EXISTS (SELECT 1 FROM pragma_table_info('reddit_posts') WHERE name = 'is_title_fetched')");

		if (columnExists)
			return;

		await dbConnection.Execute(
			"""
				alter table reddit_posts
				add is_title_fetched INTEGER;
				""");
	}
}
