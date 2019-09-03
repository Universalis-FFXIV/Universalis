import fs from "fs";
import path from "path";
import util from "util";

import { ensurePathsExist, getWorldDC, getWorldName } from "../util";

import { Tracker } from "./Tracker";

import { ExtendedDCHistory } from "../models/ExtendedDCHistory";
import { ExtendedHistory } from "../models/ExtendedHistory";
import { MarketBoardDCHistoryEntry } from "../models/MarketBoardDCHistoryEntry";
import { MarketBoardHistoryEntry } from "../models/MarketBoardHistoryEntry";
import { MarketInfoLocalData } from "../models/MarketInfoLocalData";
import { MinimizedDCHistoryEntry } from "../models/MinimizedDCHistoryEntry";
import { MinimizedHistoryEntry } from "../models/MinimizedHistoryEntry";

const exists = util.promisify(fs.exists);
const readFile = util.promisify(fs.readFile);
const writeFile = util.promisify(fs.writeFile);

export class HistoryTracker extends Tracker {
    // The path structure is /listings/<worldID/<itemID>/<branchNumber>.json
    constructor() {
        super("../../data", ".json");
    }

    public async set(itemID: number, worldID: number, recentHistory: MarketBoardHistoryEntry[]) {
        const worldDir = path.join(__dirname, this.storageLocation, String(worldID));
        const itemDir = path.join(worldDir, String(itemID));
        const filePath = path.join(itemDir, "0.json");
        // const listings = (await readdir(filePath)).filter((el) => el.endsWith(".json"));

        await ensurePathsExist(worldDir, itemDir);

        let data: MarketInfoLocalData = {
            itemID,
            recentHistory,
            worldID
        };

        if (await exists(filePath)) {
            data.listings = JSON.parse((await readFile(filePath)).toString()).listings;
        }

        this.updateDataCenterProperty("recentHistory", itemID, worldID, recentHistory);
        this.updateExtendedDCHistory(itemID, worldID, recentHistory);
        this.updateExtendedHistory(itemID, worldID, recentHistory);

        await writeFile(filePath, JSON.stringify(data));
    }

    private async updateExtendedHistory(itemID: number, worldID: number, entries: MarketBoardHistoryEntry[]) {
        const worldDir = path.join(__dirname, "../../history", String(worldID));
        const itemDir = path.join(worldDir, String(itemID));
        const extendedHistoryPath = path.join(itemDir, "0.json");

        await ensurePathsExist(worldDir, itemDir);

        // Cut out any properties we don't need
        let minimizedEntries: MinimizedHistoryEntry[] = entries.map((entry) => {
            return {
                hq: entry.hq,
                pricePerUnit: entry.pricePerUnit,
                timestamp: entry.timestamp
            };
        });

        let extendedHistory: ExtendedHistory;
        if (await exists(extendedHistoryPath)) {
            extendedHistory = JSON.parse((await readFile(extendedHistoryPath)).toString());
        } else {
            extendedHistory = {
                entries: []
            };
        }

        // Limit to 500 entries
        let entrySum = extendedHistory.entries.length + minimizedEntries.length;
        if (entrySum > 500) {
            extendedHistory.entries = extendedHistory.entries.slice(0, 500 - minimizedEntries.length);
        }
        extendedHistory.entries = minimizedEntries.concat(extendedHistory.entries);
        return await writeFile(extendedHistoryPath, JSON.stringify(extendedHistory));
    }

    private async updateExtendedDCHistory(itemID: number, worldID: number, entries: MarketBoardHistoryEntry[]) {
        const world = await getWorldName(worldID);
        const dataCenter = await getWorldDC(world);

        // Append world name to each entry
        (entries as MarketBoardDCHistoryEntry[]).forEach((entry) => entry.worldName = world);

        const dcDir = path.join(__dirname, "../../history", String(dataCenter));
        const itemDir = path.join(dcDir, String(itemID));
        const extendedHistoryPath = path.join(itemDir, "0.json");

        await ensurePathsExist(dcDir, itemDir);

        let minimizedEntries: MinimizedDCHistoryEntry[] = entries.map((entry) => {
            return {
                hq: entry.hq,
                pricePerUnit: entry.pricePerUnit,
                timestamp: entry.timestamp,
                worldName: world
            };
        });

        let extendedHistory: ExtendedDCHistory;
        if (await exists(extendedHistoryPath))
            extendedHistory = JSON.parse((await readFile(extendedHistoryPath)).toString());
        if (extendedHistory && extendedHistory.entries) { // Delete entries from the upload world
            extendedHistory.entries = extendedHistory.entries.filter((entry) => entry.worldName !== world);

            extendedHistory.entries = extendedHistory.entries.concat(minimizedEntries);
        } else {
            extendedHistory = {
                entries: []
            };
        }

        let entrySum = extendedHistory.entries.length + minimizedEntries.length;
        if (entrySum > 500) {
            extendedHistory.entries = extendedHistory.entries.slice(0, 500 - minimizedEntries.length);
        }

        return await writeFile(extendedHistoryPath, JSON.stringify(extendedHistory));
    }
}
