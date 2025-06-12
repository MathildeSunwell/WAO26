import amqp, {Channel, ChannelModel, ConfirmChannel} from 'amqplib';
import { logger } from './logger/logContext.js'
import { CONSUME_QUEUE, DEAD_LETTER_QUEUE, EVENT_EXCHANGE, RETRY_QUEUE, ROUTING_KEYS } from "./topology.js";

let connection: ChannelModel | null = null;
let consumeChannel: Channel | null = null;
let publisherChannel: ConfirmChannel | null = null;
let reconnectDelay = 1000;

const uri = process.env.RABBITMQ_URI;

export async function initializeRabbitMq(): Promise<void> {
    if (!uri) {
        logger.error('RABBITMQ_URI is missing');
        process.exit(1);
    }

    try {
        connection = await amqp.connect(uri);
        connection.on('error', err => {
            logger.error('AMQP connection error', { err });
        });
        connection.on('close', () => {
            logger.warn(`AMQP connection closed, reconnecting in ${reconnectDelay}ms`);
            connection = null;
            consumeChannel = null;
            setTimeout(initializeRabbitMq, reconnectDelay);
            reconnectDelay = Math.min(reconnectDelay * 2, 30000);
        });

        // Consume channel setup
        consumeChannel = await connection.createChannel();
        consumeChannel.on('error', err => {
            logger.error('AMQP consume channel error', { err });
        });
        await consumeChannel.prefetch(1); // Set QoS for consumer channel

        // Publisher channel setup
        publisherChannel = await connection.createConfirmChannel();
        publisherChannel.on('error', err => {
            logger.error('AMQP publisher channel error', { err });
        });
        publisherChannel.on('close', () => {
            logger.warn('AMQP publisher channel closed, will recreate on next publish');
            publisherChannel = null;
        });

        // Assert exchange and queues
        await consumeChannel.assertExchange(EVENT_EXCHANGE, "topic", {
            durable: true,
            autoDelete: false,
        });

        await consumeChannel.assertQueue(CONSUME_QUEUE, {
            durable: true,
            arguments: {
                "x-dead-letter-exchange":    "",
                "x-dead-letter-routing-key": RETRY_QUEUE
            }
        });
        logger.info(`Asserted queue ${CONSUME_QUEUE} with dead-letter routing to ${RETRY_QUEUE}`);

        for (const key of ROUTING_KEYS.CONSUME) {
            await consumeChannel.bindQueue(CONSUME_QUEUE, EVENT_EXCHANGE, key);
            logger.info(`Bound ${CONSUME_QUEUE} ← ${EVENT_EXCHANGE}:${key}`);
        }

        await consumeChannel.assertQueue(RETRY_QUEUE, {
            durable: true,
            arguments: {
                "x-message-ttl":             30_000, // 30s
                "x-dead-letter-exchange":    "",
                "x-dead-letter-routing-key": CONSUME_QUEUE
            }
        });
        logger.info(`Asserted retry queue ${RETRY_QUEUE} with TTL and dead-letter routing to ${CONSUME_QUEUE}`);

        await consumeChannel.assertQueue(DEAD_LETTER_QUEUE, {
            durable: true
        });
        logger.info(`Asserted dead-letter queue ${DEAD_LETTER_QUEUE}`);

        logger.info('Connected to RabbitMq');
    }
    catch (err) {
        logger.error('Error connecting to RabbitMq', { err });
        if (connection) {
            await connection.close();
        }
        setTimeout(initializeRabbitMq, reconnectDelay);
        reconnectDelay = Math.min(reconnectDelay * 2, 30000);
    }
}

export async function ensureConsumeChannel(): Promise<Channel> {
    if (consumeChannel) return consumeChannel;
    return new Promise<Channel>(resolve => {
        const check = () => {
            if (consumeChannel) {
                resolve(consumeChannel);
            } else {
                setTimeout(check, 100);
            }
        };
        check();
    });
}

export async function ensurePublisherChannel(): Promise<ConfirmChannel> {
    if (publisherChannel) return publisherChannel;
    return new Promise<ConfirmChannel>(resolve => {
        const check = () => {
            if (publisherChannel) {
                resolve(publisherChannel);
            } else {
                setTimeout(check, 100);
            }
        };
        check();
    });
}

export function getConnection(): ChannelModel {
    if (!connection) {
        throw new Error('RabbitMQ connection not initialized yet');
    }
    return connection;
}

export async function closeRabbit(): Promise<void> {
    if (consumeChannel)  await consumeChannel.close();
    if (connection) await connection.close();
    logger.info('Connections to RabbitMq closed');
}
