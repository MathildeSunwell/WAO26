import { getLogger } from "../../config/logger.js";
import { OrderCreatedPayload } from "../events/eventEnvelope.js";
import { createOrder } from "../../services/deliveryService.js";

export default async function handleOrderCreated(
    payload: OrderCreatedPayload,
    correlationId: string
) {
    getLogger().info({ payload }, "Received OrderCreated event");
    await createOrder(payload, correlationId);
}