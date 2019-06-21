import bodyParser from "body-parser";
import { config } from "dotenv";
import express from "express";
import expressPinoLogger from "express-pino-logger";
import pino from "pino";
import "reflect-metadata";
import {
  BotsAdminApi,
  CommentReplyAdminApi,
  MirroredVideosAdminApi
} from "./controllers/admin";
import { BotApi } from "./controllers/bot";
import { CommentReplyCronApi } from "./controllers/cron";
import { database } from "./db";

config();

export var db = database;
const logger = pino({
  name: "a-centralized-mirror",
  level: "debug"
});

export class WebServer {
  private app: express.Application;
  private port: number;

  constructor() {
    let app = express();
    let port = Number(process.env.PORT) || 3010;

    app.use(expressPinoLogger({ logger: logger, level: "debug" }));

    app.use(bodyParser.urlencoded({ extended: true }));
    app.use(bodyParser.json());

    app.use("/admin/bots", BotsAdminApi);
    app.use("/admin/commentreplies", CommentReplyAdminApi);
    app.use("/admin/mirroredvideos", MirroredVideosAdminApi);

    app.use("/cron/commentreply", CommentReplyCronApi);

    app.use("/mirroredvideos", BotApi);

    this.app = app;
    this.port = port;
  }

  async start() {
    try {
      await db.connect();

      logger.info(`database sucecssfully started`);
    } catch (err) {
      return logger.fatal({
        msg: `unable to start database`,
        err: err
      });
    }

    this.app.listen(this.port, "localhost", () => {
      console.log(
        `listening for centralized api requests at http://127.0.0.1:${
          this.port
        }`
      );
    });
  }
}
