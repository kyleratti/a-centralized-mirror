import { Router } from "express";
export interface ResponseData {
    /** The HTTP status code to respond with */
    code: number;
    /** The message to respond with */
    message: string;
    /** The data to respond with, if any */
    data?: object;
}
export declare const PublicApi: Router;
