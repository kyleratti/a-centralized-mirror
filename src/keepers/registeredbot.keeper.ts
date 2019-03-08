import loadJsonFile from 'load-json-file';

import { BotsConfig } from '../config';
import { Keeper } from '.';
import { RegisteredBot } from '../models';

export class RegisteredBotKeeper implements Keeper {
    start() {
        loadJsonFile('./config/bots.json')  
            .then((data: BotsConfig) => {
                if(!data) return;

                if(data.goodBots) {
                    data.goodBots.forEach(botData => {
                        RegisteredBot.insertOrUpdate({
                            username: botData.username,
                            developer: botData.developer,
                            token: botData.token
                        });

                        console.log(`updated bot ${botData.username} by /u/${botData.developer}`);
                    });
                }

                if(data.badBots) {
                    data.badBots.forEach(botData => {
                        RegisteredBot.destroy({ where: {
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
                console.log(`error during bots.json import, using last known good config, if any`);
            });
    }
}
