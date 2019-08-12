import fs from "fs";
import util from "util";

import { MarketBoardHistoryEntry } from "../models/MarketBoardHistoryEntry";
import { MarketInfoLocalData } from "../models/MarketInfoLocalData";

const exists = util.promisify(fs.exists);
const readFile = util.promisify(fs.readFile);
const writeFile = util.promisify(fs.writeFile);

export class HistoryTracker {
    private entries: Map<number, MarketBoardHistoryEntry[]>;

    constructor() {
        if (!fs.existsSync("../data")) {
            fs.mkdirSync("../data");
        }

        this.entries = new Map();
    }

    public get(id: number) {
        return this.entries.get(id);
    }

    public async set(itemID: number, worldID: number, saleHistory: MarketBoardHistoryEntry[]) {
        // TODO data processing
        this.entries.set(itemID, saleHistory);

        // Write to filesystem
        let path = "../data/" + itemID + ".json";
        // TODO fix race condition from concurrent updates
        let localData: MarketInfoLocalData;
        if (await exists(path)) { // Keep listings intact
            localData = JSON.parse((await readFile(path)).toString());
        }
        localData[worldID].history = saleHistory;
        await writeFile(path, JSON.stringify(localData));
    }
}
