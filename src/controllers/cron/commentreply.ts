import { Request, Response, Router } from "express";
import fs from "fs";
import HttpStatus, { OK } from "http-status-codes";
import path from "path";
import { Comment as RedditComment, Submission, Subreddit } from "snoowrap";
import { authorized } from ".";
import { response } from "..";
import * as configuration from "../../configuration";
import { AvailableMirror, CommentReply } from "../../entity";
import { redditapi } from "../../services";
import { CommentReplyStatus } from "../../structures";

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

/**
 * Generates a formatted comment reply string containing all available mirrors
 * @param mirrors An array of AvailableMirror objects
 */
function generateFormattedMirrors(mirrors: AvailableMirror[]): string {
  let formattedMirrors: string[] = [];

  for (let i = 0; i < mirrors.length; i++) {
    const mirror = mirrors[i];

    let mirrorUrl = mirror.mirrorUrl;
    let botUsername = mirror.bot.username;

    formattedMirrors.push(
      `* [Mirror #${i + 1}](${mirrorUrl}) (provided by /u/${botUsername})`
    );
  }

  return TEMPLATE_COMMENTREPLY.replace("%s", formattedMirrors.join("\n"));
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

  let mirrors: AvailableMirror[];

  mirrors = await AvailableMirror.find({
    where: {
      redditPostId: comment.redditPostId_Parent,
    },
    order: {
      createdAt: "ASC",
    },
  });

  if (mirrors.length <= 0) return deleteComment(comment);

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

    try {
      outdatedComments = await CommentReply.find({
        where: {
          status: CommentReplyStatus.Outdated,
        },
        order: {
          updatedAt: "ASC",
        },
        take: 10, // A limit is specified as not to launch a mini-DoS attack against reddit's API
      });
    } catch (err) {
      req.log.fatal(`Error retrieving outdated comment replies`);

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
