import fs from "fs";
import path from "path";
import util from "util";

import { Tracker } from "./Tracker";

const readdir = util.promisify(fs.readdir);
const writeFile = util.promisify(fs.writeFile);

export class PriceTracker extends Tracker {
    // The path structure is /listings/<worldID/<itemID>/<branchNumber>.json
    constructor() {
        super("../../listings", ".json");

        const worlds = fs.readdirSync(path.join(__dirname, this.storageLocation));
        for (let world of worlds) {
            const items = fs.readdirSync(path.join(__dirname, this.storageLocation, world));
            for (let item of items) {
                let listings = JSON.parse(
                    fs.readFileSync(
                        path.join(__dirname, this.storageLocation, world, item, "0.json")
                    ).toString()
                );

                this.data.set(parseInt(item), { worldID: listings.worldID, data: listings.listings });
            }
        }
    }

    public async set(itemID: number, worldID: number, data: any[]) {
        if (worldID === 0) return; // You can't upload crossworld market data because you can't scrape it.

        const filePath = path.join(__dirname, this.storageLocation, String(worldID), String(itemID));
        const listings = await readdir(filePath);
        const nextNumber = parseInt(
            listings[listings.length - 1].substr(0, listings[listings.length - 1].indexOf("."))
        ) + 1;

        if (!fs.existsSync(path.join(__dirname, this.storageLocation, String(worldID)))) {
            fs.mkdirSync(path.join(__dirname, this.storageLocation, String(worldID)));
        }

        await writeFile(path.join(filePath, `${nextNumber}.json`), JSON.stringify({
            listings: data,
        }));
    }
}
