import { Sequelize } from 'sequelize-typescript';
import Bluebird from 'bluebird';

import { CommentReply, MirroredVideo, RegisteredBot } from '../models';

export class Database {
    private dbLocation: String;
    public db: Sequelize;

    constructor(dbLocation: string) {
        let db = new Sequelize({
            database: dbLocation,
            dialect: 'sqlite',
            username: 'root',
            password: '',
            storage: dbLocation,
            logging: true
        });
        db.addModels([CommentReply, MirroredVideo, RegisteredBot]);

        this.dbLocation = dbLocation;
        this.db = db;
    }

    connect() {
        return this.db.authenticate();
    }
}
