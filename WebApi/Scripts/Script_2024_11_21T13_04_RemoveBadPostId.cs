using FruityFoundation.DataAccess.Abstractions;

namespace WebApi.Scripts;

/// <summary>
/// Script_2024_11_21T13_04_RemoveBadPostId
/// </summary>
public class Script_2024_11_21T13_04_RemoveBadPostId : IDbMaintenanceScript
{
	/// <inheritdoc />
	public async Task Run(IDatabaseConnection<ReadWrite> dbConnection)
	{
		await dbConnection.Execute(
			"""
			BEGIN TRANSACTION;
			DELETE FROM reddit_comments WHERE reddit_post_id = '1gwjh0b';
			DELETE FROM link_queue WHERE reddit_post_id = '1gwjh0b';
			DELETE FROM links WHERE reddit_post_id = '1gwjh0b';
			COMMIT;
			""");
	}
}
