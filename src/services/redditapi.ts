import snoowrap from "snoowrap";
import * as configuration from "../configuration";

export const redditapi = new snoowrap({
  userAgent: "a-centralized-mirror",
  clientId: configuration.reddit.clientId,
  clientSecret: configuration.reddit.clientSecret,
  username: configuration.reddit.username,
  password: configuration.reddit.password,
});
