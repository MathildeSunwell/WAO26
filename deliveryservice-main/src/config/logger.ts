import { AsyncLocalStorage } from "async_hooks";
import { pino } from "pino";
import pretty from "pino-pretty";

const usePretty = process.env.LOG_PRETTY === "true";

const destination = usePretty
  ? pretty({
      colorize: true,
      levelFirst: true,
      translateTime: "yyyy-mm-dd HH:MM:ss.l o",
      ignore: "pid,hostname",
    })
  : process.stdout;

const baseOptions: pino.LoggerOptions = {
  level:
    process.env.LOG_LEVEL ||
    (process.env.NODE_ENV === "production" ? "info" : "debug"),
  base: { service: "delivery-service", pid: false },
  timestamp: pino.stdTimeFunctions.isoTime,
};

export const rootLogger = pino(baseOptions, destination);

const als = new AsyncLocalStorage<{ correlationId: string }>();

export const logger = rootLogger;

export function getLogger(): pino.Logger {
  const store = als.getStore();
  return store
    ? rootLogger.child({ correlationId: store.correlationId })
    : rootLogger;
}

export function runWithCorrelation<T>(
  correlationId: string,
  fn: () => Promise<T>
): Promise<T> {
  return als.run({ correlationId }, fn) as Promise<T>;
}
