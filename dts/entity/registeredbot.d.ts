import { MirroredVideo } from "./mirroredvideo";
export declare class RegisteredBot {
    id: number;
    username: string;
    developer: string;
    token: string;
    mirroredVideos: MirroredVideo[];
    createdAt: Date;
    updatedAt: Date;
    deletedAt: Date;
}
