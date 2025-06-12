// tests/rabbitmq/publisher.test.ts
import { publish } from "../../src/rabbitmq/publisher";
import { getChannel } from "../../src/rabbitmq/initializer";

// mock getChannel() to return a fake channel with spies
jest.mock("../../src/rabbitmq/initializer");

describe("rabbitmq/publisher", () => {
  const fakeChannel = {
    assertQueue: jest.fn().mockResolvedValue(undefined),
    sendToQueue: jest.fn(),
  };

  beforeAll(() => {
    // @ts-ignore
    getChannel.mockReturnValue(fakeChannel);
  });

  beforeEach(() => {
    jest.clearAllMocks();
  });

  it("asserts the queue and sends the message", async () => {
    const payload = { foo: "bar" };
    await publish("my_queue", payload);

    expect(fakeChannel.assertQueue).toHaveBeenCalledWith("my_queue", {
      durable: true,
    });
    expect(fakeChannel.sendToQueue).toHaveBeenCalledWith(
      "my_queue",
      Buffer.from(JSON.stringify(payload)),
      { persistent: true }
    );
  });
});
