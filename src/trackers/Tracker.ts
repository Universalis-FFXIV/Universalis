import { CronJob } from "cron";

import { getWorldDC, getWorldName } from "../util";

import { Collection } from "mongodb";

import { MarketInfoDCLocalData } from "../models/MarketInfoDCLocalData";

export abstract class Tracker {
    protected collection: Collection;
    private scoringJob: CronJob;

    constructor(collection: Collection) {
        this.collection = collection;
        // this.scoringJob = new CronJob("* * * * */5", this.scoreAndUpdate, null, true);
    }

    public abstract set(...params);

    protected async updateDataCenterProperty(uploaderID: string, property: string, itemID: number,
                                             worldID: number, propertyData: any[]) {
        const world = await getWorldName(worldID);
        const dcName = await getWorldDC(world);

        propertyData.forEach((entry) => {
            entry.uploaderID = uploaderID;
            entry.worldName = world;
        });

        const query = { dcName, itemID };

        const existing = await this.collection.findOne(query, { projection: { _id: 0 } });

        let data: MarketInfoDCLocalData;
        if (existing) data = existing;
        if (data && data[property]) {
            data[property] = data[property].filter((entry) => entry.worldName !== world);

            data[property] = data[property].concat(propertyData);

            data[property] = data[property].sort((a, b) => {
                if (a.pricePerUnit > b.pricePerUnit) return -1;
                if (a.pricePerUnit < b.pricePerUnit) return 1;
                return 0;
            });
        } else {
            if (!data) {
                data = {
                    dcName,
                    itemID,
                    lastUploadTime: Date.now()
                };
            }

            data[property] = propertyData;
        }

        if (existing) {
            return await this.collection.updateOne(query, { $set: data });
        } else {
            return await this.collection.insertOne(data);
        }
    }
}
