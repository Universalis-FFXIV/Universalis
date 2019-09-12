import { Collection } from "mongodb";
import { CronJob } from "cron";

import { DailyUploadStatistics } from "./models/DailyUploadStatistics";
import { RecentlyUpdated } from "./models/RecentlyUpdated";

export class ExtraDataManager {
    private extraDataCollection: Collection;

    private dailyUploadTrackingLimit: number;
    private recentlyUpdatedItemsCap: number;

    constructor(extraDataCollection: Collection) {
        this.extraDataCollection = extraDataCollection;

        this.dailyUploadTrackingLimit = 30;
        this.recentlyUpdatedItemsCap = 20;
    }

    /** Return the list of the most recently updated items, or a subset of them. */
    public async getRecentlyUpdatedItems(count?: number): Promise<RecentlyUpdated> {
        const query = { setName: "recentlyUpdated" };

        const data: RecentlyUpdated = await this.extraDataCollection.findOne(query, { projection: { _id: 0 } });

        if (count) data.items = data.items.slice(0, Math.min(count, data.items.length));

        return data;
    }

    /** Add to the list of the most recently updated items. */
    public async addRecentlyUpdatedItem(itemID: number): Promise<void> {
        const query = { setName: "recentlyUpdated" };

        const data: RecentlyUpdated = await this.extraDataCollection.findOne(query);

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
            if (data.items.length > this.recentlyUpdatedItemsCap) {
                data.items = data.items.slice(0, this.recentlyUpdatedItemsCap);
            }

            await this.extraDataCollection.updateOne(query, {
                setName: "recentlyUpdated",
                items: data.items
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
        const query = { setName: "uploadCountHistory" };

        const data: DailyUploadStatistics = await this.extraDataCollection.findOne(query);

        if (count) {
            data.uploadCountByDay = data.uploadCountByDay.slice(0, Math.min(count, data.uploadCountByDay.length));
        }

        return data;
    }

    /** Increment the recorded uploads for today. */
    public async incrementDailyUploads(): Promise<void> {
        const query = { setName: "uploadCountHistory" };

        const data: DailyUploadStatistics = await this.extraDataCollection.findOne(query);

        if (data) {
            data.uploadCountByDay[data.uploadCountByDay.length - 1]++;
            await this.extraDataCollection.updateOne(query, {
                $set: {
                    uploadCountByDay: data.uploadCountByDay
                }
            });
        } else {
            await this.extraDataCollection.insertOne({
                setName: "uploadCountHistory",
                uploadCountByDay: [1]
            });
        }
    }
}
