import { Document, Model, FilterQuery, UpdateQuery, QueryOptions } from 'mongoose';

export class Repository<T extends Document> {
    protected model: Model<T>;

    constructor(model: Model<T>) {
        this.model = model;
    }

    async create(data: Partial<T>): Promise<T> {
        return this.model.create(data);
    }

    async find(filter: FilterQuery<T> = {}, options: QueryOptions = {}): Promise<T[]> {
        return this.model.find(filter, null, options).exec();
    }

    async updateByOrderId(
        id: string,
        update: UpdateQuery<T>,
        options: QueryOptions = { new: true }
    ): Promise<T | null> {
        return this.model.findOneAndUpdate({ OrderId: id }, update, options).exec();
    }
}
