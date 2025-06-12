import amqp, { Channel, ConfirmChannel } from "amqplib";
import { logger } from "../config/logger.js";
import { EVENT_EXCHANGE, CONSUME_QUEUE, RETRY_QUEUE, DEAD_LETTER_QUEUE } from "./rabbitmq.js";
import { RoutingKeyMap } from "./events/routing.js";
import { ProducerEventType } from "./events/eventEnvelope.js";

export async function initializeRabbitMq(
  hostname: string,
  username: string,
  password: string
): Promise<[ConfirmChannel, Channel]> {
  logger.debug(
    { hostname, username, password: "***" },
    "Initializing RabbitMQ connection"
  );
  const conn = await amqp.connect({ hostname, username, password });

  const publisherChannel = await conn.createConfirmChannel();
  logger.debug("PublisherChannel established");

  const consumerChannel = await conn.createChannel();
  logger.debug("ConsumerChannel established");

  await publisherChannel.assertExchange(EVENT_EXCHANGE, "topic", { durable: true });
  await consumerChannel.assertExchange(EVENT_EXCHANGE, "topic", { durable: true });
  logger.debug({ EVENT_EXCHANGE }, "Exchange asserted");

  await consumerChannel.assertQueue(CONSUME_QUEUE, {
    durable: true,
    arguments: {
      "x-dead-letter-exchange": "",
      "x-dead-letter-routing-key": RETRY_QUEUE
    }
  });
  logger.debug({ CONSUME_QUEUE }, "Queue asserted");

  // Bind the consume queue to the exchange with all relevant routing keys
  await bindQueue(
      consumerChannel,
      RoutingKeyMap[ProducerEventType.OrderCreated]
  );

  await bindQueue(
    consumerChannel,
    RoutingKeyMap[ProducerEventType.RestaurantAccepted]
  );

  await bindQueue(
    consumerChannel,
    RoutingKeyMap[ProducerEventType.RestaurantOrderReady]
  );

  await bindQueue(
      consumerChannel,
      RoutingKeyMap[ProducerEventType.RestaurantRejected]
  );

  await bindQueue(
      consumerChannel,
      RoutingKeyMap[ProducerEventType.PaymentFailed]
  );

  await consumerChannel.assertQueue(RETRY_QUEUE, {
    durable: true,
    arguments: {
      "x-message-ttl": 30_000, // 30s
      "x-dead-letter-exchange": "",
      "x-dead-letter-routing-key": CONSUME_QUEUE
    }
  });

  await consumerChannel.assertQueue(DEAD_LETTER_QUEUE, {
    durable: true
  });

  return [publisherChannel, consumerChannel];
}

async function bindQueue(consumerChannel: amqp.Channel, routingKey: string) {
  await consumerChannel.bindQueue(CONSUME_QUEUE, EVENT_EXCHANGE, routingKey);
  logger.debug(
    {
      CONSUME_QUEUE,
      routingKey,
    },
    "Queue bound to exchange with routing key"
  );
}
