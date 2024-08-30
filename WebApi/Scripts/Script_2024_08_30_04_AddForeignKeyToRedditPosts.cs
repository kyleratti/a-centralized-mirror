﻿using FruityFoundation.DataAccess.Abstractions;

namespace WebApi.Scripts;

/// <summary>
/// Script_2024_08_30_04_AddForeignKeyToRedditPosts
/// </summary>
public class Script_2024_08_30_04_AddForeignKeyToRedditPosts : IDbMaintenanceScript
{
	/// <inheritdoc />
	public async Task Run(IDatabaseConnection<ReadWrite> dbConnection)
	{
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
