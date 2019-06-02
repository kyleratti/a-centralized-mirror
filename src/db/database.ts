import { getConnectionManager, Connection } from "typeorm";

let db = getConnectionManager().create({
  type: "sqlite",
  database: process.env.DATABASE_LOCATION || "./db/database.sqlite",
  synchronize: true,
  logging: true,
  entities: [__dirname + "/entity/*.js"],
  migrations: [__dirname + "/migration/*.js"]
});

export const database: Connection = db;
