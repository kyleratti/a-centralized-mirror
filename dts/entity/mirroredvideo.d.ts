import { BaseEntity } from "typeorm";
import { RegisteredBot } from "./registeredbot";
export declare class MirroredVideo extends BaseEntity {
    id: number;
    url: string;
    redditPostId: string;
    bot: RegisteredBot;
    createdAt: Date;
    updatedAt: Date;
    updateCommentReplyStatus(): Promise<void>;
}
