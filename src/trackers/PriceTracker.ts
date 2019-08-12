import fs from "fs";
import util from "util";

import { MarketBoardItemListing } from "../models/MarketBoardItemListing";
import { MarketInfoLocalData } from "../models/MarketInfoLocalData";

const exists = util.promisify(fs.exists);
const readFile = util.promisify(fs.readFile);
const writeFile = util.promisify(fs.writeFile);

export class PriceTracker {
    private listings: Map<number, MarketBoardItemListing[]>;

    constructor() {
        if (!fs.existsSync("../data")) {
            fs.mkdirSync("../data");
        }

        this.listings = new Map();
    }

    public get(id: number) {
        return this.listings.get(id);
    }

    public async set(itemID: number, worldID: number, itemListings: MarketBoardItemListing[]) {
        // TODO data processing
        this.listings.set(itemID, itemListings);

        // Write to filesystem
        let path = "../data/" + itemID + ".json";
        // TODO fix race condition from concurrent updates
        let localData: MarketInfoLocalData;
        if (await exists(path)) { // Keep history intact
            localData = JSON.parse((await readFile(path)).toString());
        }
        localData.listings = itemListings;
        await writeFile(path, JSON.stringify(localData));
    }
}
