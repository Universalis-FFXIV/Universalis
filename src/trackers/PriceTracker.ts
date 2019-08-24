import fs from "fs";
import path from "path";
import util from "util";

import { MarketBoardItemListing } from "../models/MarketBoardItemListing";
import { MarketInfoLocalData } from "../models/MarketInfoLocalData";

const exists = util.promisify(fs.exists);
const readFile = util.promisify(fs.readFile);
const writeFile = util.promisify(fs.writeFile);

var listings: Map<number, MarketBoardItemListing[]>;

export class PriceTracker {
    constructor() {
        if (!fs.existsSync("./data")) {
            fs.mkdirSync("./data");
        }

        listings = new Map();
    }

    public get(id: number) {
        return listings.get(id);
    }

    public async set(itemID: number, worldID: number, itemListings: MarketBoardItemListing[]) {
        // TODO data processing
        listings.set(itemID, itemListings);

        // Write to filesystem
        let jsonPath = path.join(__dirname, "./data/" + itemID + ".json");
        // TODO fix race condition from concurrent updates
        let localData = {} as MarketInfoLocalData;
        if (await exists(jsonPath)) { // Keep listings intact
            localData = JSON.parse((await readFile(jsonPath)).toString());
        }
        if (!localData[worldID]) localData[worldID] = {};
        localData[worldID].listings = itemListings;
        await writeFile(jsonPath, JSON.stringify(localData));
    }
}
