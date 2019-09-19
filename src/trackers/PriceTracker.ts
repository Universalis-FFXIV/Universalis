import { Tracker } from "./Tracker";

import { Collection } from "mongodb";

import { MarketBoardItemListing } from "../models/MarketBoardItemListing";
import { MarketInfoLocalData } from "../models/MarketInfoLocalData";

export class PriceTracker extends Tracker {
    constructor(collection: Collection) {
        super(collection);
        (async () => {
            const indices = [
                { dcName: 1 },
                { itemID: 1 },
                { worldID: 1 }
            ];
            const indexNames = indices.map(Object.keys);
            for (let i = 0; i < indices.length; i++) {
                // We check each individually to ensure we don't duplicate indices on failure.
                if (!await this.collection.indexExists(indexNames[i])) {
                    await this.collection.createIndex(indices[i]);
                }
            }
        })();
    }

    public async set(uploaderID: string, itemID: number, worldID: number, listings: MarketBoardItemListing[]) {
        const data: MarketInfoLocalData = {
            itemID,
            lastUploadTime: Date.now(),
            listings,
            uploaderID,
            worldID
        };

        const query = { worldID, itemID };

        const existing = await this.collection.findOne(query, { projection: { _id: 0 } }) as MarketInfoLocalData;
        if (existing && existing.recentHistory) {
            data.recentHistory = existing.recentHistory;
        }

        this.updateDataCenterProperty(uploaderID, "listings", itemID, worldID, listings);

        if (existing) {
            await this.collection.updateOne(query, { $set: data });
        } else {
            await this.collection.insertOne(data);
        }
    }
}
