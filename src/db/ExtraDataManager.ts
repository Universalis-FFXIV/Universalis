import { RemoteDataManager } from "../remote/RemoteDataManager";

import { Collection, Db } from "mongodb";

import { DailyUploadStatistics } from "../models/DailyUploadStatistics";
import { MarketTaxRates } from "../models/MarketTaxRates";
import { MostPopularItems } from "../models/MostPopularItems";
import { RecentlyUpdated } from "../models/RecentlyUpdated";
import { WorldItemPair } from "../models/WorldItemPair";
import { WorldItemPairList } from "../models/WorldItemPairList";
import { WorldUploadCount } from "../models/WorldUploadCount";

export class ExtraDataManager {
    public static async create(rdm: RemoteDataManager, db: Db): Promise<ExtraDataManager> {
        const extraDataCollection = db.collection("extraData");

        const indices = [
            { setName: 1 }
        ];
        const indexNames = indices.map(Object.keys);
        for (let i = 0; i < indices.length; i++) {
            // We check each individually to ensure we don't duplicate indices on failure.
            if (!await extraDataCollection.indexExists(indexNames[i])) {
                await extraDataCollection.createIndex(indices[i]);
            }
        }

        // recentData indices are created in the recent data manager
        const recentData = db.collection("recentData");

        return new ExtraDataManager(rdm, extraDataCollection, recentData);
    }

    private extraDataCollection: Collection;
    private recentData: Collection;

    private rdm: RemoteDataManager;

    private dailyUploadTrackingLimit = 30;
    private maxUnsafeLoopCount = 50;
    private returnCap = 20;

    private constructor(rdm: RemoteDataManager, extraDataCollection: Collection, recentData: Collection) {
        this.extraDataCollection = extraDataCollection;
        this.recentData = recentData;

        this.rdm = rdm;
    }

    /** Return the number of uploads from each world. */
    public async getWorldUploadCounts(): Promise<WorldUploadCount[]> {
        const query = { setName: "worldUploadCount" };

        const data = await this.extraDataCollection.find(query, { projection: { _id: 0, setName: 0 } }).toArray();

        return data;
    }

    /** Increment the upload count for a world. */
    public async incrementWorldUploads(worldName: string): Promise<void> {
        const query = { setName: "worldUploadCount", worldName };

        const data = await this.extraDataCollection.findOne(query);

        if (data) {
            await this.extraDataCollection.updateOne(query, { $inc: { count: 1 } });
        } else {
            await this.extraDataCollection.insertOne(<WorldUploadCount> {
                count: 0,
                setName: "worldUploadCount",
                worldName
            });
        }
    }

    /** Return the list of the most popular items, or a subset of them. */
    public async getPopularItems(count?: number): Promise<MostPopularItems> {
        if (count) count = Math.max(count, 0);
        else count = Number.MAX_VALUE;

        const query = { setName: "itemPopularity" };

        const data: MostPopularItems =
            await this.extraDataCollection.findOne(query, { projection: { _id: 0, setName: 0 } });

        if (count && data) data.items = data.items.slice(0, Math.min(count, data.items.length));

        return data;
    }

    /** Increment the popular upload count for an item. */
    public async incrementPopularUploads(itemID: number): Promise<void> {
        const query = { setName: "itemPopularity", itemID };

        const data = await this.extraDataCollection.findOne(query);

        if (data) {
            await this.extraDataCollection.updateOne(query, { $inc: { "internal.uploadCount": 1 } });
        } else {
            await this.extraDataCollection.insertOne(<MostPopularItems> {
                internal: {
                    itemID,
                    uploadCount: 1
                },
                setName: "itemPopularity"
            });
        }
    }

    /** Return the list of the most recently updated items, or a subset of them. */
    public async getRecentlyUpdatedItems(count?: number): Promise<RecentlyUpdated> {
        if (count) count = Math.max(count, 0);
        else count = Number.MAX_VALUE;

        const query = { setName: "recentlyUpdated" };

        const data: RecentlyUpdated =
            await this.extraDataCollection.findOne(query, { projection: { _id: 0, setName: 0 } });

        if (count && data) data.items = data.items.slice(0, Math.min(count, data.items.length));

        return data;
    }

    /** Return the list of the least recently updated items, or a subset of them. */
    public async getLeastRecentlyUpdatedItems(worldDC?: string | number, count?: number): Promise<WorldItemPairList> {
        let items = (await this.getNeverUpdatedItems(worldDC, count)).items;

        if (count) count = Math.max(count, 0);
        else count = Number.MAX_VALUE;

        const query: any = {};

        if (typeof worldDC === "number") query.worldID = worldDC;
        else if (typeof worldDC === "string") query.dcName = worldDC;

        if (items.length < 20) items.concat(await this.recentData
            .find(query, { projection: { _id: 0, listings: 0, recentHistory: 0 } })
            .limit(Math.min(count, Math.max(0, this.returnCap - items.length)))
            .sort({ timestamp: 1 })
            .toArray()
        );

        // Uninitialized items won't have a timestamp in the first place.
        items = items.map((item) => {
            if (!item.timestamp) {
                item.timestamp = 0;
            }
            return item;
        });

        return { items };
    }

    /** Add to the list of the most recently updated items. */
    public async addRecentlyUpdatedItem(itemID: number): Promise<void> {
        const query = { setName: "recentlyUpdated" };

        const data: RecentlyUpdated = await this.extraDataCollection.findOne(query);

        if (data) {
            if (data.items.indexOf(itemID) !== -1) {
                // Shift the item ID to the front if it's already in the "most recent" list
                data.items.splice(data.items.indexOf(itemID), 1);
            }

            data.items = [itemID].concat(data.items);

            // Limit size
            if (data.items.length > this.returnCap) {
                data.items = data.items.slice(0, this.returnCap);
            }

            await this.extraDataCollection.updateOne(query, {
                $set: {
                    items: data.items
                }
            });
        } else {
            await this.extraDataCollection.insertOne({
                items: [itemID],
                setName: "recentlyUpdated"
            });
        }
    }

    /** Get the tax rates of all cities. */
    public async getTaxRates(worldID: number): Promise<MarketTaxRates> {
        const query = { setName: "taxRates", worldID };

        const data: MarketTaxRates =
            await this.extraDataCollection.findOne(query, { projection: { _id: 0, setName: 0, uploaderID: 0, sourceName: 0 } });

        return data;
    }

    /** Set the market tax rates. */
    public async setTaxRates(uploaderID: string | number, sourceName: string, worldID: number, tx: MarketTaxRates): Promise<void> {
        if (!uploaderID || !worldID) return;

        tx["uploaderID"] = uploaderID;
        tx["sourceName"] = sourceName;
        const query = { setName: "taxRates", worldID };

        const data: MarketTaxRates = await this.extraDataCollection.findOne(query);

        if (data) {
            await this.extraDataCollection.updateOne(query, {
                $set: tx
            });
        } else {
            tx["setName"] = "taxRates";
            tx["worldID"] = worldID;
            await this.extraDataCollection.insertOne(tx);
        }
    }

    /** Get the daily upload statistics for the past 30 days, or a specified shorter period. */
    public async getDailyUploads(count?: number): Promise<DailyUploadStatistics> {
        if (count) count = Math.max(count, 0);
        else count = Number.MAX_VALUE;

        const query = { setName: "uploadCountHistory" };

        const data: DailyUploadStatistics = await this.extraDataCollection.findOne(query, {
            projection: {
                _id: 0,
                lastPush: 0,
                setName: 0
            }
        });

        if (data && data.uploadCountByDay.length < this.dailyUploadTrackingLimit) {
            data.uploadCountByDay = data.uploadCountByDay.concat(
                (new Array(this.dailyUploadTrackingLimit - data.uploadCountByDay.length))
                .fill(0)
            );
        }

        if (count && data) {
            data.uploadCountByDay = data.uploadCountByDay.slice(0, Math.min(count, data.uploadCountByDay.length));
        }

        return data;
    }

    /** Increment the recorded uploads for today. */
    public async incrementDailyUploads(): Promise<void> {
        const query = { setName: "uploadCountHistory" };

        const data: DailyUploadStatistics = await this.extraDataCollection.findOne(query);

        if (data) {
            if (Date.now() - data.lastPush > 86400000) {
                data.lastPush = Date.now();
                data.uploadCountByDay = [0].concat(data.uploadCountByDay.slice(0, data.uploadCountByDay.length - 1));
            }

            data.uploadCountByDay[0]++;
            await this.extraDataCollection.updateOne(query, {
                $set: {
                    lastPush: data.lastPush,
                    uploadCountByDay: data.uploadCountByDay
                }
            });
        } else {
            await this.extraDataCollection.insertOne({
                lastPush: Date.now(),
                setName: "uploadCountHistory",
                uploadCountByDay: [1]
            });
        }
    }

    /** Return the list of items never uploaded, or a subset of them. */
    private async getNeverUpdatedItems(worldDC?: string | number, count?: number): Promise<WorldItemPairList> {
        if (count) count = Math.max(count, 0);
        else count = Number.MAX_VALUE;

        const items: WorldItemPair[] = [];

        for (let i = 0; i < this.maxUnsafeLoopCount; i++) {
            if (items.length === Math.min(count, this.returnCap)) return { items };

            // Random world ID
            const worldID = (() => {
                let num = Math.floor(Math.random() * 87) + 13;
                if (num === 26) num--;
                if (num === 27) num--;
                if (num === 38) num--;
                if (num === 84) num--;
                return num;
            })();

            // Item ID
            const itemIDs = await this.rdm.getMarketableItemIDs();
            const itemID = itemIDs[Math.floor(Math.random() * itemIDs.length)];

            // DB query
            const query: any = { itemID };
            if (typeof worldDC === "number") query.worldID = worldDC;
            else if (typeof worldDC === "string") query.dcName = worldDC;
            else query.worldID = worldID;

            const randomData = await this.recentData.findOne(query,
                { projection: { _id: 0, listings: 0, recentHistory: 0 } });
            if (!randomData) items.push(query);
        }

        return { items };
    }
}
