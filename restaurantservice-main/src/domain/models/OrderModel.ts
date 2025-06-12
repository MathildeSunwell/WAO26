import mongoose, { Document, Schema, Model } from 'mongoose';
import {OrderStatus} from "../enums/OrderStatus.js";

export interface IOrder extends Document {
    _id: mongoose.Types.ObjectId;
    OrderId: string;
    OrderStatus: OrderStatus;
    EstimatedPrepTime: number;
    CreateTime: Date;
    LastUpdated: Date;
    CorrelationId: string;
    OrderItems: mongoose.Types.ObjectId[];
}

const OrderSchema = new Schema<IOrder>({
    OrderId:           { type: String, required: true, unique: true },
    OrderStatus:       { type: String, required: true, enum: Object.values(OrderStatus), default: OrderStatus.Pending},
    EstimatedPrepTime: { type: Number, required: true },
    CorrelationId:     { type: String, required: true, index: true },
    OrderItems:        [{ type: Schema.Types.ObjectId, ref: 'OrderItem' }],
}, {
    timestamps: {
        createdAt: 'CreateTime',
        updatedAt: 'LastUpdated'
    }
});

export const OrderModel: Model<IOrder> =
    mongoose.model<IOrder>('Order', OrderSchema);
