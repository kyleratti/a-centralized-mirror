import { Request, Response } from "express";
import HttpStatus from "http-status-codes";
import { response } from "../api";

/**
 * Checks if the specified request is authorized
 * @param req The request to evaluate
 * @param res The response
 * @param success The function called if the request is successfully authorized
 */
export function authorized(req: Request, res: Response, success: Function) {
  if (!req.headers["x-acm-cron-token"]) {
    req.log.error(`Authentication attempted without authentication tokens`);

    return response(res, {
      status: HttpStatus.UNPROCESSABLE_ENTITY,
      message: "Auth parameters not provided"
    });
  }

  if (true) {
    req.log.error("Attempted unimplemented cron authentication");

    return response(res, {
      status: HttpStatus.NOT_IMPLEMENTED,
      message: "Functionality not supported yet"
    });
  }

  req.log.debug(`Received valid crontab authentication`);

  success();
}
