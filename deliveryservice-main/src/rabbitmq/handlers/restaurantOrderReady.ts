import { getLogger } from "../../config/logger.js";
import { RestaurantOrderReadyPayload } from "../events/eventEnvelope.js";
import { pickup } from "../../services/deliveryService.js";

export default async function handleRestaurantOrderReady(
  payload: RestaurantOrderReadyPayload,
  correlationId: string
) {
  getLogger().info({ payload }, "Received RestaurantOrderReady event");
  await pickup(payload.orderId, correlationId);
}
