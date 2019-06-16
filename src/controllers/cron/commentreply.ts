import { Request, Response, Router } from "express";
import fs from "fs";
import HttpStatus, { OK } from "http-status-codes";
import { Submission, Subreddit } from "snoowrap";
import { authorized } from ".";
import { response } from "..";
import * as configuration from "../../configuration";
import { AvailableMirror, CommentReply } from "../../entity";
import { redditapi } from "../../redditapi";
import { CommentReplyStatus } from "../../structures";

const router: Router = Router();

const TEMPLATE_COMMENTREPLY: string = fs.readFileSync(
  "./templates/commentreply.md",
  "utf-8"
);

async function isSubredditMod(subreddit: Subreddit) {
  let mods = await subreddit.getModerators();

  mods.forEach(mod => {
    if (mod.name === configuration.reddit.username) return true;
  });

  return false;
}

async function hasStickiedReplies(subredditId: string) {
  let subreddit = redditapi.getSubmission(subredditId);
  subreddit.comments.forEach(comment => {
    if (comment.stickied) return true;
  });

  return false;
}

function getFormattedMirrors(mirrors: AvailableMirror[]) {
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

function processCommentUpdates(comment: CommentReply) {
  return new Promise<CommentReply>(async (success, fail) => {
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

    let replyBody = getFormattedMirrors(mirrors);

    let reply;
    if (comment.redditPostId_Reply) {
      reply = redditapi.getComment(comment.redditPostId_Reply);
    } else {
      // @ts-ignore
      // FIXME: due to an issue with snoowrap typings, the 'await' keyword causes compile errors. see https://github.com/DefinitelyTyped/DefinitelyTyped/issues/33139
      let postedReply = await post.reply(replyBody);
      reply = redditapi.getComment(postedReply.id);
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
    success(comment);
  });
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
