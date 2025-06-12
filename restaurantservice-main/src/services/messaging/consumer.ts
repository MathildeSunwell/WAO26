import { ensureConsumeChannel, getConnection } from '../../config/messageBroker.js';
import { ConfirmChannel, ConsumeMessage } from 'amqplib';
import { withLogContext, getLogger, logger } from '../../config/logger/logContext.js';
import { DEAD_LETTER_QUEUE, DEFAULT_EXCHANGE } from "../../config/topology.js";

const MAX_RETRIES = 3;
const RETRY_BASE_MS = 1000;

async function publishToDlq(msg: ConsumeMessage): Promise<void> {
    const conn = getConnection();
    let dlqChannel: ConfirmChannel = await conn.createConfirmChannel();
    if (!dlqChannel) throw new Error('DLQ channel not initialized');
    dlqChannel.publish(
        DEFAULT_EXCHANGE,
        DEAD_LETTER_QUEUE,
        msg.content,
        {
            persistent: true,
            contentType: msg.properties.contentType,
            correlationId: msg.properties.correlationId,
            headers: msg.properties.headers
        }
    );
    await dlqChannel.waitForConfirms();
}

export async function consume(
    queue: string,
    handler: (msg: any, correlationId?: string) => Promise<void>
): Promise<void> {
    const consumeChannel = await ensureConsumeChannel();

    await consumeChannel.consume(queue, async (raw: ConsumeMessage | null) => {
        if (!raw) return;
        logger.info({ queue, fields: raw.fields, properties: raw.properties }, 'Received event');

        const correlationId = raw.properties.correlationId ?? 'unknown';
        const headers = raw.properties.headers ?? {};
        const deaths = (headers["x-death"] as any[]) || [];
        const retryCount = deaths.length > 0 ? parseInt((deaths[0] as any).count, 10) : 0;

        await withLogContext(correlationId, async () => {
            const logger = getLogger();
            let payload: any;
            try {
                payload = JSON.parse(raw.content.toString());
                await handler(payload, correlationId);
                consumeChannel!.ack(raw);
                logger.info({ payload }, 'Message processed successfully');
            } catch (err) {
                logger.error({ err, payload: raw.content.toString() }, 'Error processing message');
                if (retryCount < MAX_RETRIES) {
                    const delay = RETRY_BASE_MS * Math.pow(2, retryCount);
                    setTimeout(() => consumeChannel!.nack(raw, false, true), delay);
                    logger.warn({ nextRetry: retryCount + 1 }, `Scheduled retry #${retryCount + 1} after ${delay}ms`);
                } else {
                    logger.error({ err }, `MaxRetries exceeded; publishing to DLQ manually`);
                    await publishToDlq(raw);
                    consumeChannel!.ack(raw);
                }
            }
        })
    });
}
