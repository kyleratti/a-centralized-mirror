using FruityFoundation.DataAccess.Abstractions;

namespace WebApi.Scripts;

/// <summary>
/// Script_2025_04_08T17_41_RemoveBadPostId
/// </summary>
public class Script_2025_04_08T17_41_RemoveBadPostId : IDbMaintenanceScript
{
	/// <inheritdoc />
	public async Task Run(IDatabaseConnection<ReadWrite> dbConnection)
	{
		const string postId = "1jt7wsc";

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
