import fs from "fs";
import path from "path";
import util from "util";

import remoteDataManager from "../remoteDataManager";

import { Tracker } from "./Tracker";

import { MarketBoardDCItemListing } from "../models/MarketBoardDCItemListing";
import { MarketBoardItemListing } from "../models/MarketBoardItemListing";
import { MarketInfoDCLocalData } from "../models/MarketInfoDCLocalData";
import { MarketInfoLocalData } from "../models/MarketInfoLocalData";

const exists = util.promisify(fs.exists);
const mkdir = util.promisify(fs.mkdir);
const readFile = util.promisify(fs.readFile);
const unlink = util.promisify(fs.unlink);
const writeFile = util.promisify(fs.writeFile);

export class PriceTracker extends Tracker {
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

    public async set(itemID: number, worldID: number, listings: MarketBoardItemListing[]) {
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
            listings,
            worldID
        };

        if (await exists(filePath)) {
            data.recentHistory = JSON.parse((await readFile(filePath)).toString()).recentHistory;
        }

        this.updateDataCenterHistory(itemID, worldID, listings);

        await writeFile(filePath, JSON.stringify(data));

        /*const nextNumber = parseInt(
            listings[listings.length - 1].substr(0, listings[listings.length - 1].indexOf("."))
        ) + 1;

        await writeFile(path.join(filePath, `${nextNumber}.json`), JSON.stringify({
            listings: data,
        }));*/
    }

    private async updateDataCenterHistory(itemID: number, worldID: number, listings: any[]) {
        const dataCenterWorlds = JSON.parse((await remoteDataManager.fetchFile("dc.json")).toString());
        const worldCSV = (await remoteDataManager.parseCSV("World.csv")).slice(3);
        const world = worldCSV.find((line) => line[0] === String(worldID))[1];

        (listings as MarketBoardDCItemListing[]).forEach((listing) => listing.worldName = world);

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
        if (existingData && existingData.listings) {
            existingData.listings = existingData.listings.filter((listing) => listing.worldName !== world);

            existingData.listings = existingData.listings.concat(listings);

            existingData.listings = existingData.listings.sort((a, b) => {
                if (a.pricePerUnit > b.pricePerUnit) return -1;
                if (a.pricePerUnit < b.pricePerUnit) return 1;
                return 0;
            });
        } else {
            existingData = {
                dcName: dataCenter,
                itemID
            };

            existingData.listings = listings;
        }

        return await writeFile(filePath, JSON.stringify(existingData));
    }
}
