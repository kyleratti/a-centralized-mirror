using FruityFoundation.DataAccess.Abstractions;

namespace WebApi.Scripts;

/// <summary>
/// Script_2025_01_14T19_30_RemoveBadPostId
/// </summary>
public class Script_2025_01_14T19_30_RemoveBadPostId : IDbMaintenanceScript
{
	/// <inheritdoc />
	public async Task Run(IDatabaseConnection<ReadWrite> dbConnection)
	{
		await dbConnection.Execute(
			"""
			BEGIN TRANSACTION;
			DELETE FROM reddit_comments WHERE reddit_post_id = '1hzuql7';
			DELETE FROM link_queue WHERE reddit_post_id = '1hzuql7';
			DELETE FROM links WHERE reddit_post_id = '1hzuql7';
			COMMIT;
			""");
	}
}
