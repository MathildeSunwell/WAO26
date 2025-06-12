import mongoose, { Document, Schema } from "mongoose";

export interface IDelivery extends Document {
  orderId: string;
  correlationId: string;
  customerAddress?: string;
  driverId?: string;
  status: DeliveryStatus;
  assignedAt?: Date;
  etaMinutes?: number;
  scheduledAt?: Date;
  pickedUpAt?: Date;
  deliveredAt?: Date;
}

export enum DeliveryStatus {
  Pending = "Pending",
  Assigned = "Assigned",
  Started = "Started",
  Completed = "Completed",
  Cancelled = "Cancelled",
}

const DeliverySchema = new Schema<IDelivery>(
  {
    orderId: { type: String, required: true, unique: true },
    correlationId: { type: String },
    customerAddress: { type: String },
    driverId: { type: String },
    status: {
      type: String,
      enum: Object.values(DeliveryStatus),
      required: true,
    },
    assignedAt: { type: Date },
    etaMinutes: { type: Number },
    scheduledAt: { type: Date },
    pickedUpAt: { type: Date },
    deliveredAt: { type: Date },
  },
  { timestamps: true }
);

export default mongoose.model<IDelivery>(
  "Delivery",
  DeliverySchema,
  "deliveries"
);
