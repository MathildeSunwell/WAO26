import { getLogger } from "../../config/logger.js";
import { RestaurantRejectedPayload } from "../events/eventEnvelope.js";
import { cancel } from "../../services/deliveryService.js";

export default async function handleRestaurantRejected(
    payload: RestaurantRejectedPayload,
    correlationId: string
) {
    getLogger().info({ payload }, "Received RestaurantRejected event");
    await cancel(payload.orderId, correlationId);
}