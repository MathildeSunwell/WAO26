import { initializeRabbitMq } from "./initializer.js";
import { getRequiredEnv } from "../config/utils.js";
import { logger } from "../config/logger.js";
import type { Channel, ConfirmChannel } from "amqplib";

export let publisherChannel: ConfirmChannel;
export let consumerChannel: Channel;

export const EVENT_EXCHANGE = "events.topic";
export const DEFAULT_EXCHANGE = "";
export const CONSUME_QUEUE = "delivery-queue";
export const RETRY_QUEUE = "delivery-retry-queue";
export const DEAD_LETTER_QUEUE = "delivery-dlq-queue";

export async function connectRabbit() {
  const uri = getRequiredEnv("RABBITMQ_URI");
  const user = getRequiredEnv("RABBITMQ_USER");
  const password = getRequiredEnv("RABBITMQ_PASSWORD");

  [publisherChannel, consumerChannel] = await initializeRabbitMq(
    uri,
    user,
    password
  );

  // consumerChannel can have its own QoS
  consumerChannel.prefetch(1);

  logger.info("RabbitMQ connected and set up");
}

export async function closeRabbit(): Promise<void> {
  if (publisherChannel) await publisherChannel.close();
  if (consumerChannel) await consumerChannel.close();
  logger.info("RabbitMQ channels closed");
}
