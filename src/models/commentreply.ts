import { Table, Model, Column, CreatedAt, UpdatedAt, DeletedAt, HasMany } from 'sequelize-typescript';

import { MirroredVideo } from './mirroredvideo';

export enum CommentStatus {
    AwaitingUpdate = 5,

    Current = 10
}

@Table({
    timestamps: true
})
export class CommentReply extends Model<CommentReply> {
    @Column({
        allowNull: false,
        unique: true

    })
    redditPostId: string;

    @Column({
        allowNull: false,
        unique: false
    })
    status: number;

    @CreatedAt
    createdAt: Date;
 
    @UpdatedAt
    updatedAt: Date;
  
    @DeletedAt
    deletedAt: Date;

    @HasMany(() => MirroredVideo)
    mirroredVideos: MirroredVideo[];
}
