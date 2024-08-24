using FruityFoundation.DataAccess.Abstractions;

namespace WebApi.Scripts;

/// <summary>
/// Add foreign key to reddit posts
/// </summary>
public class Script_2024_08_24_07_MakeRedditPostsIsTitleFetchedNotNull : IDbMaintenanceScript
{
	/// <inheritdoc />
	public async Task Run(IDatabaseConnection<ReadWrite> dbConnection)
	{
		var columnIsNotNull = await dbConnection.ExecuteScalar<bool>(
			"SELECT \"notnull\" FROM pragma_table_info('reddit_posts') WHERE name = 'is_title_fetched'");

		if (columnIsNotNull)
			return;

		// First remove the foreign key on reddit_comments -> reddit_posts.reddit_post_id
		await dbConnection.Execute(
			"""
			DROP TABLE IF EXISTS reddit_comments_dg_tmp;
			create table reddit_comments_dg_tmp
			(
			    reddit_post_id    TEXT not null
			        constraint PK_RedditComments_RedditPostId
			            primary key,
			    reddit_comment_id TEXT not null,
			    posted_at         TEXT not null,
			    last_updated_at   TEXT not null
			);

			insert into reddit_comments_dg_tmp(reddit_post_id, reddit_comment_id, posted_at, last_updated_at)
			select reddit_post_id, reddit_comment_id, posted_at, last_updated_at
			from reddit_comments;

			drop table reddit_comments;

			alter table reddit_comments_dg_tmp
			    rename to reddit_comments;
			""");

		// Make is_title_fetched not null
		await dbConnection.Execute(
			"""
				DROP TABLE IF EXISTS reddit_posts_dg_tmp;

				create table reddit_posts_dg_tmp
				(
				    reddit_post_id   TEXT    not null
				        constraint PK_reddit_posts_reddit_post_id
				            primary key,
				    post_title       TEXT,
				    is_title_fetched INTEGER not null
				);
				
				insert into reddit_posts_dg_tmp(reddit_post_id, post_title, is_title_fetched)
				select reddit_post_id, post_title, is_title_fetched
				from reddit_posts;
				
				drop table reddit_posts;
				
				alter table reddit_posts_dg_tmp
				    rename to reddit_posts;
				
				create index IX_reddit_posts_is_title_fetched_false
				    on reddit_posts (is_title_fetched)
				    where reddit_posts.is_title_fetched = 0;
				""");

		// Re-add the foreign key on reddit_comments -> reddit_posts.reddit_post_id
		await dbConnection.Execute(
			"""
			create table reddit_comments_dg_tmp
			(
			    reddit_post_id    TEXT not null
			        constraint PK_RedditComments_RedditPostId
			            primary key
			        constraint FK_reddit_comments_reddit_posts_reddit_post_id
			            references reddit_posts,
			    reddit_comment_id TEXT not null,
			    posted_at         TEXT not null,
			    last_updated_at   TEXT not null
			);

			insert into reddit_comments_dg_tmp(reddit_post_id, reddit_comment_id, posted_at, last_updated_at)
			select reddit_post_id, reddit_comment_id, posted_at, last_updated_at
			from reddit_comments;

			drop table reddit_comments;

			alter table reddit_comments_dg_tmp
			    rename to reddit_comments;
			""");
	}
}
