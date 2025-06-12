// tests/rabbitmq/initializer.test.ts
import amqp from "amqplib";
import { initializeRabbitMq, getChannel } from "../../src/rabbitmq/initializer";

// 1) mock amqplib.connect()
jest.mock("amqplib");

describe("rabbitmq/initializer", () => {
  const fakeChannel = { name: "fake-ch" } as any;
  const fakeConn = { createChannel: jest.fn().mockResolvedValue(fakeChannel) };

  beforeAll(() => {
    // @ts-ignore
    amqp.connect.mockResolvedValue(fakeConn);
  });

  it("should connect and expose a channel", async () => {
    await initializeRabbitMq("amqp://test");
    // ensure we called amqp.connect with the URI
    expect(amqp.connect).toHaveBeenCalledWith("amqp://test");

    // getChannel should now return the same fakeChannel
    const ch = getChannel();
    expect(ch).toBe(fakeChannel);
  });

  it("getChannel() before init should throw", () => {
    // clear module-scoped channel to simulate “not initialized”
    jest.resetModules();
    // re-import _only_ getChannel (initializer runs in fresh module scope)
    const { getChannel: gc } = require("../../src/rabbitmq/initializer");
    expect(() => gc()).toThrow("RabbitMQ channel not initialized");
  });
});
