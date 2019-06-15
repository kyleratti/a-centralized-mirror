import { BaseEntity } from "typeorm";
import { CommentReplyStatus } from "../structures";
export declare class CommentReply extends BaseEntity {
    id: number;
    /** The reddit post ID */
    redditPostId_Parent: string;
    /** The reddit post ID of the comment posted and tracked by the mirror bot, if any */
    redditPostId_Reply: string;
    status: CommentReplyStatus;
    createdAt: Date;
    updatedAt: Date;
}
