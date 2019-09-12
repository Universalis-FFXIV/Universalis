import { Collection } from "mongodb";
import { CronJob } from "cron";

import { DailyUploadStatistics } from "./models/DailyUploadStatistics";
import { RecentlyUpdated } from "./models/RecentlyUpdated";

export class ExtraDataManager {
    private extraDataCollection: Collection;

    private shiftDailyUploadDayJob: CronJob;

    private dailyUploadTrackingLimit: number;
    private recentlyUpdatedItemsCap: number;

    constructor(extraDataCollection: Collection) {
        this.extraDataCollection = extraDataCollection;

        this.shiftDailyUploadDayJob = new CronJob("* * * 0 0", this.shiftDailyUploadDay, null, true);

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
        const query = { setName: "uploadCountHistory" };

        const data: DailyUploadStatistics = await this.extraDataCollection.findOne(query, { projection: { _id: 0 } });

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

    /** Drop a day, push today back, add a new today. */
    private async shiftDailyUploadDay(): Promise<void> {
        const query = { setName: "uploadCountHistory" };

        const data: DailyUploadStatistics = await this.extraDataCollection.findOne(query);

        if (data) {
            if (data.uploadCountByDay.length === this.dailyUploadTrackingLimit) {
                data.uploadCountByDay.pop();
                data.uploadCountByDay.push(0);
            } else {
                data.uploadCountByDay.push(0);
            }

            await this.extraDataCollection.updateOne(query, {
                $set: {
                    uploadCountByDay: data.uploadCountByDay
                }
            });
        } else {
            await this.extraDataCollection.insertOne({
                setName: "uploadCountHistory",
                uploadCountByDay: [0]
            });
        }
    }
}
