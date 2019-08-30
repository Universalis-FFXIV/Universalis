import fs from "fs";
import path from "path";
import util from "util";

import { Tracker } from "./Tracker";

import { MarketBoardItemListing } from "../models/MarketBoardItemListing";

const exists = util.promisify(fs.exists);
const mkdir = util.promisify(fs.mkdir);
const readdir = util.promisify(fs.readdir);
const unlink = util.promisify(fs.unlink);
const writeFile = util.promisify(fs.writeFile);

export class PriceTracker extends Tracker {
    // The path structure is /listings/<worldID/<itemID>/<branchNumber>.json
    constructor() {
        super("../../listings", ".json");

        const worlds = fs.readdirSync(path.join(__dirname, this.storageLocation));
        for (let world of worlds) {
            const items = fs.readdirSync(path.join(__dirname, this.storageLocation, world));
            for (let item of items) {
                let listings;
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

    public async set(itemID: number, worldID: number, data: MarketBoardItemListing[]) {
        if (worldID === 0) return; // You can't upload crossworld market data because you can't scrape it.

        const worldDir = path.join(__dirname, this.storageLocation, String(worldID));
        const itemDir = path.join(worldDir, String(itemID));
        const filePath = path.join(itemDir, "0.json");
        // const listings = (await readdir(filePath)).filter((el) => el.endsWith(".json"));

        if (!(await exists(worldDir))) {
            await mkdir(worldDir);
        }

        if (!(await exists(itemDir))) {
            await mkdir(itemDir);
        }

        if (await exists(filePath)) {
            await unlink(filePath);
        }

        await writeFile(filePath, JSON.stringify({
            listings: data,
        }));

        /*const nextNumber = parseInt(
            listings[listings.length - 1].substr(0, listings[listings.length - 1].indexOf("."))
        ) + 1;

        if (!fs.existsSync(path.join(__dirname, this.storageLocation, String(worldID)))) {
            fs.mkdirSync(path.join(__dirname, this.storageLocation, String(worldID)));
        }

        await writeFile(path.join(filePath, `${nextNumber}.json`), JSON.stringify({
            listings: data,
        }));*/
    }
}
