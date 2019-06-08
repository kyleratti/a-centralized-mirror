import { Request, Response, Router } from "express";
import HttpStatus from "http-status-codes";
import { authorized } from ".";
import { response } from "..";
import { MirroredVideo, RegisteredBot } from "../../entity";
import { EventListenerTypes } from "typeorm/metadata/types/EventListenerTypes";

const router: Router = Router();

router.get("/get", async (req: Request, res: Response) => {
  authorized(req, res, async () => {
    let reqData = req.body.data;
    let reqUrl = reqData.reqUrl;
    let mirroredVideo;

    try {
      mirroredVideo = await MirroredVideo.findOne({ url: reqUrl });
    } catch (err) {
      return response(res, {
        status: HttpStatus.INTERNAL_SERVER_ERROR,
        message: `Error retrieving mirrored video`
      });
    }

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
    else
      return response(res, {
        status: HttpStatus.NOT_FOUND,
        message: `Mirrored video not found`
      });
  });
});

router.get("/getall", async (req: Request, res: Response) => {
  authorized(req, res, async () => {
    let mirroredVideos;

    try {
      mirroredVideos = await MirroredVideo.find({
        order: {
          createdAt: "ASC"
        }
      });
    } catch (err) {
      return response(res, {
        status: HttpStatus.INTERNAL_SERVER_ERROR,
        message: `Error retriving mirrored videos information`
      });
    }

    let mirroredVideoData = [];

    mirroredVideos.forEach(mirroredVideo => {
      mirroredVideoData.push({
        id: mirroredVideo.id,
        url: mirroredVideo.url,
        botUsername: mirroredVideo.bot.username,
        createdAt: mirroredVideo.createdAt,
        updatedAt: mirroredVideo.updatedAt
      });
    });

    return response(res, {
      status: HttpStatus.OK,
      message: `OK`,
      data: {
        mirroredVideos: mirroredVideoData
      }
    });
  });
});

router.put("/add", async (req: Request, res) => {
  authorized(req, res, async () => {
    let reqData = req.body.data;
    let reqUrl = reqData.url;
    let reqBotUsername = reqData.botUsername;

    let bot;

    try {
      bot = await RegisteredBot.findOne({ username: reqBotUsername });
    } catch (err) {
      return response(res, {
        status: HttpStatus.INTERNAL_SERVER_ERROR,
        message: `Error retrieving bot information`
      });
    }

    let mirroredVideo = new MirroredVideo();
    mirroredVideo.url = reqUrl;
    mirroredVideo.bot = bot;

    try {
      mirroredVideo.save();
    } catch (err) {
      return response(res, {
        status: HttpStatus.INTERNAL_SERVER_ERROR,
        message: `Error creating mirrored video`
      });
    }

    return response(res, {
      status: HttpStatus.OK,
      message: `Successfully created new mirrored video`,
      data: {
        url: reqUrl,
        botUsername: bot.username
      }
    });
  });
});

router.post("/update", async (req: Request, res) => {
  let reqData = req.body.data;
  let reqUsername = reqData.username;
  let reqDeveloper = reqData.developer;
  let reqToken = reqData.token;
  let bot;

  try {
    bot = await RegisteredBot.findOne({ username: reqUsername });
  } catch (err) {
    return response(res, {
      status: HttpStatus.INTERNAL_SERVER_ERROR,
      message: `Error retrieving bot information`
    });
  }

  if (!bot)
    return response(res, {
      status: HttpStatus.BAD_REQUEST,
      message: `Bot does not exist`
    });

  let username = bot.username;

  let messages = [];
  let data = {};

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

  try {
    bot.save();
  } catch (err) {
    return response(res, {
      status: HttpStatus.INTERNAL_SERVER_ERROR,
      message: `Error saving updated mirrored video`
    });
  }

  return response(res, {
    status: HttpStatus.OK,
    message: messages.join(", "),
    data: data
  });
});

router.delete("/delete", async (req: Request, res) => {
  let reqData = req.body.data;
  let username = reqData.username;
  let bot;

  try {
    bot = await RegisteredBot.findOne({ username: username });
  } catch (err) {
    return response(res, {
      status: HttpStatus.INTERNAL_SERVER_ERROR,
      message: `Error retrieving bot information`
    });
  }

  if (!bot)
    return response(res, {
      status: HttpStatus.BAD_REQUEST,
      message: `Bot does not exist`,
      data: {
        username: username
      }
    });

  try {
    bot.remove();
  } catch (err) {
    return response(res, {
      status: HttpStatus.INTERNAL_SERVER_ERROR,
      message: `Error removing bot`
    });
  }
  return response(res, {
    status: HttpStatus.OK,
    message: "Successfully removed bot",
    data: {
      username: username
    }
  });
});

export const MirroredVideosAdminApi: Router = router;
