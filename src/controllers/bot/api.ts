import { Request, Response } from "express";
import HttpStatus from "http-status-codes";
import { RegisteredBot } from "../../entity";
import { response } from "../api";

/**
 * Checks if the specified request is authorized
 * @param req The request to evaluate
 * @param res The response
 * @param success The function called if the request is successfully authorized
 */
export async function authorized(
  req: Request,
  res: Response,
  success: Function
) {
  if (
    !req.body ||
    !req.body.auth ||
    !req.body.auth.token ||
    !req.body.auth.botToken
  ) {
    req.log.fatal(`Authentication attempted without authentication tokens`);

    return response(res, {
      status: HttpStatus.UNPROCESSABLE_ENTITY,
      message: "Auth parameters not provided"
    });
  }

  let authToken = req.body.auth.token;

  if (process.env.API_TOKEN !== authToken) {
    req.log.fatal(`Authentication failed with invaild API access token`);

    return response(res, {
      status: HttpStatus.UNAUTHORIZED,
      message: "Invalid access token"
    });
  }

  let botToken = req.body.auth.botToken;

  let bot = await RegisteredBot.findOne({
    where: {
      token: botToken
    }
  });

  if (bot) return success(bot);

  req.log.fatal(`Authentication failed with invalid bot access token`);

  return response(res, {
    status: HttpStatus.UNAUTHORIZED,
    message: "Invalid bot access token"
  });
}
