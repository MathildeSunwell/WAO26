import { ConsumerEventType, ProducerEventType } from "./eventEnvelope.js";

export const RoutingKeyMap: Record<
  ProducerEventType | ConsumerEventType,
  string
> = {
  // Publish
  [ProducerEventType.OrderCreated]: "order.created",
  [ProducerEventType.RestaurantAccepted]: "restaurant.accepted",
  [ProducerEventType.RestaurantOrderReady]: "restaurant.order_ready",
  [ProducerEventType.RestaurantRejected]: "restaurant.rejected",
  [ProducerEventType.PaymentFailed]: "payment.failed",
  // Consume
  [ConsumerEventType.DeliveryAssigned]: "delivery.assigned",
  [ConsumerEventType.DeliveryStarted]: "delivery.started",
  [ConsumerEventType.DeliveryCompleted]: "delivery.completed",
  [ConsumerEventType.DeliveryCancelled]: "delivery.cancelled",
};
