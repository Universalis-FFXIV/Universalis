import fs from "fs";
import path from "path";
import util from "util";

import { MarketBoardHistoryEntry } from "../models/MarketBoardHistoryEntry";
import { MarketInfoLocalData } from "../models/MarketInfoLocalData";

const exists = util.promisify(fs.exists);
const readFile = util.promisify(fs.readFile);
const writeFile = util.promisify(fs.writeFile);

var entries: Map<number, MarketBoardHistoryEntry[]>;

export class HistoryTracker {
    constructor() {
        if (!fs.existsSync("./data")) {
            fs.mkdirSync("./data");
        }

        entries = new Map();
    }

    public get(id: number) {
        return entries.get(id);
    }

    public async set(itemID: number, worldID: number, saleHistory: MarketBoardHistoryEntry[]) {
        // TODO data processing
        entries.set(itemID, saleHistory);

        // Write to filesystem
        let jsonPath = path.join(__dirname, "./data/" + itemID + ".json");
        // TODO fix race condition from concurrent updates
        let localData = {} as MarketInfoLocalData;
        if (await exists(jsonPath)) { // Keep listings intact
            localData = JSON.parse((await readFile(jsonPath)).toString());
        }
        if (!localData[worldID]) localData[worldID] = {};
        localData[worldID].history = saleHistory;
        await writeFile(jsonPath, JSON.stringify(localData));
    }
}
