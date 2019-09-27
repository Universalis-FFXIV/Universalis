import { Collection, Db } from "mongodb";

import { DailyUploadStatistics } from "./models/DailyUploadStatistics";
import { RecentlyUpdated } from "./models/RecentlyUpdated";
import { WorldItemPair } from "./models/WorldItemPair";
import { WorldItemPairList } from "./models/WorldItemPairList";

export class ExtraDataManager {
    private extraDataCollection: Collection;
    private recentData: Collection;

    private dailyUploadTrackingLimit = 30;
    private maxUnsafeLoopCount = 50;
    private neverUpdatedItemsCap = 20;
    private recentlyUpdatedItemsCap = 20;

    public static async create(db: Db): Promise<ExtraDataManager> {
        const extraDataCollection = db.collection("extraData");
        await extraDataCollection.createIndexes([
            { key: { setName: 1 }, unique: true }
        ]);

        // recentData indices are created in the recent data manager
        const recentData = db.collection("recentData");

        return new ExtraDataManager(extraDataCollection, recentData);
    }

    private constructor(extraDataCollection: Collection, recentData: Collection) {
        this.extraDataCollection = extraDataCollection;
        this.recentData = recentData;
    }

    /** Return the list of the most recently updated items, or a subset of them. */
    public async getRecentlyUpdatedItems(count?: number): Promise<RecentlyUpdated> {
        if (count) count = Math.max(count, 0);
        else count = Number.MAX_VALUE;

        const query = { setName: "recentlyUpdated" };

        const data: RecentlyUpdated = await this.extraDataCollection.findOne(query, { projection: { _id: 0, setName: 0 } });

        if (count && data) data.items = data.items.slice(0, Math.min(count, data.items.length));

        return data;
    }

    /** Return the list of the least recently updated items, or a subset of them. */
    public async getLeastRecentlyUpdatedItems(count?: number): Promise<WorldItemPairList> {
        let items = (await this.getNeverUpdatedItems(count)).items;

        if (count) count = Math.max(count, 0);
        else count = Number.MAX_VALUE;

        const sortQuery = { timestamp: 1 };

        items.concat(await this.recentData.find({}, { projection: { worldID: 1, itemID: 1 } })
            .sort(sortQuery)
            .limit(Math.min(count, Math.max(0, this.recentlyUpdatedItemsCap - items.length)))
            .toArray()
        );

        return { items };
    }

    /** Return the list of items never uploaded, or a subset of them. */
    private async getNeverUpdatedItems(count?: number): Promise<WorldItemPairList> {
        if (count) count = Math.max(count, 0);
        else count = Number.MAX_VALUE;

        const items: Array<WorldItemPair> = [];

        for (let i = 0; i < this.maxUnsafeLoopCount; i++) {
            if (items.length === Math.min(count, this.neverUpdatedItemsCap)) return { items };

            const worldID = (() => {
                let number = Math.floor(Math.random() * 87) + 13;
                if (number === 26) number--;
                if (number === 27) number--;
                if (number === 38) number--;
                if (number === 84) number--;
                return number;
            })();

            const itemID = Math.floor(Math.random() * 28099) + 1;

            const randomData = await this.recentData.findOne({ worldID, itemID },
                { projection: { _id: 0, listings: 0, recentHistory: 0 } });

            if (!randomData) items.push({ worldID, itemID });
        }

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
            if (data.items.length > this.recentlyUpdatedItemsCap) {
                data.items = data.items.slice(0, this.recentlyUpdatedItemsCap);
            }

            await this.extraDataCollection.updateOne(query, {
                $set: {
                    items: data.items
                }
            });
        } else {
            await this.extraDataCollection.insertOne({
                setName: "recentlyUpdated",
                items: [itemID]
            });
        }
    }

    /** Get the daily upload statistics for the past 30 days, or a specified shorter period. */
    public async getDailyUploads(count?: number): Promise<DailyUploadStatistics> {
        if (count) count = Math.max(count, 0);
        else count = Number.MAX_VALUE;

        const query = { setName: "uploadCountHistory" };

        const data: DailyUploadStatistics = await this.extraDataCollection.findOne(query, { projection: { _id: 0, setName: 0, lastPush: 0 } });

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
                setName: "uploadCountHistory",
                lastPush: Date.now(),
                uploadCountByDay: [1]
            });
        }
    }
}
