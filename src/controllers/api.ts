import { Response } from "express";
import { ResponseData } from "../structures";

export function response(
  res: Response,
  data: ResponseData
): Express.Application {
  return res.status(data.status).send({
    status: {
      status: data.status,
      message: data.message
    },
    data: data.data
  });
}
