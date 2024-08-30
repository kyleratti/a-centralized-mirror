using FruityFoundation.DataAccess.Abstractions;

namespace WebApi.Scripts;

/// <summary>
/// Script_2024_08_30_03_DropIsTitleFetchedColumn
/// </summary>
public class Script_2024_08_30_03_DropIsTitleFetchedColumn : IDbMaintenanceScript
{
	/// <inheritdoc />
	public async Task Run(IDatabaseConnection<ReadWrite> dbConnection)
	{
		await dbConnection.Execute(
			"""
			create table reddit_posts_dg_tmp
			(
			    reddit_post_id TEXT not null
			        constraint PK_reddit_posts_reddit_post_id
			            primary key,
			    post_title     TEXT
			);

			insert into reddit_posts_dg_tmp(reddit_post_id, post_title)
			select reddit_post_id, post_title
			from reddit_posts;

			drop table reddit_posts;

			alter table reddit_posts_dg_tmp
			    rename to reddit_posts;
			""");
	}
}
