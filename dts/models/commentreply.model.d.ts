import { Model } from 'sequelize-typescript';
import { MirroredVideo } from './mirroredvideo';
export declare class CommentReply extends Model<CommentReply> {
    redditPostId: string;
    createdAt: Date;
    updatedAt: Date;
    deletedAt: Date;
    mirroredVideos: MirroredVideo[];
}
