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

  // FIXME: this and redditPostId_Reply should be done better and I am ashamed
  /** The reddit post ID */
  @Column({
    unique: true,
    nullable: false
  })
  redditPostId_Parent: string;

  /** The reddit post ID of the comment posted and tracked by the mirror bot, if any */
  @Column({
    unique: true,
    nullable: true
  })
  redditPostId_Reply: string;

  @Column("int")
  @Index()
  status: CommentReplyStatus;

  @CreateDateColumn()
  createdAt: Date;

  @UpdateDateColumn()
  updatedAt: Date;
}
