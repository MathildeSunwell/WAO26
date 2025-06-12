import { UpdateQuery, QueryOptions } from 'mongoose';
import { Repository } from './repository.js';
import { IOrder, OrderModel } from '../domain/models/OrderModel.js';

export class OrderRepository extends Repository<IOrder> {
    async addItem(orderId: string, itemId: string) {
        return this.updateByOrderId(
            orderId,
            { $push: { OrderItems: itemId } } as UpdateQuery<IOrder>,
            { new: true }
        );
    }

    async removeItem(orderId: string, itemId: string) {
        return this.updateByOrderId(
            orderId,
            { $pull: { OrderItems: itemId } } as UpdateQuery<IOrder>,
            { new: true }
        );
    }

    async updateFields(
        orderId: string,
        update: UpdateQuery<IOrder>,
        options: QueryOptions = { new: true }
    ) {
        return this.updateByOrderId(orderId, update, options);
    }
}

export const orderRepository = new OrderRepository(OrderModel);
