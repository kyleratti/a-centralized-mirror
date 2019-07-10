import { Request, Response } from "express";
import HttpStatus from "http-status-codes";
import { response } from "..";

/**
 * Checks if the specified request is authorized
 * @param req The request to evaluate
 * @param res The response
 * @param success The function called if the request is successfully authorized
 */
export function authorized(req: Request, res: Response, success: Function) {
  if (!req.headers["x-acm-admin-token"]) {
    req.log.error(`Authentication attempted without authentication tokens`);

    return response(res, {
      status: HttpStatus.UNPROCESSABLE_ENTITY,
      message: "Auth parameters not provided"
    });
  }

  if (req.headers["x-acm-admin-token"] != process.env.API_ADMIN_TOKEN) {
    req.log.error(`Authentication failed`);

    return response(res, {
      status: HttpStatus.UNAUTHORIZED,
      message: "Invalid credentials"
    });
  }

  req.log.debug(`Received valid admin authentication`);

  success();
}
