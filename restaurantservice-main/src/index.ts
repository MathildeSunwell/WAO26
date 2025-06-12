import './config/instrumentation.js';

import express from 'express';
import { initializeRabbitMq } from './config/messageBroker.js';
import { consume } from './services/messaging/consumer.js';
import { orderEventHandler } from './services/handlers/orderEventHandler.js';
import { getLogger } from './config/logger/logContext.js'
import { apiRouter, markReady } from './routes/routes.js';
import { CONSUME_QUEUE } from "./config/topology.js";
import { connectMongo } from "./config/database.js";

const logger = getLogger();
const app = express();
const port = process.env.PORT ? parseInt(process.env.PORT) : 3000;

app.use(express.json());

app.use(apiRouter);

app.listen(port, () => {
    logger.info(`Server listening on http://localhost:${port}`);

    init().catch(err => {
        logger.error('Initialization failed', err);
        process.exit(1);
    });
});

async function init() {
    logger.info('Initializing application...');
    await connectMongo();
    await initializeRabbitMq();
    await consume(CONSUME_QUEUE, async (raw, correlationId) => {
        await orderEventHandler(raw, correlationId);
    });
    markReady();
}
