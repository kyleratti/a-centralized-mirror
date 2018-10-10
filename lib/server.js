"use strict";
var __importDefault = (this && this.__importDefault) || function (mod) {
    return (mod && mod.__esModule) ? mod : { "default": mod };
};
Object.defineProperty(exports, "__esModule", { value: true });
const express_1 = __importDefault(require("express"));
const body_parser_1 = __importDefault(require("body-parser"));
const database_1 = require("./db/database");
const controllers_1 = require("./controllers");
const keepers_1 = require("./keepers");
exports.database = new database_1.Database(process.env.DATABASE_LOCATION);
class WebServer {
    constructor() {
        let app = express_1.default();
        let port = Number(process.env.PORT) || 3010;
        app.use(body_parser_1.default.urlencoded({ extended: true }));
        app.use(body_parser_1.default.json());
        app.use('/', controllers_1.ApiController);
        this.app = app;
        this.port = port;
    }
    start() {
        exports.database.connect()
            .then(() => {
            exports.database.db.sync()
                .then(() => {
                console.log(`database successfully started and synchronized`);
                console.log(`importing bots config`);
                let botKeeper = new keepers_1.RegisteredBotKeeper();
                botKeeper.start();
            })
                .catch((err) => {
                console.error(`unable to synchronize database: ${err}`);
            });
        })
            .catch((err) => {
            console.error(`failed to load database: ${err}`);
        });
        this.app.listen(this.port, () => {
            console.log(`listening for centralized api requests at http://127.0.0.1:${this.port}`);
        });
    }
}
exports.WebServer = WebServer;
//# sourceMappingURL=server.js.map