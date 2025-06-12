import dotenv from 'dotenv';
import { getLogger } from '../config/logger/logContext.js';
dotenv.config();
const logger = getLogger();

export const getRequiredEnv = (key: string): string => {
    const v = process.env[key];
    if (!v) {
        logger.error({ key }, "Missing env var");
        process.exit(1);
    }
    logger.debug({ key }, "Environment variable loaded");

    return v;
};
