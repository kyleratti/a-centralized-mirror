using FruityFoundation.DataAccess.Abstractions;

namespace WebApi.Scripts;

/// <summary>
/// Script_2024_11_20T19_15_RemoveBadPostId
/// </summary>
public class Script_2024_11_20T19_15_RemoveBadPostId : IDbMaintenanceScript
{
	/// <inheritdoc />
	public async Task Run(IDatabaseConnection<ReadWrite> dbConnection)
	{
		await dbConnection.Execute(
			"""
			BEGIN TRANSACTION;
			DELETE FROM reddit_comments WHERE reddit_post_id = '1gfmn96';
			DELETE FROM link_queue WHERE reddit_post_id = '1gfmn96';
			DELETE FROM links WHERE reddit_post_id = '1gfmn96';
			COMMIT;
			""");
	}
}
