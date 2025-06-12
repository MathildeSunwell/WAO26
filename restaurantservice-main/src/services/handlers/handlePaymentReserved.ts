import { publish } from '../messaging/publisher.js';
import type {
    EventEnvelope,
    PaymentReservedPayload
} from '../../domain/events/EventEnvelope.js';
import { OrderEventType } from '../../domain/events/EventEnvelope.js';
import { getLogger } from '../../config/logger/logContext.js';
import { orderRepository } from "../../dataAccess/orderRepository.js";
import { OrderStatus } from "../../domain/enums/OrderStatus.js";
import { RoutingKeyMap } from "../../domain/events/routing.js";

function sleep(ms: number) {
    return new Promise<void>(resolve => setTimeout(resolve, ms));
}

async function prepareOrder(
    envelope: EventEnvelope<OrderEventType.RestaurantAccepted>,
    correlationId?: string
) {
    const logger = getLogger();
    logger.info('Preparing order in kitchen...');

    await sleep(10_000);

    try {
        const ok = await orderRepository.updateFields(
            envelope.payload.orderId,
            { OrderStatus: OrderStatus.Ready }
        );
        if (!ok) {
            logger.warn('Order not found when marking Ready');
        }
    } catch (err) {
        logger.error({ err }, 'Error updating order status to Ready');
        throw err;
    }

    const orderReadyEvent: EventEnvelope<OrderEventType.RestaurantOrderReady> = {
        messageId: envelope.messageId,
        eventType: OrderEventType.RestaurantOrderReady,
        timestamp: new Date().toISOString(),
        payload: { orderId: envelope.payload.orderId }
    };

    await publish(RoutingKeyMap[orderReadyEvent.eventType], orderReadyEvent, correlationId);
}

export async function handlePaymentReserved(
    envelope: EventEnvelope<OrderEventType.PaymentReserved>,
    correlationId?: string
): Promise<void> {
    const logger = getLogger();
    const { orderId } = envelope.payload as PaymentReservedPayload;

    const accepted = Math.random() < 0.75;

    try {
        await orderRepository.updateFields(
            orderId,
            { OrderStatus: accepted ? OrderStatus.Accepted : OrderStatus.Rejected }
        );
        logger.info(`Order ${orderId} marked ${accepted ? 'Accepted' : 'Rejected'}`);
    } catch (err: any) {
        logger.error({
                err,
                errMessage: err.message,
                validationErrors: err.errors
            },
            'Error updating status in DB');
        throw err;
    }

    if (accepted) {
        const acceptedEvent: EventEnvelope<OrderEventType.RestaurantAccepted> = {
            ...envelope,
            eventType: OrderEventType.RestaurantAccepted,
            timestamp: new Date().toISOString(),
            payload: { orderId }
        };
        await publish(RoutingKeyMap[acceptedEvent.eventType], acceptedEvent, correlationId);
        await prepareOrder(acceptedEvent, correlationId);
    } else {
        const rejectedEvent: EventEnvelope<OrderEventType.RestaurantRejected> = {
            ...envelope,
            eventType: OrderEventType.RestaurantRejected,
            timestamp: new Date().toISOString(),
            payload: { orderId, reason: 'Kitchen is busy' }
        };
        await publish(RoutingKeyMap[rejectedEvent.eventType], rejectedEvent, correlationId);
    }
}
