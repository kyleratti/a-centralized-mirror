import { Table, Model, Column, CreatedAt, UpdatedAt, DeletedAt, HasMany } from 'sequelize-typescript';
import { MirroredVideo } from '.';

@Table({
    timestamps: true
})
export class RegisteredBot extends Model<RegisteredBot> {
    @Column({
        allowNull: false,
        unique: true
    })
    username: string;

    @Column({
        allowNull: false,
        unique: false,
    })
    developer: string;

    @Column({
        allowNull: false,
        unique: true
    })
    token: string;

    @HasMany(() => MirroredVideo)
    mirroredVideos: MirroredVideo[];

    @CreatedAt
    createdAt: Date;
 
    @UpdatedAt
    updatedAt: Date;
  
    @DeletedAt
    deletedAt: Date;
}
