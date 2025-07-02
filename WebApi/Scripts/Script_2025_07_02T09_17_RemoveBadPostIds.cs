using FruityFoundation.DataAccess.Abstractions;

namespace WebApi.Scripts;

/// <summary>
/// Script_2025_07_02T09_17_RemoveBadPostIds
/// </summary>
public class Script_2025_07_02T09_17_RemoveBadPostIds : IDbMaintenanceScript
{
	/// <inheritdoc />
	public async Task Run(IDatabaseConnection<ReadWrite> dbConnection)
	{
		string[] postIds = [
			"1laudfi",
			"1lcrpcw",
			"1l9mq2t",
		];

		foreach (var postId in postIds)
		{
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
}
