import { Router, Request, Response } from "express";

/**
 * Checks if the specified request is authorized
 * @param req The request to evaluate

 */
export function authorized(req: Request, res: Response) {
  return new Promise((success, _fail) => {
    if (
      !process.env.API_ADMIN_IP ||
      !req.headers["cf-connecting-ip"] ||
      req.headers["cf-connecting-ip"] !== process.env.API_ADMIN_IP
    )
      return response(res, {
        status: HttpStatus.UNAUTHORIZED,
        message: "Authentication not permitted"
      });

    if (!req.body || !req.body.auth || !req.body.auth.adminToken)
      return response(res, {
        status: HttpStatus.UNAUTHORIZED,
        message: "Auth parameters not provided"
      });

    success();
  });
}
