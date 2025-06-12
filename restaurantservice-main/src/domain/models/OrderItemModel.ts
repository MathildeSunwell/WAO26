import mongoose, { Document, Schema, Model } from 'mongoose';

export interface IOrderItem extends Document {
    _id: mongoose.Types.ObjectId;
    ItemId: string;
    OrderId: string;
    ProductName: string;
    Quantity: number;
    Price: number;
}

const OrderItemSchema = new Schema<IOrderItem>({
    ItemId:       { type: String, required: true, unique: true },
    OrderId:      { type: String, required: true, index: true },
    ProductName:  { type: String, required: true },
    Quantity:     { type: Number, required: true },
    Price:        { type: Number, required: true },
}, {
    timestamps: false,
});

export const OrderItemModel: Model<IOrderItem> =
    mongoose.model<IOrderItem>('OrderItem', OrderItemSchema);
