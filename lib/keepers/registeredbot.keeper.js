"use strict";
var __importDefault = (this && this.__importDefault) || function (mod) {
    return (mod && mod.__esModule) ? mod : { "default": mod };
};
Object.defineProperty(exports, "__esModule", { value: true });
const load_json_file_1 = __importDefault(require("load-json-file"));
const models_1 = require("../models");
class RegisteredBotKeeper {
    start() {
        load_json_file_1.default('./config/bots.json')
            .then((data) => {
            if (!data)
                return;
            if (data.goodBots) {
                data.goodBots.forEach(botData => {
                    models_1.RegisteredBot.insertOrUpdate({
                        username: botData.username,
                        developer: botData.developer,
                        token: botData.token
                    });
                    console.log(`updated bot ${botData.username} by /u/${botData.developer}`);
                });
            }
            if (data.badBots) {
                data.badBots.forEach(botData => {
                    models_1.RegisteredBot.destroy({ where: {
                            username: botData.username
                        }
                    })
                        .then(() => {
                        console.log(`successfully removed bad bot ${botData.username} for ${botData.reason}`);
                    })
                        .catch((err) => {
                        console.error(`failed removing bad bot '${botData.username}': ${err}`);
                    });
                });
            }
        })
            .catch(err => {
            console.error(`failed loading bots.json: ${err}`);
            console.log(`error during bots.json import, using last known good config`);
        });
    }
}
exports.RegisteredBotKeeper = RegisteredBotKeeper;
//# sourceMappingURL=registeredbot.keeper.js.map