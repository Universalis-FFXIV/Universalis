import fs from "fs";
import path from "path";
import util from "util";

import { ensurePathsExist } from "../util";

import { Tracker } from "./Tracker";

import { MarketBoardItemListing } from "../models/MarketBoardItemListing";
import { MarketInfoLocalData } from "../models/MarketInfoLocalData";

const exists = util.promisify(fs.exists);
const readFile = util.promisify(fs.readFile);
const writeFile = util.promisify(fs.writeFile);

export class PriceTracker extends Tracker {
    // The path structure is /listings/<worldID/<itemID>/<branchNumber>.json
    constructor() {
        super("../../data", ".json");
    }

    public async set(itemID: number, worldID: number, listings: MarketBoardItemListing[]) {
        const worldDir = path.join(__dirname, this.storageLocation, String(worldID));
        const itemDir = path.join(worldDir, String(itemID));
        const filePath = path.join(itemDir, "0.json");
        // const listings = (await readdir(filePath)).filter((el) => el.endsWith(".json"));

        await ensurePathsExist(worldDir, itemDir);

        let data: MarketInfoLocalData = {
            itemID,
            listings,
            worldID
        };

        if (await exists(filePath)) {
            data.recentHistory = JSON.parse((await readFile(filePath)).toString()).recentHistory;
        }

        this.updateDataCenterProperty("listings", itemID, worldID, listings);

        await writeFile(filePath, JSON.stringify(data));
    }
}
