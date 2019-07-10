import { Request, Response, Router } from "express";
import HttpStatus from "http-status-codes";
import { authorized } from ".";
import { response } from "..";
import { RegisteredBot } from "../../entity";

const router: Router = Router();

router.get("/get", async (req: Request, res: Response) => {
  authorized(req, res, async () => {
    let reqData = req.body.data;
    let username = reqData.username;
    let bot;

    try {
      bot = await RegisteredBot.findOne({ username: username });
    } catch (err) {
      req.log.fatal({
        msg: `Error locating bot`,
        error: err
      });

      return response(res, {
        status: HttpStatus.INTERNAL_SERVER_ERROR,
        message: `Error retrieving bot information`
      });
    }

    if (bot)
      return response(res, {
        status: HttpStatus.OK,
        message: `OK`,
        data: {
          username: bot.username,
          developer: bot.developer,
          token: bot.token
        }
      });
    else
      return response(res, {
        status: HttpStatus.NOT_FOUND,
        message: `Bot not found`
      });
  });
});

router.get("/getall", async (req: Request, res: Response) => {
  authorized(req, res, async () => {
    let bots;

    try {
      bots = await RegisteredBot.find({
        order: {
          username: "ASC"
        }
      });
    } catch (err) {
      req.log.fatal({
        msg: `Error locating bot`,
        error: err
      });

      return response(res, {
        status: HttpStatus.INTERNAL_SERVER_ERROR,
        message: `Error retrieving information for bots`
      });
    }

    let bot_data = [];

    bots.forEach(bot => {
      bot_data.push({
        id: bot.id,
        username: bot.username,
        developer: bot.developer,
        token: bot.token,
        createdAt: bot.createdAt,
        updatedAt: bot.updatedAt
      });
    });

    return response(res, {
      status: HttpStatus.OK,
      message: `OK`,
      data: {
        count: bot_data.length,
        bots: bot_data
      }
    });
  });
});

router.put("/add", (req: Request, res) => {
  authorized(req, res, async () => {
    let reqData = req.body.data;
    let username = reqData.username;
    let developer = reqData.developer;
    let token = reqData.token;
    let bot;

    try {
      bot = await RegisteredBot.findOne({ username: username });
    } catch (err) {
      req.log.fatal({
        msg: `Error locating bot`,
        error: err
      });

      return response(res, {
        status: HttpStatus.INTERNAL_SERVER_ERROR,
        message: `Error retrieving bot information`
      });
    }

    if (bot)
      return response(res, {
        status: HttpStatus.BAD_REQUEST,
        message: `Bot is already registered; please issue an update instead`
      });

    let newBot = new RegisteredBot();
    newBot.username = username;
    newBot.developer = developer;
    newBot.token = token;

    try {
      await newBot.save();

      return response(res, {
        status: HttpStatus.OK,
        message: `Successfully created new bot`,
        data: {
          username: username,
          developer: developer
        }
      });
    } catch (err) {
      req.log.fatal({
        msg: `Error locating bot`,
        error: err
      });

      return response(res, {
        status: HttpStatus.INTERNAL_SERVER_ERROR,
        message: `Error creating bot`
      });
    }
  });
});

router.post("/update", (req: Request, res) => {
  authorized(req, res, async () => {
    let reqData = req.body.data;
    let reqUsername = reqData.username;
    let reqDeveloper = reqData.developer;
    let reqToken = reqData.token;
    let bot;

    try {
      bot = await RegisteredBot.findOne({ username: reqUsername });
    } catch (err) {
      req.log.fatal({
        msg: `Error locating bot`,
        error: err
      });

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
        message: `Error saving updated bot`
      });
    }

    return response(res, {
      status: HttpStatus.OK,
      message: messages.join(", "),
      data: data
    });
  });
});

router.delete("/delete", (req: Request, res) => {
  authorized(req, res, async () => {
    let reqData = req.body.data;
    let username = reqData.username;
    let bot;

    try {
      bot = await RegisteredBot.findOne({ username: username });
    } catch (err) {
      req.log.fatal({
        msg: `Error locating bot`,
        error: err
      });

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

    try {
      bot.remove();
    } catch (err) {
      return response(err, {
        status: HttpStatus.INTERNAL_SERVER_ERROR,
        message: `Error deleting bot`
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
});

export const BotsAdminApi: Router = router;
