// tests/services/deliveryService.test.ts
jest.mock("../../src/models/delivery");
jest.mock("../../src/rabbitmq/publisher");

import Delivery from "../../src/models/delivery";
import { publish } from "../../src/rabbitmq/publisher";
import * as DeliveryService from "../../src/services/deliveryService";

// grab the mocks
const mockCreate = Delivery.create as jest.Mock;
const mockFindOneUpdate = Delivery.findOneAndUpdate as jest.Mock;
const mockPublish = publish as jest.Mock;

beforeEach(() => {
  jest.clearAllMocks();
});

describe("deliveryService.schedule()", () => {
  const MIN_ETA = 15;
  const MAX_ETA = 30;

  it("should persist and publish eta = MIN_ETA when Math.random() = 0.0", async () => {
    jest.spyOn(Math, "random").mockReturnValue(0.0);

    await DeliveryService.schedule("order-1");

    expect(mockCreate).toHaveBeenCalledWith({
      orderId: "order-1",
      driverId: expect.stringMatching(/^driver-/),
      status: "Scheduled",
      etaMinutes: MIN_ETA,
    });
    expect(mockPublish).toHaveBeenCalledWith("delivery_scheduled_queue", {
      orderId: "order-1",
      driverId: expect.any(String),
      etaMinutes: MIN_ETA,
    });
  });

  it("should persist and publish eta = MAX_ETA when Math.random() ≈ 1.0", async () => {
    jest.spyOn(Math, "random").mockReturnValue(0.999);

    await DeliveryService.schedule("order-2");

    expect(mockCreate).toHaveBeenCalledWith({
      orderId: "order-2",
      driverId: expect.stringMatching(/^driver-/),
      status: "Scheduled",
      etaMinutes: MAX_ETA,
    });
    expect(mockPublish).toHaveBeenCalledWith("delivery_scheduled_queue", {
      orderId: "order-2",
      driverId: expect.any(String),
      etaMinutes: MAX_ETA,
    });
  });
});

describe("deliveryService.pickup()", () => {
  it("should update to PickedUp and publish order_picked_up_queue", async () => {
    const fakeDoc = {
      orderId: "order-3",
      status: "PickedUp",
      pickedUpAt: new Date(),
    };
    mockFindOneUpdate.mockResolvedValue(fakeDoc);

    await DeliveryService.pickup("order-3");

    expect(mockFindOneUpdate).toHaveBeenCalledWith(
      { orderId: "order-3" },
      expect.objectContaining({
        status: "PickedUp",
        pickedUpAt: expect.any(Date),
      }),
      { new: true }
    );
    expect(mockPublish).toHaveBeenCalledWith("order_picked_up_queue", {
      orderId: "order-3",
      pickedUpAt: expect.any(Date),
    });
  });
});

describe("deliveryService.deliver()", () => {
  it("should update to Delivered and publish order_delivered_queue on success", async () => {
    jest.spyOn(Math, "random").mockReturnValue(0.1);
    const fakeDoc = {
      orderId: "order-4",
      status: "Delivered",
      deliveredAt: new Date(),
    };
    mockFindOneUpdate.mockResolvedValue(fakeDoc);

    await DeliveryService.deliver("order-4");

    expect(mockFindOneUpdate).toHaveBeenCalledWith(
      { orderId: "order-4" },
      expect.objectContaining({
        status: "Delivered",
        deliveredAt: expect.any(Date),
      }),
      { new: true }
    );
    expect(mockPublish).toHaveBeenCalledWith("order_delivered_queue", {
      orderId: "order-4",
      deliveredAt: expect.any(Date),
    });
  });

  it("should update to Failed and publish delivery_failed_queue on failure", async () => {
    jest.spyOn(Math, "random").mockReturnValue(0.95);
    const fakeDoc = { orderId: "order-5", status: "Failed" };
    mockFindOneUpdate.mockResolvedValue(fakeDoc);

    await DeliveryService.deliver("order-5");

    expect(mockFindOneUpdate).toHaveBeenCalledWith(
      { orderId: "order-5" },
      { status: "Failed", reason: "Driver lost the pizza" },
      { new: true }
    );
    expect(mockPublish).toHaveBeenCalledWith("delivery_failed_queue", {
      orderId: "order-5",
      reason: "Driver lost the pizza",
    });
  });
});
