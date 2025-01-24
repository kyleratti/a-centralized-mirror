using FruityFoundation.DataAccess.Abstractions;

namespace WebApi.Scripts;

/// <summary>
/// Script_2025_01_23T22_03_RemoveBadPostId
/// </summary>
public class Script_2025_01_23T22_03_RemoveBadPostId : IDbMaintenanceScript
{
	/// <inheritdoc />
	public async Task Run(IDatabaseConnection<ReadWrite> dbConnection)
	{
		const string postId = "1hzuql7";

		await dbConnection.Execute(
			"""
			BEGIN TRANSACTION;
			DELETE FROM reddit_comments WHERE reddit_post_id = @postId;
			DELETE FROM link_queue WHERE reddit_post_id = @postId;
			DELETE FROM links WHERE reddit_post_id = @postId;
			COMMIT;
			""", new { postId }, CancellationToken.None);
	}
}
