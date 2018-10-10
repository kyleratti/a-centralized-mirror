import { Model } from 'sequelize-typescript';
import { CommentReply, RegisteredBot } from '.';
export declare class MirroredVideo extends Model<MirroredVideo> {
    url: string;
    commentId: number;
    comment: CommentReply;
    botId: number;
    bot: RegisteredBot;
    createdAt: Date;
    updatedAt: Date;
    deletedAt: Date;
}
