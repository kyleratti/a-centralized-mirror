import { Sequelize } from 'sequelize-typescript';
import Bluebird from 'bluebird';
export declare class Database {
    private dbLocation;
    db: Sequelize;
    constructor(dbLocation: string);
    connect(): Bluebird<void>;
}
