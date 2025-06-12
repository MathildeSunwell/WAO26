import { getLogger } from "../../config/logger.js";
import { RestaurantAcceptedPayload } from "../events/eventEnvelope.js";
import { schedule } from "../../services/deliveryService.js";

export default async function handleRestaurantAccepted(
  payload: RestaurantAcceptedPayload,
  correlationId: string
) {
  getLogger().info({ payload }, "Received RestaurantAccepted event");
  await schedule(payload.orderId, correlationId);
}
