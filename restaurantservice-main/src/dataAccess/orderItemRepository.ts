import { v4 as uuidv4 } from 'uuid';
import { Repository } from './repository.js';
import { IOrderItem, OrderItemModel } from '../domain/models/OrderItemModel.js';

export class OrderItemRepository extends Repository<IOrderItem> {
    async createItem(params: {
        OrderId: string;
        ProductName: string;
        Quantity: number;
        Price: number;
    }): Promise<IOrderItem> {
        const data: Partial<IOrderItem> = {
            ItemId: uuidv4(),
            OrderId: params.OrderId,
            ProductName: params.ProductName,
            Quantity: params.Quantity,
            Price: params.Price,
        };
        return this.create(data);
    }
}

export const orderItemRepository = new OrderItemRepository(OrderItemModel);
