import express from 'express';
import bodyParser from 'body-parser';
import { Database } from './db/database';
import { ApiController } from './controllers';
import { RegisteredBotKeeper } from './keepers';

export var database = new Database(process.env.DATABASE_LOCATION);

export class WebServer {
    private app: express.Application;
    private port: number;

    constructor() {
        let app = express();
        let port = Number(process.env.PORT) || 3010;

        app.use(bodyParser.urlencoded({ extended: true }));
        app.use(bodyParser.json());

        app.use('/', ApiController);

        this.app = app;
        this.port = port;
    }

    start() {
        database.connect()
            .then(() => {
                database.db.sync()
                    .then(() => {
                        console.log(`database successfully started and synchronized`);
                        console.log(`importing bots config`);

                        let botKeeper = new RegisteredBotKeeper();
                        botKeeper.start();
                    })
                    .catch((err) => {
                        console.error(`unable to synchronize database: ${err}`)
                    });
            })
            .catch((err) => {
                console.error(`failed to load database: ${err}`);
            });
        
        this.app.listen(this.port, () => {
            console.log(`listening for centralized api requests at http://127.0.0.1:${this.port}`);
        })
    }
}
