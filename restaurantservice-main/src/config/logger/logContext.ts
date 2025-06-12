import { AsyncLocalStorage } from 'async_hooks';
import type { Logger } from 'pino';
import type { Request, Response, NextFunction } from 'express';
import { httpLogger } from './httpLogger.js';
import { rootLogger } from './rootLogger.js';

interface Store {
    logger: Logger;
}

const als = new AsyncLocalStorage<Store>();

export function httpLoggerWithContext(
    req: Request,
    res: Response,
    next: NextFunction
): void {
    httpLogger(req, res, () => {
        const childLogger = (req as any).log as Logger;
        als.run({ logger: childLogger }, () => next());
    });
}

export function withLogContext<T>(
    correlationId: string,
    fn: () => Promise<T>
): Promise<T> {
    const child = rootLogger.child({ correlationId });
    return als.run({ logger: child }, fn);
}

export function getLogger(): Logger {
    const store = als.getStore();
    return store?.logger ?? rootLogger;
}

export const logger = rootLogger;