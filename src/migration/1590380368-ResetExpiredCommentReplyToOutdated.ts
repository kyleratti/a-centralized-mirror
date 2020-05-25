import { MigrationInterface, QueryRunner } from "typeorm";

export class ResetExpiredCommentReplyToOutdated1590380368
  implements MigrationInterface {
  async up(queryRunner: QueryRunner): Promise<any> {
    await queryRunner.query(
      `UPDATE commentreply SET status = 1 WHERE status = 2`
    );
  }

  async down(queryRunner: QueryRunner): Promise<any> {
    // WARN: there is no undoing this
  }
}
