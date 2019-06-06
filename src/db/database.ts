import { getConnectionManager, Connection } from "typeorm";

let db = getConnectionManager().create({
  type: "sqlite",
  database: process.env.DATABASE_LOCATION || "./db/database.sqlite",
  synchronize: true,
  logging: true,
  entities: [__dirname + "/../entity/**{.ts,.js}"],
  migrations: [__dirname + "/../migration/**{.ts,.js}"]
});

export const database: Connection = db;
