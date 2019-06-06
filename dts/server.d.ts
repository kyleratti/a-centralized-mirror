import "reflect-metadata";
export declare var db: import("../../../../Documents/GitHub/a-centralized-mirror/node_modules/typeorm/connection/Connection").Connection;
export declare class WebServer {
    private app;
    private port;
    constructor();
    start(): void;
}
