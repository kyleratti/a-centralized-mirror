export enum CommentReplyStatus {
  /** Indicates the comment is current and not in need of updates */
  Current,
  /** Indicates the comment is outdated and needs to be updated */
  Outdated,
  /** Indicates the parent is expired and cannot be updated anymore */
  Expired,
}
