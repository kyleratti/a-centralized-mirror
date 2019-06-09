import { Request, Response } from "express";
import HttpStatus from "http-status-codes";
import { response } from "..";

/**
 * Checks if the specified request is authorized
 * @param req The request to evaluate

 */
export function authorized(req: Request, res: Response, success: Function) {
  if (
    !process.env.API_ADMIN_IP ||
    !req.headers["cf-connecting-ip"] ||
    req.headers["cf-connecting-ip"] !== process.env.API_ADMIN_IP
  ) {
    req.log.error(`Authentication attempted from non-Cloudflare IP address`);

    return response(res, {
      status: HttpStatus.UNAUTHORIZED,
      message: "Authentication not permitted"
    });
  }

  if (!req.body || !req.body.auth || !req.body.auth.adminToken) {
    req.log.error(`Authentication attempted without authentication tokens`);

    return response(res, {
      status: HttpStatus.UNAUTHORIZED,
      message: "Auth parameters not provided"
    });
  }

  req.log.debug(`Received valid admin authentication`);

  success();
}
