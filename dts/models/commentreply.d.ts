import { Model } from 'sequelize-typescript';
import { MirroredVideo } from './mirroredvideo';
export declare enum CommentStatus {
    AwaitingUpdate = 5,
    Current = 10
}
export declare class CommentReply extends Model<CommentReply> {
    redditPostId: string;
    status: number;
    createdAt: Date;
    updatedAt: Date;
    deletedAt: Date;
    mirroredVideos: MirroredVideo[];
}
