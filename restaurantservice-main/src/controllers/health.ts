import { Router, RequestHandler } from 'express';
import { httpLoggerWithContext } from '../config/logger/logContext.js'
import { ensureConsumeChannel } from '../config/messageBroker.js';
import mongoose from "mongoose";

const healthRouter = Router();
let isReady = false;

//healthRouter.use(httpLoggerWithContext)

const live: RequestHandler = (_req, res) => {
    res.status(200).send('OK');
};

const ready: RequestHandler = (_req, res) => {
    if (!isReady) {
        res.status(503).send('NOT READY');
        return;
    }

    // MongoDB health check
    const mongoState = mongoose.connection.readyState;
    const dbHealthy = mongoState === 1;

    // RabbitMQ health check
    let rabbitHealthy = true;
    try {
        const ch = ensureConsumeChannel();
        if (!ch) rabbitHealthy = false;
    } catch {
        rabbitHealthy = false;
    }

    const allHealthy = dbHealthy && rabbitHealthy;

    if (allHealthy) {
        res.status(200).json({
            status:   "UP",
            mongodb:  "CONNECTED",
            rabbitmq: "CONNECTED",
        });
    } else {
        res.status(503).json({
            status:   "DOWN",
            mongodb:  dbHealthy ? "CONNECTED" : "DISCONNECTED",
            rabbitmq: rabbitHealthy ? "CONNECTED" : "DISCONNECTED",
        });
    }
};

healthRouter.get('/live', live);
healthRouter.get('/ready', ready);

export function markReady(): void {
    isReady = true;
}

export default healthRouter;
