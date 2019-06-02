import {
  Entity,
  Column,
  CreateDateColumn,
  UpdateDateColumn,
  PrimaryGeneratedColumn
} from "typeorm";

@Entity()
export class CommentReply {
  @PrimaryGeneratedColumn()
  id: number;

  @Column({
    unique: true,
    nullable: false
  })
  redditPostId: string;

  // TODO: add a has-many relationship to mirrored videos

  @CreateDateColumn({ type: "timestamp" })
  createdAt: Date;

  @UpdateDateColumn({ type: "timestamp" })
  updatedAt: Date;

  @Column({ type: "timestamp", nullable: true })
  deletedAt: Date;
}
