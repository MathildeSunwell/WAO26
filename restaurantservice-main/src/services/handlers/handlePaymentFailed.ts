import type { EventEnvelope, PaymentFailedPayload } from '../../domain/events/EventEnvelope.js';
import { OrderEventType } from '../../domain/events/EventEnvelope.js';
import { getLogger } from '../../config/logger/logContext.js';
import { orderRepository } from '../../dataAccess/orderRepository.js'
import { OrderStatus } from "../../domain/enums/OrderStatus.js";
import { publish } from "../messaging/publisher.js";
import { RoutingKeyMap } from "../../domain/events/routing.js";

export async function handlePaymentFailed(
    envelope: EventEnvelope<OrderEventType.PaymentFailed>,
    correlationId?: string)
    : Promise<void> {
    const logger = getLogger();
    const { orderId } = envelope.payload as PaymentFailedPayload;

    try {
        const updated = await orderRepository.updateFields(
            orderId,
            { OrderStatus: OrderStatus.Cancelled }
        );
        if (!updated) {
            logger.warn('Order not found when marking Cancelled');
        } else {
            logger.info('Order status set to Cancelled');
        }

        const orderCancelledEvent: EventEnvelope<OrderEventType.RestaurantCancelled> = {
            messageId: envelope.messageId,
            eventType: OrderEventType.RestaurantCancelled,
            timestamp: new Date().toISOString(),
            payload: { orderId: envelope.payload.orderId }
        };

        await publish(RoutingKeyMap[orderCancelledEvent.eventType], orderCancelledEvent, correlationId);
    } catch (err) {
        logger.error('Failed to update order status', err);
        throw err;
    }
}
