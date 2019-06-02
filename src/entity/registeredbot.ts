import {
  Entity,
  Column,
  CreateDateColumn,
  UpdateDateColumn,
  PrimaryGeneratedColumn,
  OneToMany
} from "typeorm";
import { MirroredVideo } from "./mirroredvideo";

@Entity()
export class RegisteredBot {
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

  @CreateDateColumn({ type: "timestamp" })
  createdAt: Date;

  @UpdateDateColumn({ type: "timestamp" })
  updatedAt: Date;

  @Column({ type: "timestamp", nullable: true })
  deletedAt: Date;

  // TODO: add a has-many relationship to mirrored videos
}
