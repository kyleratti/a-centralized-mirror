import { Router } from "express";
import HttpStatus from "http-status-codes";
import { response } from "..";
import { AvailableMirror, RegisteredBot } from "../../entity";
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
  const bot = res.locals.bot as RegisteredBot;
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
  } catch (err) {
    req.log.error(err);

    return response(res, {
      status: HttpStatus.INTERNAL_SERVER_ERROR,
      message: `An error occurred trying to retrieve your mirror's data`,
    });
  }

  if (mirroredVideo) {
    try {
      await updateVideo(mirroredVideo, url);
    } catch (err) {
      req.log.error(err);

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
    } catch (err) {
      req.log.error(err);

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
  const bot = res.locals.bot as RegisteredBot;
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
  } catch (err) {
    req.log.error(err);

    return response(res, {
      status: HttpStatus.INTERNAL_SERVER_ERROR,
      message: `An error occurred trying to retrieve your mirror's data`,
    });
  }

  if (mirroredVideo) {
    try {
      await mirroredVideo.remove();
    } catch (err) {
      req.log.error(err);

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

export const BotApi: Router = router;
