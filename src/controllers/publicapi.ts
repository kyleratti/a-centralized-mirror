import { Router } from "express";
import HttpStatus from "http-status-codes";
import { RegisteredBot, MirroredVideo, CommentReply } from "../entity";

const router: Router = Router();

const SUCCESS_MSG = "a-mirror-bot will update the relevant comment shortly.";

export interface ResponseData {
  /** The HTTP status code to respond with */
  code: number;

  /** The message to respond with */
  message: string;

  /** The data to respond with, if any */
  data?: object;
}

function response(res, data: ResponseData): Express.Application {
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
    if (
      !req.body ||
      !req.body.auth ||
      !req.body.auth.token ||
      !req.body.auth.botToken
    )
      return fail("Auth parameters not provided");

    // TODO: Verify the request has a valid bot token
  });
}

router.post("/video/update", (req, res) => {
  authorized(req).then((bot: RegisteredBot) => {
    let data = req.body.data;

    /*MirroredVideo.insertOrUpdate({
        redditPostId: data.redditPostId,
        url: data.url
      })
        .then(() => {
          return response(res, {
            code: HttpStatus.OK,
            message: `Successfully updated mirror in database. ${SUCCESS_MSG}`
          });
        })
        .catch(err => {
          return response(res, {
            code: HttpStatus.INTERNAL_SERVER_ERROR,
            message: `Error updating mirror: ${err}`
          });
        });
    })
    .catch(err => {
      console.log(err);

      return response(res, {
        code: HttpStatus.UNAUTHORIZED,
        message: err
      });*/
  });
});

router.delete("/video/delete", (req, res) => {
  authorized(req)
    .then((bot: RegisteredBot) => {
      /*MirroredVideo.destroy({
        where: {
          botId: bot.id
        }
      })
        .then(() => {
          return response(res, {
            code: HttpStatus.OK,
            message: `Successfully deleted mirrored video from database. ${SUCCESS_MSG}`
          });
        })
        .catch(err => {
          return response(res, {
            code: HttpStatus.INTERNAL_SERVER_ERROR,
            message: err
          });
        });*/
    })
    .catch(err => {
      console.error(err);

      return response(res, {
        code: HttpStatus.UNAUTHORIZED,
        message: err
      });
    });
});

router.post("/reddit/updateposts", (req, res) => {
  // TODO: check for API + CRONTAB authentication
  /*CommentReply.findAll({
    where: {
      status: CommentStatus.AwaitingUpdate
    },
    group: "redditPostId"
  })
    .then(data => {
      // TODO: assemble array based on post ID
      // TODO: loop through array
      // TODO: select all comments based on postId?
      // TODO: generate new reply, send to reddit
    })
    .catch(err => {
      return response(res, {
        code: HttpStatus.INTERNAL_SERVER_ERROR,
        message: `Error updating reddit posts: ${err}`
      });
    });*/
});

export const PublicApi: Router = router;
