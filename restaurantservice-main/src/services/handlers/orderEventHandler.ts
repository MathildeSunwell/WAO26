import { AnyEventEnvelope } from '../../domain/events/EventEnvelope.js';
import { OrderEventType } from '../../domain/events/EventEnvelope.js';
import { handleOrderCreated }   from './handleOrderCreated.js';
import { handlePaymentReserved } from "./handlePaymentReserved.js";
import { handlePaymentFailed } from "./handlePaymentFailed.js";
import { getLogger } from '../../config/logger/logContext.js';
import { EventEnvelopeSchema } from "../../domain/events/schemas.js";
import { ZodError } from "zod";

export async function orderEventHandler(raw: any, correlationId?: string): Promise<void> {
    const logger = getLogger();
    let envelope: AnyEventEnvelope;

    try {
        envelope = EventEnvelopeSchema.parse(raw);
    } catch (err) {
        if (err instanceof ZodError) {
            logger.error({ issues: err.errors }, "Envelope validation failed");
        } else {
            logger.error({ err }, "Failed to parse raw event");
        }
        throw err;
    }

    switch (envelope.eventType) {
        case OrderEventType.OrderCreated:
            await handleOrderCreated(envelope, correlationId);
            break;

        case OrderEventType.PaymentReserved:
            await handlePaymentReserved(envelope, correlationId);
            break;

        case OrderEventType.PaymentFailed:
            await handlePaymentFailed(envelope, correlationId);
            break;

        default:
            logger.warn('Unhandled EventType');
    }
}
