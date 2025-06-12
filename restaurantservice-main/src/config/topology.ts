export const EVENT_EXCHANGE = "events.topic";
export const DEFAULT_EXCHANGE = "";

export const CONSUME_QUEUE = "restaurant-queue";
export const RETRY_QUEUE = "restaurant-retry-queue";
export const DEAD_LETTER_QUEUE = "restaurant-dlq-queue";

export const ROUTING_KEYS = {
    CONSUME: ['order.created', 'payment.failed', 'payment.reserved'],
    PUBLISH: ['restaurant.accepted', 'restaurant.rejected', 'restaurant.order_ready']
};
