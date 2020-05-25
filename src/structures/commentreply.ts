export enum CommentReplyStatus {
  /** Indicates the comment is current and not in need of updates */
  Current = 0,
  /** Indicates the comment is outdated and needs to be updated */
  Outdated = 1,
  /** Indicates the parent is expired and cannot be updated anymore */
  Expired = 2,
}
