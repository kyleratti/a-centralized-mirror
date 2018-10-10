"use strict";
var __importDefault = (this && this.__importDefault) || function (mod) {
    return (mod && mod.__esModule) ? mod : { "default": mod };
};
Object.defineProperty(exports, "__esModule", { value: true });
const express_1 = require("express");
const http_status_codes_1 = __importDefault(require("http-status-codes"));
const models_1 = require("../models");
const router = express_1.Router();
const SUCCESS_MSG = 'a-mirror-bot will update the relevant comment shortly.';
function response(res, data) {
    return res.status(data.code).send({
        status: {
            code: data.code,
            message: data.message
        },
        data: data.data
    });
}
/**
 * Checks if the specified request is authorized
 * @param req The request to evaluate

 */
function authorized(req) {
    return new Promise((success, fail) => {
        if (!req.body || !req.body.auth || !req.body.auth.token || !req.body.auth.botToken)
            return fail('Auth parameters not provided');
        models_1.RegisteredBot.findOne({
            where: {
                token: req.body.auth.botToken
            }
        })
            .then((data) => {
            return (data !== null && req.body.auth.apiToken === process.env.API_TOKEN) ? success(data) : fail('Unauthorized');
        })
            .catch(fail);
    });
}
router.post('/video/update', (req, res) => {
    authorized(req)
        .then((bot) => {
        let data = req.body.data;
        models_1.MirroredVideo.insertOrUpdate({
            redditPostId: data.redditPostId,
            url: data.url
        })
            .then(() => {
            return response(res, {
                code: http_status_codes_1.default.OK,
                message: `Successfully updated mirror in database. ${SUCCESS_MSG}`
            });
        })
            .catch((err) => {
            return response(res, {
                code: http_status_codes_1.default.INTERNAL_SERVER_ERROR,
                message: `Error updating mirror: ${err}`
            });
        });
    })
        .catch((err) => {
        console.log(err);
        return response(res, {
            code: http_status_codes_1.default.UNAUTHORIZED,
            message: err
        });
    });
});
router.delete('/video/delete', (req, res) => {
    authorized(req)
        .then((bot) => {
        models_1.MirroredVideo.destroy({
            where: {
                botId: bot.id
            }
        })
            .then(() => {
            return response(res, {
                code: http_status_codes_1.default.OK,
                message: `Successfully deleted mirrored video from database. ${SUCCESS_MSG}`
            });
        })
            .catch((err) => {
            return response(res, {
                code: http_status_codes_1.default.INTERNAL_SERVER_ERROR,
                message: err
            });
        });
    })
        .catch((err) => {
        console.error(err);
        return response(res, {
            code: http_status_codes_1.default.UNAUTHORIZED,
            message: err
        });
    });
});
exports.ApiController = router;
//# sourceMappingURL=api.controller.js.map