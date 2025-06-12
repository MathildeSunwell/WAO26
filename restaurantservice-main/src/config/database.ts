import mongoose from 'mongoose';
import { getRequiredEnv } from '../utils/env.js';
import { logger } from './logger/logContext.js';

export async function connectMongo() {
    const uri= getRequiredEnv("MONGODB_URI");
    const dbName = 'restaurantdb';

    logger.info("Connecting to MongoDB", { dbName, host: uri });

    mongoose.connection
        .on("error", err => logger.error(err, "MongoDB connection error"))
        .on("disconnected", () => logger.warn("MongoDB disconnected"));

    try {
        await mongoose.connect(uri);
        logger.info("MongoDB connection successful");
    } catch (err) {
        logger.error(err, "MongoDB connection failed");
        process.exit(1);
    }

    return mongoose.connection;
}