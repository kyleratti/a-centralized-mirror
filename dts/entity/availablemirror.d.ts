import { BaseEntity } from "typeorm";
import { RegisteredBot } from "./registeredbot";
export declare class AvailableMirror extends BaseEntity {
    id: number;
    mirrorUrl: string;
    redditPostId: string;
    bot: RegisteredBot;
    createdAt: Date;
    updatedAt: Date;
    updateCommentReplyStatus(): Promise<void>;
}
