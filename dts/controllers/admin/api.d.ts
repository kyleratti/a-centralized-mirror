import { Request, Response } from "express";
/**
 * Checks if the specified request is authorized
 * @param req The request to evaluate

 */
export declare function authorized(req: Request, res: Response): Promise<{}>;
