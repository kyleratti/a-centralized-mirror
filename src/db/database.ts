import path from "path";
import { Connection, getConnectionManager } from "typeorm";

let db = getConnectionManager().create({
  type: "sqlite",
  database: path.resolve(
    process.env.DATABASE_LOCATION || "data/database.sqlite3"
  ),
  synchronize: true,
  logging: true,
  entities: [__dirname + "/../entity/**{.ts,.js}"],
  migrations: [__dirname + "/../migration/**{.ts,.js}"]
});

export const database: Connection = db;
