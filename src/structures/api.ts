import { RegisteredBot } from "../entity";
import { LinkType } from "./availablemirror";

/** Data structure for API responses */
export interface ResponseData {
  /** The HTTP status code to respond with */
  status: number;

  /** The message to respond with */
  message: string;

  /** The data to respond with, if any */
  data?: object;
}

export interface CreateMirrorRequest {
  /** The unique ID to the reddit post */
  redditPostId: string;

  /** The URL to the mirrored video */
  url: string;

  /** The bot hosting the mirror */
  bot: RegisteredBot;

  /** The type of link */
  linkType: LinkType;
}

/** Data structure for video API requests */
export interface ApiRequest {
  /** The unique ID to the reddit post */
  redditPostId: string;

  /** The URL to the mirrored video */
  url: string;

  /** The type of link */
  linkType?: LinkType;
}

/** Data structure for delete requests */
export interface DeleteRequest extends ApiRequest {}

/** Data structure for update requests */
export interface UpdateRequest extends ApiRequest {}
