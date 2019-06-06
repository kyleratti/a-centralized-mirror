import { BaseEntity } from "typeorm";
import { CommentReplyStatus } from "../structures";
export declare class CommentReply extends BaseEntity {
    id: number;
    redditPostId: string;
    status: CommentReplyStatus;
    createdAt: Date;
    updatedAt: Date;
}
