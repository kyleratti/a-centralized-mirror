import { Table, Model, Column, CreatedAt, UpdatedAt, DeletedAt, BelongsTo, ForeignKey } from 'sequelize-typescript';
import { CommentReply, RegisteredBot } from '.';

@Table({
    timestamps: true
})
export class MirroredVideo extends Model<MirroredVideo> {
    @Column({
        allowNull: false,
        unique: false
    })
    url: string;

    @ForeignKey(() => CommentReply)
    commentId: number;

    @BelongsTo(() => CommentReply)
    comment: CommentReply;

    @ForeignKey(() => RegisteredBot)
    botId: number;

    @BelongsTo(() => RegisteredBot)
    bot: RegisteredBot;

    @CreatedAt
    createdAt: Date;
 
    @UpdatedAt
    updatedAt: Date;
  
    @DeletedAt
    deletedAt: Date;
}
