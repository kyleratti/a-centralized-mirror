import { Database } from './db/database';
export declare var database: Database;
export declare class WebServer {
    private app;
    private port;
    constructor();
    start(): void;
}
