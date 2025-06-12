import './config/instrumentation.js';

import express from "express";
import { connectMongo } from "./mongodb/mongodb.js";
import { registerConsumers } from "./rabbitmq/consumer.js";
import healthRouter from "./api/health.js";
import { logger } from "./config/logger.js";
import { connectRabbit } from "./rabbitmq/rabbitmq.js";

async function bootstrap() {
  await connectMongo();
  await connectRabbit();
  await registerConsumers();

  const app = express();
  app.use(express.json());
  app.use("/health", healthRouter);

  app.listen(3000, () =>
    logger.info("DeliveryService listening on http://localhost:3000")
  );
}

bootstrap().catch((err) => {
  logger.error(err, "Fatal bootstrap error");
  process.exit(1);
});
