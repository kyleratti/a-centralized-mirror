import "reflect-metadata";
import express from "express";
import bodyParser from "body-parser";
import { database } from "./db";
import { ApiController } from "./controllers";

export var db = database;

export class WebServer {
  private app: express.Application;
  private port: number;

  constructor() {
    let app = express();
    let port = Number(process.env.PORT) || 3010;

    app.use(bodyParser.urlencoded({ extended: true }));
    app.use(bodyParser.json());

    app.use("/", ApiController);

    this.app = app;
    this.port = port;
  }

  start() {
    db.connect()
      .then(_conn => {
        console.log(`database successfully started`);
      })
      .catch(err => {
        console.error(`unable to start database: ${err}`);
      });

    this.app.listen(this.port, () => {
      console.log(
        `listening for centralized api requests at http://127.0.0.1:${
          this.port
        }`
      );
    });
  }
}
