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

function processCommentUpdates(
  req: Request,
  res: Response,
  comment: CommentReply
) {
  return new Promise<CommentReply>(async () => {
    let post: Submission;

    try {
      // @ts-ignore
      // FIXME: due to an issue with snoowrap typings, the 'await' keyword causes compile errors. see https://github.com/DefinitelyTyped/DefinitelyTyped/issues/33139
      post = await redditapi.getSubmission(comment.redditPostId_Parent).fetch();
    } catch (err) {
      req.log.fatal(err);

      return response(res, {
        status: HttpStatus.INTERNAL_SERVER_ERROR,
        message: `Error retrieving post from reddit`
      });
    }
    let mirrors: AvailableMirror[];

    try {
      mirrors = await AvailableMirror.find({
        where: {
          redditPostId: comment.redditPostId_Parent
        },
        order: {
          createdAt: "ASC"
        }
      });
    } catch (err) {
      req.log.fatal(`Error retrieving mirrored videos for post`);

      return response(res, {
        status: HttpStatus.INTERNAL_SERVER_ERROR,
        message: `Error retrieving mirrored videos for post`
      });
    }
    let formattedMirrors: string[] = [];

    for (let i = 0; i < mirrors.length; i++) {
      const mirror = mirrors[i];

      let mirrorUrl = mirror.mirrorUrl;
      let botUsername = mirror.bot.username;

      formattedMirrors.push(
        `* [Mirror #${i + 1}](${mirrorUrl}) (provided by /u/${botUsername})`
      );
    }
    let replyBody = TEMPLATE_COMMENTREPLY.replace(
      "%s",
      formattedMirrors.join("\n")
    );
    let reply: Submission;
    if (comment.redditPostId_Reply) {
      // @ts-ignore
      // FIXME: due to an issue with snoowrap typings, the 'await' keyword causes compile errors. see https://github.com/DefinitelyTyped/DefinitelyTyped/issues/33139
      reply = await redditapi.getComment(comment.redditPostId_Reply).fetch();
    } else {
      // @ts-ignore
      // FIXME: due to an issue with snoowrap typings, the 'await' keyword causes compile errors. see https://github.com/DefinitelyTyped/DefinitelyTyped/issues/33139
      let postedReply: Submission = await post.reply(replyBody);
      // @ts-ignore
      // FIXME: due to an issue with snoowrap typings, the 'await' keyword causes compile errors. see https://github.com/DefinitelyTyped/DefinitelyTyped/issues/33139
      reply = await redditapi.getComment(postedReply.id).fetch();
    }

    if (await isSubredditMod(reply.subreddit)) {
      // @ts-ignore
      // FIXME: due to an issue with snoowrap typings, the 'await' keyword causes compile errors. see https://github.com/DefinitelyTyped/DefinitelyTyped/issues/33139
      await reply.distinguish({
        status: true,
        sticky: true
      });

      // TODO: sticky post (ONLY IF ANOTHER POST IS NOT STICKIED)
      // FIXME: this doesn't actually check if the post has a sticky first
    }

    comment.redditPostId_Reply = reply.id;
    comment.status = CommentReplyStatus.Current;
    await comment.save();
    return comment;
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

    await comments.forEach(async comment => {
      await processCommentUpdates(req, res, comment);
    });

    let numPostsUpdated = comments.length;

    // FIXME: this returns before line 142 runs
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
