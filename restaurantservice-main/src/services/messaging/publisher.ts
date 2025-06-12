import { ensurePublisherChannel } from '../../config/messageBroker.js';
import { AnyEventEnvelope } from "../../domain/events/EventEnvelope.js";
import { getLogger } from '../../config/logger/logContext.js';
import { DEAD_LETTER_QUEUE, DEFAULT_EXCHANGE, EVENT_EXCHANGE } from "../../config/topology.js";
import { ConfirmChannel } from "amqplib";

const MAX_PUBLISH_RETRIES = 3;
const RETRY_BASE_MS = 1000;

export async function publish(
    routingKey: string,
    payload: AnyEventEnvelope,
    correlationId?: string
): Promise<void> {
    const logger = getLogger();
    let lastError: Error | null = null;

    for (let attempt = 1; attempt <= MAX_PUBLISH_RETRIES; attempt++) {
        let channel: ConfirmChannel | null = null;
        try {
            channel = await ensurePublisherChannel();

            // set up a promise that rejects on unroutable (returned) messages
            const returnPromise = new Promise<never>((_, reject) => {
                channel!.once('return', (msg) => {
                    reject(new Error(`Message returned (unroutable) for key="${msg.fields.routingKey}"`));
                });
            });

            // publish with mandatory flag
            const buffer = Buffer.from(JSON.stringify(payload));
            channel.publish(
                EVENT_EXCHANGE,
                routingKey,
                buffer,
                { persistent: true, contentType: 'application/json', correlationId, mandatory: true }
            );

            // wait for either confirm or return
            await Promise.race([channel.waitForConfirms(), returnPromise]);

            logger.info({ exchange: EVENT_EXCHANGE, routingKey, payload }, 'Event published successfully');
            return;
        }  catch (err: any) {
            lastError = err;
            logger.warn({ err, attempt, routingKey }, `Publish attempt #${attempt} failed: ${err.message}`);
            // reset channel so initializeChannel recreates it
            channel = null;
            // exponential backoff
            const delay = RETRY_BASE_MS * (2 ** (attempt - 1));
            await new Promise(res => setTimeout(res, delay));
        }
    }

    try {
        const dlqChannel = await ensurePublisherChannel();
        const dlqBuffer = Buffer.from(JSON.stringify(payload));
        dlqChannel.publish(
            DEFAULT_EXCHANGE,
            DEAD_LETTER_QUEUE,
            dlqBuffer,
            { persistent: false, contentType: 'application/json', correlationId }
        );
        await dlqChannel.waitForConfirms();

        logger.error({ err: lastError }, `Sent to DLQ after ${MAX_PUBLISH_RETRIES} attempts`);
    } catch (dlqErr: any) {
        logger.error({ err: dlqErr, originalError: lastError }, 'Failed to send to DLQ');
    }
}
