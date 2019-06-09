import { Request, Response, Router } from "express";
import HttpStatus from "http-status-codes";
import { authorized } from ".";
import { response } from "..";
import { CommentReply } from "../../entity";

const router: Router = Router();

router.get("/get", (req: Request, res: Response) => {
  authorized(req, res, async () => {
    let reqData = req.body.data;
    let reqRedditPostId = reqData.redditPostId;
    let comment;

    try {
      comment = await CommentReply.findOne({
        redditPostId: reqRedditPostId
      });
    } catch (err) {
      req.log.fatal({
        msg: `Error locating comment reply`,
        error: err
      });

      return response(res, {
        status: HttpStatus.INTERNAL_SERVER_ERROR,
        message: `Error retrieving comment reply information`
      });
    }

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
    else
      return response(res, {
        status: HttpStatus.NOT_FOUND,
        message: `Comment reply not found`
      });
  });
});

router.get("/getall", (req: Request, res: Response) => {
  authorized(req, res, async () => {
    let comments;

    try {
      comments = await CommentReply.find({
        order: {
          createdAt: "ASC"
        }
      });
    } catch (err) {
      req.log.fatal({
        msg: `Error locating comment reply`,
        error: err
      });

      return response(res, {
        status: HttpStatus.INTERNAL_SERVER_ERROR,
        message: `Error retrieving comment replies information`
      });
    }

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
  });
});

router.post("/update", (req: Request, res) => {
  authorized(req, res, async () => {
    let reqData = req.body.data;
    let reqRedditPostId = reqData.redditPostId;
    let reqStatus = reqData.status;
    let comment;

    try {
      comment = await CommentReply.findOne({ redditPostId: reqRedditPostId });
    } catch (err) {
      req.log.fatal({
        msg: `Error locating comment reply`,
        error: err
      });

      return response(res, {
        status: HttpStatus.INTERNAL_SERVER_ERROR,
        message: `Error retrieving comment reply information`
      });
    }

    if (!comment)
      return response(res, {
        status: HttpStatus.BAD_REQUEST,
        message: `Comment does not exist`,
        data: {
          redditPostId: reqRedditPostId
        }
      });

    comment.status = reqStatus;

    try {
      await comment.save();
    } catch (err) {
      req.log.fatal({
        msg: `Error saving updated comment`,
        error: err
      });

      return response(res, {
        status: HttpStatus.INTERNAL_SERVER_ERROR,
        message: `Error saving updated comment`
      });
    }

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
