import { Request, Response, Router } from "express";
import HttpStatus from "http-status-codes";
import { response } from "..";
import { MirroredVideo, RegisteredBot, CommentReply } from "../../entity";
import { authorized } from ".";

const router: Router = Router();

router.get("/get", async (req: Request, res: Response) => {
  authorized(req, res)
    .then(async () => {
      let reqData = req.body.data;
      let reqRedditPostId = reqData.redditPostId;
      let comment = await CommentReply.findOne({
        redditPostId: reqRedditPostId
      });

      if (comment)
        return response(res, {
          status: HttpStatus.OK,
          message: `OK`,
          data: {
            id: comment.id,
            redditPostId: comment.redditPostId,
            status: comment.status,
            createdAt: comment.createdAt,
            updatedAt: comment.updatedAt
          }
        });
    })
    .catch(err => {
      return response(res, {
        status: HttpStatus.NOT_FOUND,
        message: `Comment reply not found`,
        data: {
          error: err
        }
      });
    });
});

router.get("/getall", async (req: Request, res: Response) => {
  authorized(req, res)
    .then(async () => {
      let comments = await CommentReply.find({
        order: {
          createdAt: "ASC"
        }
      });
      let commentsData = [];

      comments.forEach(comment => {
        commentsData.push({
          id: comment.id,
          redditPostId: comment.redditPostId,
          status: comment.status,
          createdAt: comment.createdAt,
          updatedAt: comment.updatedAt
        });
      });

      return response(res, {
        status: HttpStatus.OK,
        message: `OK`,
        data: {
          commentReplies: commentsData
        }
      });
    })
    .catch(err => {
      return response(res, {
        status: HttpStatus.NOT_FOUND,
        message: `No comment replies found`,
        data: {
          error: err
        }
      });
    });
});

router.post("/update", (req: Request, res) => {
  authorized(req, res).then(async () => {
    let reqData = req.body.data;
    let reqRedditPostId = reqData.redditPostId;
    let reqStatus = reqData.status;
    let comment = await CommentReply.findOne({ redditPostId: reqRedditPostId });

    if (!comment)
      return response(res, {
        status: HttpStatus.BAD_REQUEST,
        message: `Comment does not exist`,
        data: {
          redditPostId: reqRedditPostId
        }
      });

    comment.status = reqStatus;
    await comment.save();

    return response(res, {
      status: HttpStatus.OK,
      message: `Successfully updated comment reply`,
      data: {
        redditPostId: reqRedditPostId,
        status: reqStatus
      }
    });
  });
});

export const CommentReplyAdminApi: Router = router;
