import {
  Entity,
  Column,
  CreateDateColumn,
  UpdateDateColumn,
  PrimaryGeneratedColumn,
  ManyToOne
} from "typeorm";
import { RegisteredBot } from "./registeredbot";

@Entity()
export class MirroredVideo {
  @PrimaryGeneratedColumn()
  id: number;

  @Column({
    unique: true,
    nullable: false
  })
  url: string;

  @ManyToOne(type => RegisteredBot, bot => bot.mirroredVideos)
  bot: RegisteredBot;

  // TODO: add a belongs-to relationship to the reddit comment

  @CreateDateColumn({ type: "timestamp" })
  createdAt: Date;

  @UpdateDateColumn({ type: "timestamp" })
  updatedAt: Date;
}
