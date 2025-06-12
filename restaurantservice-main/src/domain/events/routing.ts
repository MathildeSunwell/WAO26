import { OrderEventType } from './EventEnvelope.js';

export const RoutingKeyMap: Record<OrderEventType, string> = {
    [OrderEventType.OrderCreated]: 'order.created',
    [OrderEventType.RestaurantAccepted]: 'restaurant.accepted',
    [OrderEventType.RestaurantRejected]: 'restaurant.rejected',
    [OrderEventType.RestaurantOrderReady]: 'restaurant.order_ready',
    [OrderEventType.RestaurantCancelled]: 'restaurant.cancelled',
    [OrderEventType.PaymentReserved]: 'payment.reserved',
    [OrderEventType.PaymentFailed]: 'payment.failed',
};
