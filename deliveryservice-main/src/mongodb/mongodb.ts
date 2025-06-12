import mongoose from "mongoose";
import { getRequiredEnv } from "../config/utils.js";
import { logger } from "../config/logger.js";

export async function connectMongo() {
  const uri = getRequiredEnv("MONGODB_URI");
  const dbName = getRequiredEnv("MONGODB_DB_NAME");
  const user = getRequiredEnv("MONGODB_USER");
  const pass = getRequiredEnv("MONGODB_PASSWORD");

  logger.info({ dbName, host: uri }, "Connecting to MongoDB");

  try {
    await mongoose.connect(uri, {
      user,
      pass,
      dbName,
    });
    logger.info("MongoDB connection successful");
  } catch (err) {
    logger.error(err, "MongoDB connection error");
    process.exit(1);
  }
}
