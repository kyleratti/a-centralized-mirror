import { MigrationInterface, QueryRunner } from "typeorm";

export class AddWeightToBots1609376826000 implements MigrationInterface {
  async up(queryRunner: QueryRunner): Promise<any> {
    await queryRunner.query(`UPDATE registered_bot SET weight = 0`);

    await queryRunner.query(
      `UPDATE registered_bot SET weight = -1 WHERE username = 'tuckbot'`
    );
  }

  async down(queryRunner: QueryRunner): Promise<any> {
    await queryRunner.query(`UPDATE registered_bot SET weight = 0`);
  }
}
