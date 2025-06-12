import { EventEnvelope, OrderEventType } from '../../domain/events/EventEnvelope.js';
import { getLogger } from '../../config/logger/logContext.js';
import { orderRepository } from '../../dataAccess/orderRepository.js';
import { orderItemRepository } from '../../dataAccess/orderItemRepository.js';
import { OrderStatus } from "../../domain/enums/OrderStatus.js";

export async function handleOrderCreated(
    envelope: EventEnvelope<OrderEventType.OrderCreated>,
    correlationId?: string
): Promise<void> {
    const logger = getLogger();
    const { orderId, items } = envelope.payload;

    try {
        let estimatedPrepTime = 10;

        const order = await orderRepository.create({
            OrderId:           orderId,
            CorrelationId:     correlationId,
            OrderStatus:       OrderStatus.Pending,
            EstimatedPrepTime: estimatedPrepTime,
            OrderItems:        [],
        });

        for (const item of items) {
            const createdItem = await orderItemRepository.createItem({
                OrderId:     orderId,
                ProductName: item.productName,
                Quantity:    item.quantity,
                Price:       item.price
            });

            await orderRepository.addItem(
                order._id.toString(),
                createdItem._id.toString()
            );
        }

        logger.info('Order saved', {
            orderId: order._id
        });

    } catch (err: any) {
        logger.error(
            {
                err,
                errMessage: err.message,
                validationErrors: err.errors
            },
            'Failed to save order'
        );
        throw err;
    }
}
