export enum ProducerEventType {
  OrderCreated = "OrderCreated",
  RestaurantAccepted = "RestaurantAccepted",
  RestaurantOrderReady = "RestaurantOrderReady",
  RestaurantRejected = "RestaurantRejected",
  PaymentFailed = "PaymentFailed",
}

export enum ConsumerEventType {
  DeliveryAssigned = "DeliveryAssigned",
  DeliveryStarted = "DeliveryStarted",
  DeliveryCompleted = "DeliveryCompleted",
  DeliveryCancelled = "DeliveryCancelled",
}

export interface OrderCreatedPayload {
  orderId: string;
  customerAddress: string;
  items: Array<{
    itemId: string;
    productName: string;
    quantity: number;
    price: number;
  }>;
  totalPrice: number;
}

export interface RestaurantAcceptedPayload {
  orderId: string;
}

export interface RestaurantOrderReadyPayload {
  orderId: string;
}

export interface RestaurantRejectedPayload {
  orderId: string;
  reason: string;
}

export interface PaymentFailedPayload {
  orderId: string;
  reason: string;
}

export interface DeliveryAssignedPayload {
  orderId: string;
}

export interface DeliveryStartedPayload {
  orderId: string;
}

export interface DeliveryCompletedPayload {
  orderId: string;
}

export interface DeliveryCancelledPayload {
  orderId: string;
}

export interface PayloadMap {
  [ProducerEventType.OrderCreated]: OrderCreatedPayload;
  [ProducerEventType.RestaurantAccepted]: RestaurantAcceptedPayload;
  [ProducerEventType.RestaurantOrderReady]: RestaurantOrderReadyPayload;
  [ProducerEventType.RestaurantRejected]: RestaurantRejectedPayload;
  [ProducerEventType.PaymentFailed]: PaymentFailedPayload;
  [ConsumerEventType.DeliveryAssigned]: DeliveryAssignedPayload;
  [ConsumerEventType.DeliveryStarted]: DeliveryStartedPayload;
  [ConsumerEventType.DeliveryCompleted]: DeliveryCompletedPayload;
  [ConsumerEventType.DeliveryCancelled]: DeliveryCancelledPayload;
}

export type EventEnvelope<T extends ProducerEventType | ConsumerEventType> = {
  messageId: string;
  eventType: T;
  payload: PayloadMap[T];
  timestamp: Date;
};
