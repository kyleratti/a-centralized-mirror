import {
  BaseEntity,
  Column,
  CreateDateColumn,
  Entity,
  OneToMany,
  PrimaryGeneratedColumn,
  UpdateDateColumn,
} from "typeorm";
import { AvailableMirror } from "./availablemirror";

@Entity()
export class RegisteredBot extends BaseEntity {
  @PrimaryGeneratedColumn()
  id: number;

  @Column({
    unique: true,
    nullable: false,
  })
  username: string;

  @Column({
    unique: true,
    nullable: false,
  })
  developer: string;

  @Column({
    unique: true,
    nullable: false,
  })
  token: string;

  @OneToMany((type) => AvailableMirror, (mirroredvideo) => mirroredvideo.bot)
  mirroredVideos: AvailableMirror[];

  @CreateDateColumn()
  createdAt: Date;

  @UpdateDateColumn()
  updatedAt: Date;
}
