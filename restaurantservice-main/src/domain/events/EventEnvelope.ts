export enum OrderEventType {
    OrderCreated = 'OrderCreated',
    RestaurantAccepted = 'RestaurantAccepted',
    RestaurantRejected = 'RestaurantRejected',
    RestaurantOrderReady = 'RestaurantOrderReady',
    RestaurantCancelled = 'RestaurantCancelled',
    PaymentReserved = 'PaymentReserved',
    PaymentFailed = 'PaymentFailed',
}

export interface ItemDto {
    itemId: string;
    productName: string;
    quantity: number;
    price: number;
}

export interface OrderCreatedPayload {
    orderId: string;
    customerAddress: string;
    items: ItemDto[];
    totalPrice: number;
}

export interface RestaurantAcceptedPayload {
    orderId: string;
}

export interface RestaurantRejectedPayload {
    orderId: string;
    reason: string;
}

export interface RestaurantOrderReadyPayload {
    orderId: string;
}

export interface RestaurantCancelledPayload {
    orderId: string;
}

export interface PaymentReservedPayload {
    orderId: string;
}

export interface PaymentFailedPayload {
    orderId: string;
    reason: string;
}

export interface PayloadMap {
    [OrderEventType.OrderCreated]: OrderCreatedPayload;
    [OrderEventType.RestaurantAccepted]: RestaurantAcceptedPayload;
    [OrderEventType.RestaurantRejected]: RestaurantRejectedPayload;
    [OrderEventType.RestaurantOrderReady]: RestaurantOrderReadyPayload;
    [OrderEventType.RestaurantCancelled]: RestaurantCancelledPayload;
    [OrderEventType.PaymentReserved]: PaymentReservedPayload;
    [OrderEventType.PaymentFailed]: PaymentFailedPayload;
}

export interface EventEnvelope<T extends OrderEventType> {
    messageId: string;
    eventType: T;
    timestamp: string;
    payload: PayloadMap[T];
}

export type AnyEventEnvelope =
    { [K in OrderEventType]: EventEnvelope<K> }[OrderEventType];
