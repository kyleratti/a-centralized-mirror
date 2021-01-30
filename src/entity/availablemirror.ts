import {
  AfterInsert,
  AfterRemove,
  AfterUpdate,
  BaseEntity,
  Column,
  CreateDateColumn,
  Entity,
  Index,
  ManyToOne,
  PrimaryGeneratedColumn,
  UpdateDateColumn,
} from "typeorm";
import { CommentReplyStatus, LinkType } from "../structures";
import { CommentReply } from "./commentreply";
import { RegisteredBot } from "./registeredbot";

@Entity()
export class AvailableMirror extends BaseEntity {
  @PrimaryGeneratedColumn()
  id: number;

  @Column({
    unique: true,
    nullable: false,
  })
  mirrorUrl: string;

  @Column()
  @Index()
  redditPostId: string;

  @ManyToOne((type) => RegisteredBot, (bot) => bot.mirroredVideos, {
    eager: true,
  })
  bot: RegisteredBot;

  @Column({
    default: LinkType.Mirror,
  })
  linkType: LinkType;

  @CreateDateColumn()
  createdAt: Date;

  @UpdateDateColumn()
  updatedAt: Date;

  @AfterInsert()
  @AfterUpdate()
  @AfterRemove()
  async updateCommentReplyStatus() {
    try {
      let comment = await CommentReply.findOne({
        where: {
          redditPostId_Parent: this.redditPostId,
        },
      });

      if (comment) {
        comment.status = CommentReplyStatus.Outdated;
        await comment.save();
      } else {
        comment = new CommentReply();
        comment.redditPostId_Parent = this.redditPostId;
        comment.status = CommentReplyStatus.Outdated;
        await comment.save();
      }
    } catch (err) {
      return console.error(`Error on AfterUpdate call: ${err}`);
    }
  }
}
