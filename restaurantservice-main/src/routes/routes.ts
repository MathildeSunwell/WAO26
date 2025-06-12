import { Router } from 'express';
import healthRouter, { markReady } from '../controllers/health.js';

const apiRouter = Router();

apiRouter.use('/health', healthRouter);

apiRouter.get('/', (_req, res) => {
    res.send('The API is running');
});

export { apiRouter, markReady };
