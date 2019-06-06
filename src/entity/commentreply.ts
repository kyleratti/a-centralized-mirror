import {
  BaseEntity,
  Column,
  CreateDateColumn,
  Entity,
  Index,
  PrimaryGeneratedColumn,
  UpdateDateColumn
} from "typeorm";
import { CommentReplyStatus } from "../structures";

@Entity()
export class CommentReply extends BaseEntity {
  @PrimaryGeneratedColumn()
  id: number;

  @Column({
    unique: true,
    nullable: false
  })
  redditPostId: string;

  @Column("int")
  @Index()
  status: CommentReplyStatus;

  @CreateDateColumn()
  createdAt: Date;

  @UpdateDateColumn()
  updatedAt: Date;
}
