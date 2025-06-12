import pinoHttp, { Options } from 'pino-http';
import { Request, Response, RequestHandler } from 'express';
import { v4 as uuidv4 } from 'uuid';
import { rootLogger } from './rootLogger.js';
import pino from "pino";

const opts: Options<Request, Response> = {
    logger: rootLogger,
    genReqId: (req) => (req.headers['x-correlation-id'] as string) || uuidv4(),
    customLogLevel: (req, res, err) => {
        if (err || res.statusCode >= 500) return 'error';
        if (res.statusCode >= 400) return 'warn';
        return 'info';
    },
    serializers: {
        req: pino.stdSerializers.req,
        res: pino.stdSerializers.res,
    },
    customSuccessMessage: (req, res) =>
        `${req.method} ${req.url} (${res.statusCode})`,
};

export const httpLogger: RequestHandler = pinoHttp(opts) as RequestHandler;