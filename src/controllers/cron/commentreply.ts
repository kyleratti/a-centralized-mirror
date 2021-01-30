import { Request, Response, Router } from "express";
import fs from "fs";
import HttpStatus, { OK } from "http-status-codes";
import path from "path";
import { Comment as RedditComment, Submission, Subreddit } from "snoowrap";
import { In, Not } from "typeorm";
import { authorized } from ".";
import { response } from "..";
import * as configuration from "../../configuration";
import { AvailableMirror, CommentReply } from "../../entity";
import { redditapi } from "../../services";
import { CommentReplyStatus, LinkType } from "../../structures";

const router: Router = Router();

/** The template string used in comment replies */
const TEMPLATE_COMMENTREPLY: string = fs.readFileSync(
  path.resolve("templates/commentreply.md"),
  "utf-8"
);

/**
 * Checks if a-mirror-bot is a moderator of the specified subreddit
 * @param subreddit The subreddit to check
 */
async function isSubredditMod(subreddit: Subreddit) {
  return (
    (
      await subreddit.getModerators({
        name: configuration.reddit.username,
      })
    ).length > 0
  );
}

/**
 * Checks if the specified submission already has stickied comments
 * @param submissionId The submission ID to check
 */
async function hasStickiedReplies(submissionId: string) {
  let submission = redditapi.getSubmission(submissionId);

  submission.comments.fetchAll().forEach((comment) => {
    if (comment.stickied) return true;
  });

  return false;
}

const generateFormattedDownloads = (downloads: AvailableMirror[]) => {
  const formattedDownloads = [];

  downloads.forEach((download) => {
    const [mirrorUrl, botUsername] = [
      download.mirrorUrl,
      download.bot.username,
    ];

    formattedDownloads.push();
  });
};

/**
 * Generates a formatted comment reply string containing all available mirrors
 * @param mirrors An array of AvailableMirror objects
 */
function generateFormattedMirrors(allMirrors: AvailableMirror[]): string {
  const [mirrors, downloads] = [
    allMirrors.filter((obj) => obj.linkType === LinkType.Mirror),
    allMirrors.filter((obj) => obj.linkType === LinkType.Download),
  ];
  const hasMirrorsAndLinks = mirrors.length > 0 && downloads.length > 0;
  const formattedLinks: string[] = [];

  const appendAll = (links: AvailableMirror[]) =>
    links.forEach((link, idx) => {
      const prefix =
        link.linkType === null || link.linkType === LinkType.Mirror
          ? "Mirror"
          : "Download";
      const [mirrorUrl, botUsername] = [link.mirrorUrl, link.bot.username];

      formattedLinks.push(
        `* [${prefix} #${
          idx + 1
        }](${mirrorUrl}) (provided by /u/${botUsername})`
      );
    });

  if (mirrors.length > 0) {
    formattedLinks.push(`**Mirrors**`, `\n`);
    appendAll(mirrors);
  }

  if (downloads.length > 0) {
    if (hasMirrorsAndLinks) formattedLinks.push("\n");
    formattedLinks.push(`**Downloads**`, `\n`);
    appendAll(downloads);
  }

  return TEMPLATE_COMMENTREPLY.replace("%s", formattedLinks.join("\n"));
}

/**
 * Deletes the comment reply from a submission
 * @param comment The reddit CommentReply to delete
 */
async function deleteComment(comment: CommentReply) {
  if (comment.redditPostId_Reply) {
    try {
      // @ts-ignore
      // FIXME: due to an issue with snoowrap typings, the 'await' keyword causes compile errors. see https://github.com/DefinitelyTyped/DefinitelyTyped/issues/33139
      const reply = await redditapi
        .getComment(comment.redditPostId_Reply)
        .fetch();
      await reply.delete();
    } catch (err) {
      throw err;
    }

    comment.redditPostId_Reply = null;
    comment.status = CommentReplyStatus.Current;
    await comment.save();
  } else {
    throw `No reddit comment reply found in database; nothing to remove`;
  }
  return comment;
}

/**
 * Processes updates on the specified CommentReply, posting or editing the existing post as necessary
 * @param comment The CommentReply to process updates on
 */
async function processCommentUpdates(comment: CommentReply) {
  let post: Submission;

  // @ts-ignore
  // FIXME: due to an issue with snoowrap typings, the 'await' keyword causes compile errors. see https://github.com/DefinitelyTyped/DefinitelyTyped/issues/33139
  post = await redditapi.getSubmission(comment.redditPostId_Parent).fetch();

  let mirrors = await AvailableMirror.find({
    where: {
      redditPostId: comment.redditPostId_Parent,
    },
    order: {
      createdAt: "ASC",
    },
  });

  if (mirrors.length <= 0) return deleteComment(comment);

  // This is a dirty workaround for sorting mirrors by the
  // weight of their registered bot. This really should be done
  // with an SQL query, but I opted to do this instead because
  // there is already a bunch of Active Record stuff in this
  // model and I don't want to break pattern. That's probably a
  // bad reason. In fact, I know it is.
  mirrors.sort((a, b) => a.bot.weight - b.bot.weight);

  let commentBody = generateFormattedMirrors(mirrors);

  let reply: RedditComment;
  let success = false;
  if (comment.redditPostId_Reply) {
    // @ts-ignore
    // FIXME: due to an issue with snoowrap typings, the 'await' keyword causes compile errors. see https://github.com/DefinitelyTyped/DefinitelyTyped/issues/33139
    reply = await redditapi.getComment(comment.redditPostId_Reply).fetch();
    // @ts-ignore
    // FIXME: see https://github.com/not-an-aardvark/snoowrap/issues/221
    await reply.edit(commentBody);
    // @ts-ignore
    // FIXME: see https://github.com/not-an-aardvark/snoowrap/issues/221
    await reply.refresh();
    success = true;
  } else {
    // This should fix trying to reply to backlogged items that are too old to be commented on
    if (post.archived) {
      comment.status = CommentReplyStatus.Expired;
      await comment.save();
      return comment;
    }

    // @ts-ignore
    // FIXME: due to an issue with snoowrap typings, the 'await' keyword causes compile errors. see https://github.com/DefinitelyTyped/DefinitelyTyped/issues/33139
    reply = await post.reply(commentBody);

    if (await isSubredditMod(reply.subreddit)) {
      // @ts-ignore
      // FIXME: see https://github.com/not-an-aardvark/snoowrap/issues/221
      await reply.lock();

      // @ts-ignore
      await reply.ignoreReports();
    }

    success = true;
  }

  if (await isSubredditMod(reply.subreddit)) {
    // @ts-ignore
    // FIXME: due to an issue with snoowrap typings, the 'await' keyword causes compile errors. see https://github.com/DefinitelyTyped/DefinitelyTyped/issues/33139
    await reply.distinguish({
      status: true,
      sticky: !(await hasStickiedReplies(comment.redditPostId_Parent)),
    });
  }

  comment.redditPostId_Reply = reply.id;
  comment.status =
    reply?.id && success
      ? CommentReplyStatus.Current
      : CommentReplyStatus.Outdated;
  await comment.save();
  return comment;
}

router.post("/sync", (req: Request, res: Response) => {
  authorized(req, res, async () => {
    let outdatedComments: CommentReply[];

    // FIXME: add proper error handling

    // I decided to split this up into two queries because there may be a time/definitely was
    // a time that the bot stopped posting to reddit for an extended period but was still
    // processing update API requests. Once it comes back online, there is a massive queue
    // of posts that need processing. During times of high volume of submissions, this can
    // quickly overwhelm the bot and result in hours if not days before it gets caught up
    // and starts mirroring new submissions. The compromise is to attack 5 of the oldest
    // and 5 of the newest, ensuring the backlog is processed while new comments are, too.
    // Eventually the bot will catch back up and things will work as expected (e.g. within
    // 60 seconds)
    const MAX_TO_PROCESS = 10;

    try {
      const oldestOutdatedComments = await CommentReply.find({
        where: {
          status: CommentReplyStatus.Outdated,
        },
        order: {
          updatedAt: "ASC",
        },
        take: MAX_TO_PROCESS / 2, // A limit is specified as not to launch a mini-DoS attack against reddit's API
      });

      const newestOutdatedComments = await CommentReply.find({
        where: {
          status: CommentReplyStatus.Outdated,
          id: Not(In(oldestOutdatedComments.map((comment) => comment.id))),
        },
        order: {
          updatedAt: "DESC",
        },
        take: MAX_TO_PROCESS / 2,
      });

      outdatedComments = [...oldestOutdatedComments, ...newestOutdatedComments];
    } catch (err) {
      req.log.fatal({
        msg: `Error retrieving outdated comment replies`,
        err: err,
      });

      return response(res, {
        status: HttpStatus.INTERNAL_SERVER_ERROR,
        message: `Error retrieving outdated comment replies`,
      });
    }

    if (!outdatedComments || outdatedComments.length <= 0)
      return response(res, {
        status: HttpStatus.OK,
        message: `All comments are up-to-date`,
      });

    for (const comment of outdatedComments) {
      try {
        await processCommentUpdates(comment);
      } catch (err) {
        req.log.fatal({
          msg: `Failed processing reddit post ${comment.redditPostId_Parent} (reply: ${comment.redditPostId_Reply})`,
          err: err,
        });
      }
    }

    const numPostsUpdated = outdatedComments.length;

    return response(res, {
      status: OK,
      message: `Updated ${numPostsUpdated} comment(s)`,
      data: {
        numPostsUpdated: outdatedComments.length,
      },
    });
  });
});

export const CommentReplyCronApi: Router = router;
