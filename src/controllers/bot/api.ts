import { Request } from "express";
import { isWebUri } from "valid-url";
import { RegisteredBot } from "../../entity";

export function hasAuthHeaders(req: Request) {
  if (
    !req.headers ||
    !req.headers["x-acm-api-token"] ||
    !req.headers["x-acm-bot-token"]
  ) {
    throw `Authentication attempted without authentication tokens`;
  }

  return true;
}

export async function isAuthorized(req: Request) {
  const authToken = req.headers["x-acm-api-token"];
  if (process.env.API_TOKEN !== authToken)
    throw `Authentication failed with invalid API token`;

  const botToken = req.headers["x-acm-bot-token"];
  const bot = await RegisteredBot.findOne({
    where: {
      token: botToken,
    },
  });

  if (!bot) throw `Authentication failed with invalid bot token`;

  return bot;
}

export function isValidRequest(req: Request) {
  const [redditPostId, url] = [req.body.data.redditPostId, req.body.data.url];

  if (!url)
    throw `Missing 'url' key on payload. Please check your request and try again.`;

  if (!isWebUri(url))
    throw `Invalid 'url' key on payload. Please check your request and try again. If this is an error, please open an issue.`;

  if (!redditPostId)
    throw `Missing 'redditPostId' on payload. Please check your request and try again`;

  if (redditPostId.length < 4)
    throw `Invalid 'redditPsotId' payload received. Please check your req uest and try again.`;

  return true;
}
