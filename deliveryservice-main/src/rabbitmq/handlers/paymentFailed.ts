import { getLogger } from "../../config/logger.js";
import { PaymentFailedPayload } from "../events/eventEnvelope.js";
import { cancel } from "../../services/deliveryService.js";

export default async function handlePaymentFailed(
    payload: PaymentFailedPayload,
    correlationId: string
) {
    getLogger().info({ payload }, "Received PaymentFailed event");
    await cancel(payload.orderId, correlationId);
}