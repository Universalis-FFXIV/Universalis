import { Tracker } from "./Tracker";

import { Collection } from "mongodb";

import { MarketBoardItemListing } from "../models/MarketBoardItemListing";
import { MarketInfoLocalData } from "../models/MarketInfoLocalData";

export class PriceTracker extends Tracker {
    constructor(collection: Collection) {
        super(collection);
    }

    public async set(uploaderID: string, itemID: number, worldID: number, listings: MarketBoardItemListing[]) {
        let data: MarketInfoLocalData = {
            itemID,
            listings,
            uploaderID,
            worldID
        };

        const query = { worldID, itemID };

        const existing = await this.collection.findOne(query) as MarketInfoLocalData;
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
