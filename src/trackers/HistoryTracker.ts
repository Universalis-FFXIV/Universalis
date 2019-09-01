import fs from "fs";
import path from "path";
import util from "util";

import { Tracker } from "./Tracker";

import { ExtendedHistory } from "../models/ExtendedHistory";
import { MarketBoardHistoryEntry } from "../models/MarketBoardHistoryEntry";
import { MarketInfoLocalData } from "../models/MarketInfoLocalData";
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

        await writeFile(filePath, JSON.stringify(data));

        this.updateExtendedHistory(itemID, worldID, recentHistory);

        /*const nextNumber = parseInt(
            listings[listings.length - 1].substr(0, listings[listings.length - 1].indexOf("."))
        ) + 1;

        await writeFile(path.join(filePath, `${nextNumber}.json`), JSON.stringify({
            listings: data,
        }));*/
    }

    public async updateExtendedHistory(itemID: number, worldID: number, entries: MarketBoardHistoryEntry[]) {
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
            delete entry.buyerName;
            delete entry.quantity;
            return entry;
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
}
