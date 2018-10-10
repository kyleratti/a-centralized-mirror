"use strict";
Object.defineProperty(exports, "__esModule", { value: true });
const sequelize_typescript_1 = require("sequelize-typescript");
const models_1 = require("../models");
class Database {
    constructor(dbLocation) {
        let db = new sequelize_typescript_1.Sequelize({
            database: dbLocation,
            dialect: 'sqlite',
            username: 'root',
            password: '',
            storage: dbLocation,
            logging: true
        });
        db.addModels([models_1.CommentReply, models_1.MirroredVideo, models_1.RegisteredBot]);
        this.dbLocation = dbLocation;
        this.db = db;
    }
    connect() {
        return this.db.authenticate();
    }
}
exports.Database = Database;
//# sourceMappingURL=database.js.map