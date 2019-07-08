import { Request, Response, Router } from "express";
import fs from "fs";
import HttpStatus, { OK } from "http-status-codes";
import path from "path";
import { Submission, Subreddit } from "snoowrap";
import { authorized } from ".";
import { response } from "..";
import * as configuration from "../../configuration";
import { AvailableMirror, CommentReply } from "../../entity";
import { redditapi } from "../../redditapi";
import { CommentReplyStatus } from "../../structures";

const router: Router = Router();

const TEMPLATES_LOCATION = process.env.TEMPLATES_LOCATION;

/** The template string used in comment replies */
const TEMPLATE_COMMENTREPLY: string = fs.readFileSync(
  path.resolve(`${TEMPLATES_LOCATION}/commentreply.md`),
  "utf-8"
);

/**
 * Checks if a-mirror-bot is a moderator of the specified subreddit
 * @param subreddit The subreddit to check
 */
async function isSubredditMod(subreddit: Subreddit) {
  let mods = await subreddit.getModerators();

  mods.forEach(mod => {
    if (mod.name === configuration.reddit.username) return true;
  });

  return false;
}

/**
 * Checks if the specified submission already has stickied comments
 * @param submissionId The submission ID to check
 */
async function hasStickiedReplies(submissionId: string) {
  let submission = redditapi.getSubmission(submissionId);
  submission.comments.forEach(comment => {
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
  // @ts-ignore
  // FIXME: due to an issue with snoowrap typings, the 'await' keyword causes compile errors. see https://github.com/DefinitelyTyped/DefinitelyTyped/issues/33139
  let reply = await redditapi.getComment(comment.redditPostId_Reply).fetch();
  await reply.delete();

  comment.redditPostId_Reply = null;
  comment.status = CommentReplyStatus.Current;
  await comment.save();
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
  post = redditapi.getSubmission(comment.redditPostId_Parent);
  let mirrors: AvailableMirror[];

  mirrors = await AvailableMirror.find({
    where: {
      redditPostId: comment.redditPostId_Parent
    },
    order: {
      createdAt: "ASC"
    }
  });

  if (mirrors.length <= 0) return deleteComment(comment);

  let commentBody = generateFormattedMirrors(mirrors);

  let reply;
  if (comment.redditPostId_Reply) {
    // @ts-ignore
    // FIXME: due to an issue with snoowrap typings, the 'await' keyword causes compile errors. see https://github.com/DefinitelyTyped/DefinitelyTyped/issues/33139
    reply = await redditapi.getComment(comment.redditPostId_Reply).fetch();
    await reply.edit(commentBody);
    await reply.refresh();
  } else {
    // @ts-ignore
    // FIXME: due to an issue with snoowrap typings, the 'await' keyword causes compile errors. see https://github.com/DefinitelyTyped/DefinitelyTyped/issues/33139
    reply = await post.reply(commentBody);
  }

  if (await isSubredditMod(reply.subreddit)) {
    // @ts-ignore
    // FIXME: due to an issue with snoowrap typings, the 'await' keyword causes compile errors. see https://github.com/DefinitelyTyped/DefinitelyTyped/issues/33139
    await reply.distinguish({
      status: true,
      sticky: !(await hasStickiedReplies(comment.redditPostId_Parent))
    });
  }

  comment.redditPostId_Reply = reply.id;
  comment.status = CommentReplyStatus.Current;
  await comment.save();
  return comment;
}

router.post("/sync", (req: Request, res: Response) => {
  authorized(req, res, async () => {
    let comments: CommentReply[];

    try {
      comments = await CommentReply.find({
        where: {
          status: CommentReplyStatus.Outdated
        },
        order: {
          updatedAt: "ASC"
        },
        take: 10 // A limit is specified as not to launch a mini-DoS attack against reddit's API
      });
    } catch (err) {
      req.log.fatal(`Error retrieving outdated comment replies`);

      return response(res, {
        status: HttpStatus.INTERNAL_SERVER_ERROR,
        message: `Error retrieving outdated comment replies`
      });
    }

    if (!comments || comments.length <= 0)
      return response(res, {
        status: HttpStatus.OK,
        message: `All comments are up-to-date`
      });

    for (const comment of comments) {
      try {
        await processCommentUpdates(comment);
      } catch (err) {
        req.log.fatal(err);

        return response(res, {
          status: HttpStatus.INTERNAL_SERVER_ERROR,
          message: `Error processing comment updates`
        });
      }
    }

    let numPostsUpdated = comments.length;

    return response(res, {
      status: OK,
      message: `Updated ${numPostsUpdated} comment(s)`,
      data: {
        numPostsUpdated: comments.length
      }
    });
  });
});

export const CommentReplyCronApi: Router = router;
