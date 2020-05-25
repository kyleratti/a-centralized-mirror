import { Router } from "express";
import HttpStatus from "http-status-codes";
import { response } from "..";
import { AvailableMirror } from "../../entity";
import {
  CreateMirrorRequest,
  DeleteRequest,
  UpdateRequest,
} from "../../structures";
import { hasAuthHeaders, isAuthorized, isValidRequest } from "./api";

const router: Router = Router();

const SUCCESS_MSG = "a-mirror-bot will update the associated comment shortly.";

async function updateVideo(mirroredVideo: AvailableMirror, url: string) {
  mirroredVideo.mirrorUrl = url;
  await mirroredVideo.save();
}

async function createVideo(data: CreateMirrorRequest) {
  let newMirroredVideo = new AvailableMirror();
  newMirroredVideo.redditPostId = data.redditPostId;
  newMirroredVideo.mirrorUrl = data.url;
  newMirroredVideo.bot = data.bot;
  await newMirroredVideo.save();
}

router.all("/*", async (req, res, next) => {
  try {
    hasAuthHeaders(req);
  } catch (err) {
    return response(res, {
      status: HttpStatus.UNPROCESSABLE_ENTITY,
      message: err,
    });
  }

  try {
    res.locals.bot = await isAuthorized(req);
  } catch (err) {
    return response(res, {
      status: HttpStatus.UNAUTHORIZED,
      message: err,
    });
  }

  try {
    isValidRequest(req);
  } catch (err) {
    return response(res, {
      status: HttpStatus.UNPROCESSABLE_ENTITY,
      message: err,
    });
  }

  next();
});

router.post("/update", async (req, res) => {
  const bot = res.locals.bot;
  const data = req.body.data as UpdateRequest;
  const [redditPostId, url] = [data.redditPostId, data.url];

  let mirroredVideo: AvailableMirror;

  try {
    mirroredVideo = await AvailableMirror.findOne({
      where: {
        redditPostId: redditPostId,
        bot: bot,
      },
    });
  } catch (_err) {
    return response(res, {
      status: HttpStatus.INTERNAL_SERVER_ERROR,
      message: `An error occurred trying to retrieve your mirror's data`,
    });
  }

  if (mirroredVideo) {
    try {
      await updateVideo(mirroredVideo, url);
    } catch (_err) {
      return response(res, {
        status: HttpStatus.INTERNAL_SERVER_ERROR,
        message: `An error occurred updating your mirror in the database`,
      });
    }

    return response(res, {
      status: HttpStatus.OK,
      message: `Successfully updated mirror in database. ${SUCCESS_MSG}`,
    });
  } else {
    try {
      createVideo({
        redditPostId: redditPostId,
        url: url,
        bot: bot,
      });
    } catch (_err) {
      return response(res, {
        status: HttpStatus.INTERNAL_SERVER_ERROR,
        message: `An error occurred creating your mirror in the database`,
      });
    }

    return response(res, {
      status: HttpStatus.OK,
      message: `Successfully created mirror in database. ${SUCCESS_MSG}`,
    });
  }
});

router.delete("/delete", async (req, res) => {
  const bot = res.locals.bot;
  const data = req.body.data as DeleteRequest;
  const [redditPostId, url] = [data.redditPostId, data.url];

  let mirroredVideo: AvailableMirror;

  try {
    mirroredVideo = await AvailableMirror.findOne({
      where: {
        redditPostId: redditPostId,
        mirrorUrl: url,
        bot: bot,
      },
    });
  } catch (_err) {
    return response(res, {
      status: HttpStatus.INTERNAL_SERVER_ERROR,
      message: `An error occurred trying to retrieve your mirror's data`,
    });
  }

  if (mirroredVideo) {
    try {
      await mirroredVideo.remove();
    } catch (_err) {
      return response(res, {
        status: HttpStatus.INTERNAL_SERVER_ERROR,
        message: `An error occurred trying to remove your mirror`,
      });
    }

    return response(res, {
      status: HttpStatus.OK,
      message: `Successfully removed mirror from database. ${SUCCESS_MSG}`,
    });
  } else {
    return response(res, {
      status: HttpStatus.NOT_FOUND,
      message: `Mirror not found in database`,
    });
  }
});

/* router.post("/reddit/updateposts", (req, res) => {
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
    });
}); */

export const BotApi: Router = router;
