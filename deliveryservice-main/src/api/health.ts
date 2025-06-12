import { Router, Request, Response } from "express";
import mongoose from "mongoose";
import { consumerChannel, publisherChannel } from "../rabbitmq/rabbitmq.js";

const router = Router();

// Liveness probe: is the app process even running?
router.get("/live", (_req: Request, res: Response) => {
  res.status(200).json({ status: "UP" });
});

// Readiness probe: are we connected to Mongo & RabbitMQ?
router.get("/ready", async (_req: Request, res: Response) => {
  // 0 = disconnected, 1 = connected, 2 = connecting, 3 = disconnecting
  const mongoState = mongoose.connection.readyState;
  const dbHealthy = mongoState === 1;

  let rabbitHealthy = true;

  try {
    if (!publisherChannel || !consumerChannel) rabbitHealthy = false;
  } catch {
    rabbitHealthy = false;
  }

  const allHealthy = dbHealthy && rabbitHealthy;

  res.status(allHealthy ? 200 : 503).json({
    status: allHealthy ? "UP" : "DOWN",
    mongodb: dbHealthy ? "CONNECTED" : "DISCONNECTED",
    rabbitmq: rabbitHealthy ? "CONNECTED" : "DISCONNECTED",
  });
});

export default router;
