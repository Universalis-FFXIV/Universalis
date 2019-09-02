import fs from "fs";
import path from "path";
import util from "util";

import remoteDataManager from "../remoteDataManager";

import { Tracker } from "./Tracker";

import { ExtendedDCHistory } from "../models/ExtendedDCHistory";
import { ExtendedHistory } from "../models/ExtendedHistory";
import { MarketBoardDCHistoryEntry } from "../models/MarketBoardDCHistoryEntry";
import { MarketBoardHistoryEntry } from "../models/MarketBoardHistoryEntry";
import { MarketInfoDCLocalData } from "../models/MarketInfoDCLocalData";
import { MarketInfoLocalData } from "../models/MarketInfoLocalData";
import { MinimizedDCHistoryEntry } from "../models/MinimizedDCHistoryEntry";
import { MinimizedHistoryEntry } from "../models/MinimizedHistoryEntry";

const exists = util.promisify(fs.exists);
const mkdir = util.promisify(fs.mkdir);
const readFile = util.promisify(fs.readFile);
const unlink = util.promisify(fs.unlink);
const writeFile = util.promisify(fs.writeFile);

export class HistoryTracker extends Tracker {
    // The path structure is /listings/<worldID/<itemID>/<branchNumber>.json
    constructor() {
        super("../../data", ".json");

        const worlds = fs.readdirSync(path.join(__dirname, this.storageLocation));
        for (let world of worlds) {
            const items = fs.readdirSync(path.join(__dirname, this.storageLocation, world));
            for (let item of items) {
                let listings: MarketInfoLocalData;
                try {
                    listings = JSON.parse(
                        fs.readFileSync(
                            path.join(__dirname, this.storageLocation, world, item, "0.json")
                        ).toString()
                    );
                } catch {
                    continue;
                }

                this.data.set(parseInt(item), { worldID: listings.worldID, data: listings.listings });
            }
        }
    }

    public async set(itemID: number, worldID: number, recentHistory: MarketBoardHistoryEntry[]) {
        const worldDir = path.join(__dirname, this.storageLocation, String(worldID));
        const itemDir = path.join(worldDir, String(itemID));
        const filePath = path.join(itemDir, "0.json");
        // const listings = (await readdir(filePath)).filter((el) => el.endsWith(".json"));

        if (!await exists(worldDir)) {
            await mkdir(worldDir);
        }

        if (!await exists(itemDir)) {
            await mkdir(itemDir);
        }

        let data: MarketInfoLocalData = {
            itemID,
            recentHistory,
            worldID
        };

        if (await exists(filePath)) {
            data.listings = JSON.parse((await readFile(filePath)).toString()).listings;
        }

        this.updateDataCenterHistory(itemID, worldID, recentHistory);
        this.updateExtendedDCHistory(itemID, worldID, recentHistory);
        this.updateExtendedHistory(itemID, worldID, recentHistory);

        await writeFile(filePath, JSON.stringify(data));

        /*const nextNumber = parseInt(
            listings[listings.length - 1].substr(0, listings[listings.length - 1].indexOf("."))
        ) + 1;

        await writeFile(path.join(filePath, `${nextNumber}.json`), JSON.stringify({
            listings: data,
        }));*/
    }

    private async updateDataCenterHistory(itemID: number, worldID: number, entries: any[]) {
        const dataCenterWorlds = JSON.parse((await remoteDataManager.fetchFile("dc.json")).toString());
        const worldCSV = (await remoteDataManager.parseCSV("World.csv")).slice(3);
        const world = worldCSV.find((line) => line[0] === String(worldID))[1];

        (entries as MarketBoardDCHistoryEntry[]).forEach((entry) => entry.worldName = world);

        let dataCenter: string;
        for (let dc in dataCenterWorlds) {
            if (dataCenterWorlds.hasOwnProperty(dc)) {
                let foundWorld = dataCenterWorlds[dc].find((el) => el === world);
                if (foundWorld) dataCenter = dc;
            }
        }

        const dcDir = path.join(__dirname, "../../data", String(dataCenter));
        const itemDir = path.join(dcDir, String(itemID));
        const filePath = path.join(itemDir, "0.json");

        if (!await exists(dcDir)) {
            await mkdir(dcDir);
        }

        if (!await exists(itemDir)) {
            await mkdir(itemDir);
        }

        let existingData: MarketInfoDCLocalData;
        if (await exists(filePath)) existingData = JSON.parse((await readFile(filePath)).toString());
        if (existingData && existingData.recentHistory) {
            existingData.recentHistory = existingData.recentHistory.filter((entry) => entry.worldName !== world);

            existingData.recentHistory = existingData.recentHistory.concat(entries);

            existingData.recentHistory = existingData.recentHistory.sort((a, b) => {
                if (a.pricePerUnit > b.pricePerUnit) return -1;
                if (a.pricePerUnit < b.pricePerUnit) return 1;
                return 0;
            });
        } else {
            if (!existingData) {
                existingData = {
                    dcName: dataCenter,
                    itemID
                };
            }

            existingData.recentHistory = entries;
        }

        return await writeFile(filePath, JSON.stringify(existingData));
    }

    private async updateExtendedHistory(itemID: number, worldID: number, entries: MarketBoardHistoryEntry[]) {
        const worldDir = path.join(__dirname, "../../history", String(worldID));
        const itemDir = path.join(worldDir, String(itemID));
        const extendedHistoryPath = path.join(itemDir, "0.json");

        if (!await exists(worldDir)) {
            await mkdir(worldDir);
        }

        if (!await exists(itemDir)) {
            await mkdir(itemDir);
        }

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

        let entrySum = extendedHistory.entries.length + minimizedEntries.length;
        if (entrySum > 500) {
            extendedHistory.entries = extendedHistory.entries.slice(0, 500 - minimizedEntries.length);
        }
        extendedHistory.entries = minimizedEntries.concat(extendedHistory.entries);
        return await writeFile(extendedHistoryPath, JSON.stringify(extendedHistory));
    }

    private async updateExtendedDCHistory(itemID: number, worldID: number, entries: MarketBoardHistoryEntry[]) {
        const dataCenterWorlds = JSON.parse((await remoteDataManager.fetchFile("dc.json")).toString());
        const worldCSV = (await remoteDataManager.parseCSV("World.csv")).slice(3);
        const world = worldCSV.find((line) => line[0] === String(worldID))[1];

        (entries as MarketBoardDCHistoryEntry[]).forEach((entry) => entry.worldName = world);

        let dataCenter: string;
        for (let dc in dataCenterWorlds) {
            if (dataCenterWorlds.hasOwnProperty(dc)) {
                let foundWorld = dataCenterWorlds[dc].find((el) => el === world);
                if (foundWorld) dataCenter = dc;
            }
        }

        const dcDir = path.join(__dirname, "../../history", String(dataCenter));
        const itemDir = path.join(dcDir, String(itemID));
        const extendedHistoryPath = path.join(itemDir, "0.json");

        if (!await exists(dcDir)) {
            await mkdir(dcDir);
        }

        if (!await exists(itemDir)) {
            await mkdir(itemDir);
        }

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
        if (extendedHistory && extendedHistory.entries) {
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
