import { Collection } from "mongodb";

import { RecentlyUpdated } from "./models/RecentlyUpdated";

export class ExtraDataManager {
    public recentlyUpdatedItemsCap: number;

    private extraDataCollection: Collection;

    constructor(extraDataCollection: Collection, recentlyUpdatedItemsCap?: number) {
        this.extraDataCollection = extraDataCollection;

        if (recentlyUpdatedItemsCap) {
            this.recentlyUpdatedItemsCap = recentlyUpdatedItemsCap;
        } else {
            this.recentlyUpdatedItemsCap = Math.min(recentlyUpdatedItemsCap, 20);
        }
    }

    /** Return the list of the most recently updated items, or a subset of them. */
    async getRecentlyUpdatedItems(count?: number): Promise<RecentlyUpdated> {
        const query = { set: "recentlyUpdated" };

        const data: RecentlyUpdated = await this.extraDataCollection.findOne(query, { projection: { _id: 0 } });

        if (count) data.items = data.items.slice(0, Math.min(count, data.items.length));

        return data;
    }

    /** Add to the list of the most recently updated items. */
    async addRecentlyUpdatedItem(itemID: number): Promise<void> {
        const query = { set: "recentlyUpdated" };

        const data: RecentlyUpdated = await this.extraDataCollection.findOne(query, { projection: { set: 1 } });

        if (data) {
            if (data.items.indexOf(itemID) === -1) {
                // If the existing array does not contain the item ID, concat the item with the rest of the array.
                data.items = [itemID].concat(data.items.slice(1, data.items.length));
            } else {
                // If the existing array does contain the item ID, splice it out and move it to the front.
                data.items.splice(data.items.indexOf(itemID), 1);
                data.items = [itemID].concat(data.items);
            }

            // Limit size
            if (data.items.length > 20) data.items = data.items.slice(0, 20);

            await this.extraDataCollection.updateOne(query, {
                set: "recentlyUpdated",
                items: data.items
            });
        } else {
            await this.extraDataCollection.insertOne({
                set: "recentlyUpdated",
                items: [itemID]
            });
        }
    }
}
