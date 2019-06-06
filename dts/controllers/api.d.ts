/// <reference types="express-serve-static-core" />
import { Response } from "express";
import { ResponseData } from "../structures";
export declare function response(res: Response, data: ResponseData): Express.Application;
