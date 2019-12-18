import compact from "lodash.compact";
import isEqual from "lodash.isequal";

import { getWorldDC, getWorldName } from "../util";

import { Tracker } from "./Tracker";

import { Collection, Db } from "mongodb";

import { ExtendedDCHistory } from "../models/ExtendedDCHistory";
import { ExtendedHistory } from "../models/ExtendedHistory";
import { MarketBoardDCHistoryEntry } from "../models/MarketBoardDCHistoryEntry";
import { MarketBoardHistoryEntry } from "../models/MarketBoardHistoryEntry";
import { MarketInfoLocalData } from "../models/MarketInfoLocalData";
import { MinimizedDCHistoryEntry } from "../models/MinimizedDCHistoryEntry";
import { MinimizedHistoryEntry } from "../models/MinimizedHistoryEntry";

export class HistoryTracker extends Tracker {
    public static async create(db: Db): Promise<HistoryTracker> {
        const recentData = db.collection("recentData");
        const extendedHistory = db.collection("extendedHistory");

        const indices = [
            { dcName: 1 },
            { itemID: 1 },
            { worldID: 1 }
        ];
        const indexNames = indices.map(Object.keys);
        for (let i = 0; i < indices.length; i++) {
            // We check each individually to ensure we don't duplicate indices on failure.
            if (!await extendedHistory.indexExists(indexNames[i])) {
                await extendedHistory.createIndex(indices[i]);
            }
        }

        return new HistoryTracker(recentData, extendedHistory);
    }

    private extendedHistory: Collection;

    private constructor(recentData: Collection, extendedHistory: Collection) {
        super(recentData);
        this.extendedHistory = extendedHistory;
    }

    public async set(uploaderID: string, itemID: number, worldID: number, recentHistory: MarketBoardHistoryEntry[]) {
        if (!recentHistory) return; // This should never be empty.

        const data: MarketInfoLocalData = {
            itemID,
            lastUploadTime: Date.now(),
            recentHistory,
            uploaderID,
            worldID
        };

        const query = { worldID, itemID };

        const existing = await this.collection.findOne(query, { projection: { _id: 0 } }) as MarketInfoLocalData;
        if (existing && existing.listings) {
            data.listings = existing.listings;
        }

        this.updateDataCenterProperty(uploaderID, "recentHistory", itemID, worldID, recentHistory);
        const existingExtendedHistory = await this.updateExtendedHistory(uploaderID, itemID, worldID, recentHistory);
        this.updateExtendedDCHistory(uploaderID, itemID, worldID, existingExtendedHistory.entries);

        if (existing) {
            await this.collection.updateOne(query, { $set: data });
        } else {
            await this.collection.insertOne(data);
        }
    }

    private async updateExtendedHistory(uploaderID: string, itemID: number, worldID: number,
                                        entries: MarketBoardHistoryEntry[]) {
        // Cut out any properties we don't need
        let minimizedEntries: MinimizedHistoryEntry[] = entries.map((entry) => {
            return {
                hq: entry.hq,
                pricePerUnit: entry.pricePerUnit,
                quantity: entry.quantity,
                timestamp: entry.timestamp,
                uploaderID
            };
        });

        const query = { worldID, itemID };

        const existing = await this.extendedHistory.findOne(query, { projection: { _id: 0 } }) as ExtendedHistory;

        let extendedHistory: ExtendedHistory;
        if (existing) {
            extendedHistory = existing;

            minimizedEntries = compact(minimizedEntries.map((entry) => {
                if (extendedHistory.entries.some((ex) => {
                    return ex.hq === entry.hq &&
                           ex.pricePerUnit === entry.pricePerUnit &&
                           ex.timestamp === entry.timestamp;
                })) {
                    return;
                }
                return entry;
            }));
        } else {
            extendedHistory = {
                entries: [],
                itemID,
                lastUploadTime: Date.now(),
                worldID
            };
        }

        // Limit to 500 entries
        const entrySum = extendedHistory.entries.length + minimizedEntries.length;
        if (entrySum > 500) {
            extendedHistory.entries = extendedHistory.entries.slice(0, 500 - minimizedEntries.length);
        }

        extendedHistory.entries = minimizedEntries.concat(extendedHistory.entries);

        if (existing) {
            await this.extendedHistory.updateOne(query, { $set: extendedHistory });
        } else {
            await this.extendedHistory.insertOne(extendedHistory);
        }

        return extendedHistory;
    }

    private async updateExtendedDCHistory(uploaderID: string, itemID: number, worldID: number,
                                          entries: MinimizedHistoryEntry[]) {
        const world = await getWorldName(worldID);
        const dcName = await getWorldDC(world);

        // Append world name to each entry
        (entries as MarketBoardDCHistoryEntry[]).forEach((entry) => entry.worldName = world);

        let minimizedEntries: MinimizedDCHistoryEntry[] = entries.map((entry) => {
            return {
                hq: entry.hq,
                pricePerUnit: entry.pricePerUnit,
                quantity: entry.quantity,
                timestamp: entry.timestamp,
                uploaderID,
                worldName: world
            };
        });

        const query = { dcName, itemID };

        const existing = await this.extendedHistory.findOne(query, { projection: { _id: 0 } }) as ExtendedDCHistory;

        let extendedHistory: ExtendedDCHistory;
        if (existing) extendedHistory = existing;
        if (extendedHistory && extendedHistory.entries) { // Delete entries from the upload world
            extendedHistory.entries = extendedHistory.entries.filter((entry) => entry.worldName !== world);

            minimizedEntries = compact(minimizedEntries.map((entry) => {
                if (extendedHistory.entries.some((ex) => {
                    return ex.hq === entry.hq &&
                           ex.pricePerUnit === entry.pricePerUnit &&
                           ex.timestamp === entry.timestamp;
                })) {
                    return;
                }
                return entry;
            }));

            extendedHistory.entries = extendedHistory.entries.concat(minimizedEntries);
        } else {
            extendedHistory = {
                dcName,
                entries: [],
                itemID,
                lastUploadTime: Date.now()
            };
        }

        const entrySum = extendedHistory.entries.length + minimizedEntries.length;
        if (entrySum > 500) {
            extendedHistory.entries = extendedHistory.entries.slice(0, 500 - minimizedEntries.length);
        }

        if (existing) {
            return await this.extendedHistory.updateOne(query, { $set: extendedHistory });
        } else {
            return await this.extendedHistory.insertOne(extendedHistory);
        }
    }
}
