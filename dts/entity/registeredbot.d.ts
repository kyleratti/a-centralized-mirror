import { BaseEntity } from "typeorm";
import { AvailableMirror } from "./availablemirror";
export declare class RegisteredBot extends BaseEntity {
    id: number;
    username: string;
    developer: string;
    token: string;
    mirroredVideos: AvailableMirror[];
    createdAt: Date;
    updatedAt: Date;
}
