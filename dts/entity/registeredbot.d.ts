import { BaseEntity } from "typeorm";
import { MirroredVideo } from "./mirroredvideo";
export declare class RegisteredBot extends BaseEntity {
    id: number;
    username: string;
    developer: string;
    token: string;
    mirroredVideos: MirroredVideo[];
    createdAt: Date;
    updatedAt: Date;
}
