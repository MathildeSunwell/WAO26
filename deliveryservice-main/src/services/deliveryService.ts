import { getLogger } from "../config/logger.js";
import Delivery from "../mongodb/models/delivery.js";
import {
  ConsumerEventType,
  DeliveryAssignedPayload,
  DeliveryStartedPayload,
  DeliveryCompletedPayload, OrderCreatedPayload, DeliveryCancelledPayload,
} from "../rabbitmq/events/eventEnvelope.js";
import { publish } from "../rabbitmq/publisher.js";
import { DeliveryStatus } from "../mongodb/models/delivery.js";

export async function createOrder(payload: OrderCreatedPayload, correlationId: string): Promise<void> {
    const logger = getLogger();
    logger.info("Creating a new order");
  try {
    await Delivery.create({
      orderId: payload.orderId,
      customerAddress: payload.customerAddress,
      status: DeliveryStatus.Pending,
      correlationId,
    });
    logger.debug({ payload: payload.orderId }, "Inserted new delivery in MongoDB");
  } catch (err: any) {
    if (err.code === 11000) {
      // duplicate orderId → just log & continue
      logger.error(
          { payload: payload.orderId },
          "Order already created (duplicate orderId), skipping insert"
      );
    } else {
      logger.error(err, "Error inserting delivery in MongoDB");
      return;
    }
  }
}

export async function schedule(
  orderId: string,
  correlationId: string
): Promise<void> {
  const logger = getLogger();

  logger.debug({ orderId }, "Scheduling delivery");

  const driverId = `driver-${(Math.random() * 10000) | 0}`;
  const assignedAt = new Date();

  // compute a random ETA between 15 and 30 minutes
  const MIN = 15,
    MAX = 30;
  const etaMinutes = MIN + Math.floor(Math.random() * (MAX - MIN + 1));

  try {
    await Delivery.findOneAndUpdate(
    { orderId },
    { status: DeliveryStatus.Assigned, driverId, assignedAt, etaMinutes, correlationId },
    { new: true, upsert: true }
    );
    logger.debug({ orderId }, "Inserted new delivery in MongoDB");
  } catch (err: any) {
    if (err.code === 11000) {
      // duplicate orderId → just log & continue
      logger.error(
        { orderId },
        "Delivery already scheduled (duplicate orderId), skipping insert"
      );
    } else {
      logger.error(err, "Error inserting delivery in MongoDB");
      return;
    }
  }

  // emit a DeliveryAssigned event
  const payload: DeliveryAssignedPayload = {
    orderId
  };
  await publish(ConsumerEventType.DeliveryAssigned, payload, correlationId);
  logger.info({ orderId }, "Published DeliveryAssigned event");
}

export async function pickup(
  orderId: string,
  correlationId: string
): Promise<void> {
  const logger = getLogger();

  logger.debug({ orderId }, "Starting pickup");

  const startedAt = new Date();
  await Delivery.findOneAndUpdate(
    { orderId },
    { status: DeliveryStatus.Started, startedAt, correlationId },
    { new: true }
  );

  // emit a DeliveryStarted event
  const payload: DeliveryStartedPayload = { orderId };
  await publish(ConsumerEventType.DeliveryStarted, payload, correlationId);
  logger.info({ orderId }, "Published DeliveryStarted event");

  // then hand off to delivery
  await deliver(orderId, correlationId);
}

export async function deliver(
  orderId: string,
  correlationId: string
): Promise<void> {
  const logger = getLogger();
  logger.info({ orderId }, "Beginning delivery");

  // simulate your delay...
  await new Promise((r) => setTimeout(r, 10000));

  const now = new Date();

  // mark Completed
  await Delivery.findOneAndUpdate(
    { orderId },
    { status: DeliveryStatus.Completed, completedAt: now },
    { new: true }
  );

  const completedPayload: DeliveryCompletedPayload = {
    orderId
  };
  await publish(
    ConsumerEventType.DeliveryCompleted,
    completedPayload,
    correlationId
  );
  logger.info({ orderId }, "Published DeliveryCompleted event");
}

export async function cancel(
  orderId: string,
  correlationId: string
): Promise<void> {
  const logger = getLogger();

  logger.debug({ orderId }, "Cancelling delivery");

  await Delivery.findOneAndUpdate(
      { orderId },
      { status: DeliveryStatus.Cancelled },
      { new: true }
  );

  const payload: DeliveryCancelledPayload = { orderId };
  await publish(ConsumerEventType.DeliveryCancelled, payload, correlationId);
  logger.info({ orderId }, "Delivery cancelled and event published");
}
