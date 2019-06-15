import { Request, Response } from "express";
/**
 * Checks if the specified request is authorized
 * @param req The request to evaluate
 * @param res The response
 * @param success The function called if the request is successfully authorized
 */
export declare function authorized(req: Request, res: Response, success: Function): Promise<any>;
