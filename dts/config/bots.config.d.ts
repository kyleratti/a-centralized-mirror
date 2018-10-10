export interface GoodBotConfig {
    /** Reddit username of the bot */
    username: string;
    /** Reddit username of the bot developer */
    developer: string;
    /** Unique bot authentication token */
    token: string;
}
export interface BadBotConfig {
    /** Reddit username of the bot */
    username: string;
    /** The reason the bot is having its access revoked */
    reason: string;
}
export interface BotsConfig {
    /** Good bots that should be created/updated */
    goodBots: Array<GoodBotConfig>;
    /** Bad bots that should have their access revoked */
    badBots: Array<BadBotConfig>;
}
