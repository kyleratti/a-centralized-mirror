import { Request, Response, Router } from "express";
import HttpStatus from "http-status-codes";
import { response } from "..";
import { MirroredVideo, RegisteredBot, CommentReply } from "../../entity";
import { authorized } from ".";

const router: Router = Router();

router.get("/get", async (req: Request, res: Response) => {
  authorized(req, res)
    .then(async () => {
      let reqData = req.body.data;
      let reqUrl = reqData.reqUrl;
      let mirroredVideo = await MirroredVideo.findOne({ url: reqUrl });

      if (mirroredVideo)
        return response(res, {
          status: HttpStatus.OK,
          message: `OK`,
          data: {
            id: mirroredVideo.id,
            url: mirroredVideo.url,
            botUsername: mirroredVideo.bot.username,
            createdAt: mirroredVideo.createdAt,
            updatedAt: mirroredVideo.updatedAt
          }
        });
    })
    .catch(err => {
      return response(res, {
        status: HttpStatus.NOT_FOUND,
        message: `Mirrored video not found`,
        data: {
          error: err
        }
      });
    });
});

router.get("/getall", async (req: Request, res: Response) => {
  authorized(req, res)
    .then(async () => {
      let comments = await CommentReply.find({
        order: {
          createdAt: "ASC"
        }
      });
      let commentsData = [];

      comments.forEach(comment => {
        commentsData.push({
          id: comment.id,
          redditPostId: comment.redditPostId,
          status: comment.status,
          createdAt: comment.createdAt,
          updatedAt: comment.updatedAt
        });
      });

      return response(res, {
        status: HttpStatus.OK,
        message: `OK`,
        data: {
          commentReplies: commentsData
        }
      });
    })
    .catch(err => {
      return response(res, {
        status: HttpStatus.NOT_FOUND,
        message: `No comment replies found`,
        data: {
          error: err
        }
      });
    });
});

router.put("/add", async (req: Request, res) => {
  authorized(req, res)
    .then(async () => {
      let reqData = req.body.data;
      let reqUrl = reqData.url;
      let reqBotUsername = reqData.botUsername;
      // TODO: reddit comment ID

      let bot = await RegisteredBot.findOne({ username: reqBotUsername });

      let mirroredVideo = new MirroredVideo();
      mirroredVideo.url = reqUrl;
      mirroredVideo.bot = bot;
      // TODO: reddit comment ID
      mirroredVideo.save();

      return response(res, {
        status: HttpStatus.OK,
        message: `Successfully created new mirrored video`,
        data: {
          url: reqUrl,
          botUsername: bot.username
          // TODO: reddit comment ID
        }
      });
    })
    .catch(err => {
      return response(res, {
        status: HttpStatus.NOT_FOUND,
        message: `Mirrored video not found`,
        data: {
          error: err
        }
      });
    });
});

router.post("/update", (req: Request, res) => {
  authorized(req, res).then(async () => {
    let reqData = req.body.data;
    let reqUsername = reqData.username;
    let reqDeveloper = reqData.developer;
    let reqToken = reqData.token;
    let bot = await RegisteredBot.findOne({ username: reqUsername });
    let username = bot.username;

    let messages = [];
    let data = {};

    if (!bot)
      return response(res, {
        status: HttpStatus.BAD_REQUEST,
        message: `Bot does not exist`,
        data: {
          username: username
        }
      });

    if (reqDeveloper) {
      bot.developer = reqDeveloper;
      messages.push(`Updated developer`);
      data["newDeveloper"] = reqDeveloper;
    }

    if (reqToken) {
      bot.token = reqToken;
      messages.push(`Updated token`);
      data["newToken"] = reqToken;
    }

    bot
      .save()
      .then(() => {
        return response(res, {
          status: HttpStatus.OK,
          message: messages.join(", "),
          data: data
        });
      })
      .catch(err => {
        return response(res, {
          status: HttpStatus.INTERNAL_SERVER_ERROR,
          message: `Error saving changes to bot`,
          data: {
            username: username,
            error: err
          }
        });
      });
  });
});

router.delete("/delete", (req: Request, res) => {
  authorized(req, res).then(async () => {
    let reqData = req.body.data;
    let username = reqData.username;
    let bot = await RegisteredBot.findOne({ username: username });

    if (!bot)
      return response(res, {
        status: HttpStatus.BAD_REQUEST,
        message: `Bot does not exist`,
        data: {
          username: username
        }
      });

    bot
      .remove()
      .then(() => {
        return response(res, {
          status: HttpStatus.OK,
          message: "Successfully removed bot",
          data: {
            username: username
          }
        });
      })
      .catch(err => {
        return response(res, {
          status: HttpStatus.INTERNAL_SERVER_ERROR,
          message: "Error removing bot",
          data: {
            username: username,
            error: err
          }
        });
      });
  });
});

export const CommentReplyAdminApi: Router = router;
