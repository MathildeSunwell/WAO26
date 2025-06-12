import { getLogger } from "../config/logger.js";
import {
  EventEnvelope,
  ConsumerEventType,
  PayloadMap,
} from "./events/eventEnvelope.js";
import { RoutingKeyMap } from "./events/routing.js";
import { DEAD_LETTER_QUEUE, DEFAULT_EXCHANGE, EVENT_EXCHANGE, publisherChannel } from "./rabbitmq.js";
import { ConfirmChannel } from "amqplib";
import {randomUUID} from "node:crypto";

const MAX_PUBLISH_RETRIES = 3;
const RETRY_BASE_MS = 1000;

export async function publish<T extends ConsumerEventType>(
  eventType: T,
  payload: PayloadMap[T],
  correlationId: string
): Promise<void> {
  const envelope: EventEnvelope<T> = {
    messageId: randomUUID(),
    eventType,
    payload,
    timestamp: new Date(),
  };
  const logger = getLogger();
  const routingKey = RoutingKeyMap[eventType];
  let lastError: Error | null = null;

  for (let attempt = 1; attempt <= MAX_PUBLISH_RETRIES; attempt++) {
    let channel: ConfirmChannel | null = publisherChannel;
    try {
      // set up a promise that rejects on unroutable (returned) messages
      const returnPromise = new Promise<never>((_, reject) => {
        channel!.once('return', (msg) => {
          reject(new Error(`Message returned (unroutable) for key="${msg.fields.routingKey}"`));
        });
      });

      // publish with mandatory flag
      const opts = {
        persistent:   true,
        contentType:  "application/json",
        correlationId,
        mandatory:    true
      };

      const buffer = Buffer.from(JSON.stringify(envelope));
      publisherChannel.publish(EVENT_EXCHANGE, routingKey, buffer, opts);

      // wait for either confirm or return
      await Promise.race([publisherChannel.waitForConfirms(), returnPromise]);

      logger.info({ exchange: EVENT_EXCHANGE, routingKey, payload }, 'Event published successfully');
      return;
    }  catch (err: any) {
      lastError = err;
      logger.warn({ err, attempt, routingKey }, `Publish attempt #${attempt} failed: ${err.message}`);
      // reset channel so initializeChannel recreates it
      channel = null;
      // exponential backoff
      const delay = RETRY_BASE_MS * (2 ** (attempt - 1));
      await new Promise(res => setTimeout(res, delay));
    }
  }

  try {
    const opts = {
      persistent: true,
      contentType: "application/json",
      correlationId,
      mandatory: false,
    };

    const dlqBuffer = Buffer.from(JSON.stringify(payload));
    publisherChannel.publish(
        DEFAULT_EXCHANGE,
        DEAD_LETTER_QUEUE,
        dlqBuffer,
        opts
    );
    await publisherChannel.waitForConfirms();

    logger.error({ err: lastError }, `Sent to DLQ after ${MAX_PUBLISH_RETRIES} attempts`);
  } catch (dlqErr: any) {
    logger.error({ err: dlqErr, originalError: lastError }, 'Failed to send to DLQ');
  }
}