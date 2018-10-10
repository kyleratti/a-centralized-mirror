import { Model } from 'sequelize-typescript';
import { MirroredVideo } from '.';
export declare class RegisteredBot extends Model<RegisteredBot> {
    username: string;
    developer: string;
    token: string;
    mirroredVideos: MirroredVideo[];
    createdAt: Date;
    updatedAt: Date;
    deletedAt: Date;
}
