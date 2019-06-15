import "reflect-metadata";
export declare var db: import("typeorm").Connection;
export declare class WebServer {
    private app;
    private port;
    constructor();
    start(): Promise<void>;
}
