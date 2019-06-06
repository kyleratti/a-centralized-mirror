import {
  Entity,
  Column,
  CreateDateColumn,
  UpdateDateColumn,
  PrimaryGeneratedColumn,
  OneToMany,
  BaseEntity
} from "typeorm";
import { MirroredVideo } from "./mirroredvideo";

@Entity()
export class RegisteredBot extends BaseEntity {
  @PrimaryGeneratedColumn()
  id: number;

  @Column({
    unique: true,
    nullable: false
  })
  username: string;

  @Column({
    unique: true,
    nullable: false
  })
  developer: string;

  @Column({
    unique: true,
    nullable: false
  })
  token: string;

  @OneToMany(type => MirroredVideo, mirroredvideo => mirroredvideo.bot)
  mirroredVideos: MirroredVideo[];

  @CreateDateColumn()
  createdAt: Date;

  @UpdateDateColumn()
  updatedAt: Date;
}
