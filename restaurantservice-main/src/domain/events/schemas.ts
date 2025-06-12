import { z } from "zod";
import {OrderEventType} from "./EventEnvelope.js";

const BaseEnvelope = z.object({
    messageId: z.string().uuid(),
    eventType: z.nativeEnum(OrderEventType),
    timestamp: z.string()
        .refine(s => !isNaN(Date.parse(s)), {
            message: "Invalid ISO timestamp"
        })
});

const OrderCreatedData = z.object({
    orderId: z.string().uuid(),
    customerAddress: z.string(),
    totalPrice: z.number(),
    items: z.array(z.object({
        itemId: z.string().uuid(),
        productName: z.string(),
        quantity: z.number().int().positive(),
        price: z.number()
    }))
});

export const EventEnvelopeSchema = z.discriminatedUnion("eventType", [
    BaseEnvelope.extend({
        eventType: z.literal(OrderEventType.OrderCreated),
        payload: OrderCreatedData
    }),
    BaseEnvelope.extend({
        eventType: z.literal(OrderEventType.PaymentReserved),
        payload: z.object({ orderId: z.string().uuid() })
    }),
    BaseEnvelope.extend({
        eventType: z.literal(OrderEventType.PaymentFailed),
        payload: z.object({ orderId: z.string().uuid(), reason: z.string() })
    }),
    BaseEnvelope.extend({
        eventType: z.literal(OrderEventType.RestaurantAccepted),
        payload: z.object({ orderId: z.string().uuid() })
    }),
    BaseEnvelope.extend({
        eventType: z.literal(OrderEventType.RestaurantRejected),
        payload: z.object({ orderId: z.string().uuid(), reason: z.string() })
    }),
    BaseEnvelope.extend({
        eventType: z.literal(OrderEventType.RestaurantOrderReady),
        payload: z.object({ orderId: z.string().uuid() })
    }),
    BaseEnvelope.extend({
        eventType: z.literal(OrderEventType.RestaurantCancelled),
        payload: z.object({ orderId: z.string().uuid() })
    })
]);

export type AnyEventEnvelope = z.infer<typeof EventEnvelopeSchema>;
