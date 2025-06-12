import { Channel, ConsumeMessage } from "amqplib";
import { getLogger, runWithCorrelation } from "../config/logger.js";
import { consumerChannel, CONSUME_QUEUE, DEAD_LETTER_QUEUE } from "./rabbitmq.js";
import { ProducerEventType } from "./events/eventEnvelope.js";
import handleRestaurantAccepted from "./handlers/restaurantAccepted.js";
import handleRestaurantOrderReady from "./handlers/restaurantOrderReady.js";
import handleOrderCreated from "./handlers/orderCreated.js";
import handleRestaurantRejected from "./handlers/restaurantRejected.js";
import handlePaymentFailed from "./handlers/paymentFailed.js";

export async function registerConsumers() {
  await consumerChannel.consume(
    CONSUME_QUEUE,
    wrapConsumer(consumerChannel, processDeliveryMessage),
    { noAck: false }
  );

  getLogger().info({ queue: CONSUME_QUEUE }, "Consumer registered");
}

async function processDeliveryMessage(msg: ConsumeMessage) {
  const logger = getLogger();
  const corrId = msg.properties.correlationId;
  let envelope: any;

  try {
    envelope = JSON.parse(msg.content.toString());
  } catch (err) {
    logger.error(err, "Invalid JSON payload");
    throw err;
  }

  logger.debug({ envelope }, "Envelope parsed");

  switch (envelope.eventType) {
    case ProducerEventType.OrderCreated:
      await handleOrderCreated(envelope.payload, corrId);
        break;
    case ProducerEventType.RestaurantAccepted:
      await handleRestaurantAccepted(envelope.payload, corrId);
      break;
    case ProducerEventType.RestaurantOrderReady:
      await handleRestaurantOrderReady(envelope.payload, corrId);
      break;
    case ProducerEventType.RestaurantRejected:
      await handleRestaurantRejected(envelope.payload, corrId);
      break;
    case ProducerEventType.PaymentFailed:
      await handlePaymentFailed(envelope.payload, corrId);
      break;
    default:
      logger.warn({ eventType: envelope.eventType }, "Unknown eventType");
      throw new Error("Unknown eventType");
  }
}

export function wrapConsumer(
  channel: Channel,
  handler: (msg: ConsumeMessage) => Promise<void>
) {
  return async (msg: ConsumeMessage | null) => {
    const MaxRetries = 3;
    const logger = getLogger();

    if (!msg) {
      logger.warn("Received null message");
      return;
    }
    logger.info({ fields: msg.fields, properties: msg.properties }, 'Received event');

    const headers = msg.properties.headers ?? {};
    const deaths = (headers["x-death"] as any[]) || [];
    const retryCount = deaths.length > 0
        ? parseInt((deaths[0] as any).count, 10)
        : 0;
    const corrId = msg.properties.correlationId;

    try {
      if (!corrId) {
        throw new Error('Missing correlationId');
      }
      await runWithCorrelation(corrId, () => handler(msg));
      channel.ack(msg);
    } catch (err) {
      if (retryCount < MaxRetries) {
        logger.warn(`Transient failure; retrying attempt #${retryCount+1}`);
        consumerChannel.nack(msg, false, false);
      } else {
        logger.error(`MaxRetries exceeded; publishing to DLQ manually`);
        consumerChannel.publish(
            "",
            DEAD_LETTER_QUEUE,
            msg.content,
            {
              persistent:    true,
              contentType:   msg.properties.contentType,
              correlationId: msg.properties.correlationId,
              headers:       msg.properties.headers
            }
        );
        consumerChannel.ack(msg);
      }
    }
  };
}
