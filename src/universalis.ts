// Dependencies
import cors from "@koa/cors";
import Router from "@koa/router";
import Koa from "koa";
import bodyParser from "koa-bodyparser";
import serve from "koa-static";
import { MongoClient } from "mongodb";
import sha from "sha.js";
import winston from "winston";
import DailyRotateFile from "winston-daily-rotate-file";

import { BlacklistManager } from "./BlacklistManager";
import { ContentIDCollection } from "./ContentIDCollection";
import { CronJobManager } from "./CronJobManager";
import { ExtraDataManager } from "./ExtraDataManager";
import { RemoteDataManager } from "./RemoteDataManager";

// Scripts
// import createGarbageData from "../scripts/createGarbageData";
// createGarbageData();

// Load models
import { Collection } from "mongodb";

import { CharacterContentIDUpload } from "./models/CharacterContentIDUpload";
import { City } from "./models/City";
import { DailyUploadStatistics } from "./models/DailyUploadStatistics";
import { ExtendedHistory } from "./models/ExtendedHistory";
import { MarketBoardHistoryEntry } from "./models/MarketBoardHistoryEntry";
import { MarketBoardItemListing } from "./models/MarketBoardItemListing";
import { MarketBoardListingsUpload } from "./models/MarketBoardListingsUpload";
import { MarketBoardSaleHistoryUpload } from "./models/MarketBoardSaleHistoryUpload";
import { RecentlyUpdated } from "./models/RecentlyUpdated";
import { TrustedSource } from "./models/TrustedSource";
import { WorldItemPairList } from "./models/WorldItemPairList";

import { HistoryTracker } from "./trackers/HistoryTracker";
import { PriceTracker } from "./trackers/PriceTracker";

// Define application and its resources
const logger = winston.createLogger({
    transports: [
        new (DailyRotateFile)({
            datePattern: "YYYY-MM-DD-HH",
            filename: "logs/universalis-%DATE%.log",
            maxSize: "20m"
        }),
        new winston.transports.File({
            filename: "logs/error.log",
            level: "error"
        }),
        new winston.transports.Console({
            format: winston.format.simple()
        })
    ]
});
logger.info("Process started.");

const db = MongoClient.connect("mongodb://localhost:27017/", { useNewUrlParser: true, useUnifiedTopology: true });

var blacklistManager: BlacklistManager;
var contentIDCollection: ContentIDCollection;
var extendedHistory: Collection;
var extraDataManager: ExtraDataManager;
var historyTracker: HistoryTracker;
var priceTracker: PriceTracker;
var recentData: Collection;
var remoteDataManager: RemoteDataManager;

const worldMap = new Map();

const init = (async () => {
    // DB Data Managers
    const universalisDB = (await db).db("universalis");

    const blacklist = universalisDB.collection("blacklist");
    const contentCollection = universalisDB.collection("content");
    extendedHistory = universalisDB.collection("extendedHistory");
    const extraData = universalisDB.collection("extraData");
    recentData = universalisDB.collection("recentData");

    blacklistManager = new BlacklistManager(blacklist);
    contentIDCollection = new ContentIDCollection(contentCollection);
    extraDataManager = new ExtraDataManager(extraData, recentData);
    historyTracker = new HistoryTracker(recentData, extendedHistory);
    priceTracker = new PriceTracker(recentData);
    remoteDataManager = new RemoteDataManager({ logger });
    remoteDataManager.fetchAll();

    // World-ID conversions
    const worldList = await remoteDataManager.parseCSV("World.csv");
	for (let worldEntry of worldList) {
	    worldMap.set(worldEntry[1], parseInt(worldEntry[0]));
	}

    logger.info("Connected to database and started data managers.");
})();

const universalis = new Koa();
universalis.use(cors());
universalis.use(bodyParser({
    enableTypes: ["json"],
    jsonLimit: "1mb"
}));

// Logging
universalis.use(async (ctx, next) => {
    console.log(`${ctx.method} ${ctx.url}`);
    await next();
});

// Get query parameters
universalis.use(async (ctx, next) => {
    const queryParameters: string[] = ctx.url.substr(ctx.url.indexOf("?")).split(/[?&]+/g).slice(1);
    ctx.queryParameters = {};
    if (queryParameters) {
        for (let param of queryParameters) {
            const keyValuePair = param.split(/[^a-zA-Z0-9]+/g);
            ctx.queryParameters[keyValuePair[0]] = keyValuePair[1];
        }
    }
    await next();
});

// Publish public resources
universalis.use(serve("./public"));

// Routing
const router = new Router();

router.get("/api/:world/:item", async (ctx) => { // Normal data
    await init;

    const query = { itemID: parseInt(ctx.params.item) };

    const worldName = ctx.params.world.charAt(0).toUpperCase() + ctx.params.world.substr(1);

    if (!parseInt(ctx.params.world) && !worldMap.get(worldName)) {
        query["dcName"] = ctx.params.world;
    } else {
        if (parseInt(ctx.params.world)) {
            query["worldID"] = parseInt(ctx.params.world);
        } else {
            query["worldID"] = worldMap.get(worldName);
        }
    }

    const data = await recentData.findOne(query, { projection: { _id: 0 } });

    if (!data) {
        ctx.body = {
            itemID: parseInt(ctx.params.item),
            lastUploadTime: 0,
            listings: [],
            recentHistory: []
        };
        if (!parseInt(ctx.params.world) && !worldMap.get(worldName)) {
            ctx.body["dcName"] = ctx.params.world;
        } else {
            if (parseInt(ctx.params.world)) {
                ctx.body["worldID"] = parseInt(ctx.params.world);
            } else {
                ctx.body["worldID"] = worldMap.get(worldName);
            }
        }
        return;
    }

    if (!data.lastUploadTime) data.lastUploadTime = 0;
    delete data.uploaderID;

    ctx.body = data;
});

router.get("/api/history/:world/:item", async (ctx) => { // Extended history
    await init;

    const itemID = parseInt(ctx.params.item);
    let entriesToReturn: any = ctx.queryParameters.entries;
    if (entriesToReturn) entriesToReturn = parseInt(entriesToReturn.replace(/[^0-9]/g, ""));

    const query = { itemID: itemID };

    const worldName = ctx.params.world.charAt(0).toUpperCase() + ctx.params.world.substr(1);

    if (!parseInt(ctx.params.world) && !worldMap.get(worldName)) {
        query["dcName"] = ctx.params.world;
    } else {
        if (parseInt(ctx.params.world)) {
            query["worldID"] = parseInt(ctx.params.world);
        } else {
            query["worldID"] = worldMap.get(worldName);
        }
    }

    const data: ExtendedHistory = await extendedHistory.findOne(query, { projection: { _id: 0 } });

    if (!data) {
        ctx.body = {
            entries: [],
            itemID: itemID,
            lastUploadTime: 0,
        };
        if (!parseInt(ctx.params.world) && !worldMap.get(worldName)) {
            ctx.body["dcName"] = ctx.params.world;
        } else {
            if (parseInt(ctx.params.world)) {
                ctx.body["worldID"] = parseInt(ctx.params.world);
            } else {
                ctx.body["worldID"] = worldMap.get(worldName);
            }
        }
        return;
    }

    if (!data.lastUploadTime) data.lastUploadTime = 0;
    if (entriesToReturn) data.entries = data.entries.slice(0, Math.min(500, entriesToReturn));
    data.entries = data.entries.map((entry) => {
        delete entry.uploaderID;
        return entry;
    });

    ctx.body = data;
});

router.get("/api/extra/content/:contentID", async (ctx) => { // Content IDs
    await init;

    const content = contentIDCollection.get(ctx.params.contentID);

    if (!content) {
        ctx.body = {};
        return;
    }

    ctx.body = content;
});

router.get("/api/extra/stats/upload-history", async (ctx) => { // Upload rate
    await init;

    let daysToReturn: any = ctx.queryParameters.entries;
    if (daysToReturn) daysToReturn = parseInt(daysToReturn.replace(/[^0-9]/g, ""));

    const data: DailyUploadStatistics = await extraDataManager.getDailyUploads(daysToReturn);

    if (!data) {
        ctx.body =  {
            uploadCountByDay: []
        } as DailyUploadStatistics;
        return;
    }

    ctx.body = data;
});

router.get("/api/extra/stats/recently-updated", async (ctx) => { // Recently updated items
    await init;

    let entriesToReturn: any = ctx.queryParameters.entries;
    if (entriesToReturn) entriesToReturn = parseInt(entriesToReturn.replace(/[^0-9]/g, ""));

    const data: RecentlyUpdated = await extraDataManager.getRecentlyUpdatedItems(entriesToReturn);

    if (!data) {
        ctx.body =  {
            items: []
        } as RecentlyUpdated;
        return;
    }

    ctx.body = data;
});

router.get("/api/extra/stats/least-recently-updated", async (ctx) => { // Recently updated items
    await init;

    let entriesToReturn: any = ctx.queryParameters.entries;
    if (entriesToReturn) entriesToReturn = parseInt(entriesToReturn.replace(/[^0-9]/g, ""));

    const data: WorldItemPairList = await extraDataManager.getLeastRecentlyUpdatedItems(entriesToReturn);

    if (!data) {
        ctx.body =  {
            items: []
        } as WorldItemPairList;
        return;
    }

    ctx.body = data;
});

router.post("/upload/:apiKey", async (ctx) => { // Kinda like a main loop
    if (!ctx.params.apiKey) {
        return ctx.throw(401);
    }

    if (!ctx.is("json")) {
        ctx.body = "Unsupported content type";
        return ctx.throw(415);
    }

    await init;

    const promises: Array<Promise<any>> = []; // Sort of like a thread list.

    // Accept identity via API key.
    const dbo = (await db).db("universalis");
    const apiKey = sha("sha512").update(ctx.params.apiKey).digest("hex");
    const trustedSource: TrustedSource = await dbo.collection("trustedSources").findOne({ apiKey });
    if (!trustedSource) return ctx.throw(401);

    const sourceName = trustedSource.sourceName;

    if (trustedSource.uploadCount) promises.push(dbo.collection("trustedSources").updateOne({ apiKey }, {
        $inc: {
            uploadCount: 1
        }
    }));
    else promises.push(dbo.collection("trustedSources").updateOne({ apiKey }, {
        $set: {
            uploadCount: 1
        }
    }));

    logger.info("Received upload from " + sourceName + ":\n" + JSON.stringify(ctx.request.body));

    promises.push(extraDataManager.incrementDailyUploads());

    // Data processing
    ctx.request.body.retainerCity = City[ctx.request.body.retainerCity];
    const uploadData:
        CharacterContentIDUpload &
        MarketBoardListingsUpload &
        MarketBoardSaleHistoryUpload
    = ctx.request.body;

    // Check blacklisted uploaders (people who upload fake data)
    uploadData.uploaderID = sha("sha256").update(uploadData.uploaderID + "").digest("hex");
    if (await blacklistManager.has(uploadData.uploaderID)) return ctx.throw(403);

    // You can't upload data for these worlds because you can't scrape it.
    // This does include Chinese and Korean worlds for the time being.
    if (!uploadData.worldID || !uploadData.itemID) {
        ctx.body = "Unsupported World";
        return ctx.throw(404);
    }
    if (uploadData.worldID <= 16 ||
            uploadData.worldID >= 100 ||
            uploadData.worldID === 26 ||
            uploadData.worldID === 27 ||
            uploadData.worldID === 38 ||
            uploadData.worldID === 84) {
        ctx.body = "Unsupported World";
        return ctx.throw(404);
    }

    // Hashing and passing data
    if (uploadData.listings) {
        const dataArray: MarketBoardItemListing[] = [];
        uploadData.listings = uploadData.listings.map((listing) => {
            const newListing = {
                creatorID: sha("sha256").update(listing.creatorID + "").digest("hex"),
                creatorName: listing.creatorName,
                hq: listing.hq,
                lastReviewTime: listing.lastReviewTime,
                listingID: sha("sha256").update(listing.listingID + "").digest("hex"),
                materia: listing.materia ? listing.materia : [],
                onMannequin: listing.onMannequin,
                pricePerUnit: listing.pricePerUnit,
                quantity: listing.quantity,
                retainerCity: listing.retainerCity,
                retainerID: sha("sha256").update(listing.retainerID + "").digest("hex"),
                retainerName: listing.retainerName,
                sellerID: sha("sha256").update(listing.sellerID + "").digest("hex"),
                stainID: listing.stainID
            };

            if (listing.creatorID && listing.creatorName) {
                contentIDCollection.set(newListing.creatorID, "player", {
                    characterName: newListing.creatorName
                });
            }

            if (listing.retainerID && listing.retainerName) {
                contentIDCollection.set(newListing.retainerID, "retainer", {
                    characterName: newListing.retainerName
                });
            }

            return newListing;
        });

        for (const listing of uploadData.listings) {
            listing.total = listing.pricePerUnit * listing.quantity;
            dataArray.push(listing as any);
        }

        promises.push(priceTracker.set(
            uploadData.uploaderID,
            uploadData.itemID,
            uploadData.worldID,
            dataArray as MarketBoardItemListing[]
        ));
    }

    if (uploadData.entries) {
        const dataArray: MarketBoardHistoryEntry[] = [];
        uploadData.entries = uploadData.entries.map((entry) => {
            return {
                buyerName: entry.buyerName,
                hq: entry.hq,
                pricePerUnit: entry.pricePerUnit,
                quantity: entry.quantity,
                sellerID: sha("sha256").update(entry.sellerID + "").digest("hex"),
                timestamp: entry.timestamp
            };
        });

        for (const entry of uploadData.entries) {
            entry.total = entry.pricePerUnit * entry.quantity;
            dataArray.push(entry);
        }

        promises.push(historyTracker.set(
            uploadData.uploaderID,
            uploadData.itemID,
            uploadData.worldID,
            dataArray as MarketBoardHistoryEntry[]
        ));
    }

    if (uploadData.itemID) {
        promises.push(extraDataManager.addRecentlyUpdatedItem(uploadData.itemID));
    }

    if (uploadData.contentID && uploadData.characterName) {
        uploadData.contentID = sha("sha256").update(uploadData.contentID + "").digest("hex");

        promises.push(contentIDCollection.set(uploadData.contentID, "player", {
            characterName: uploadData.characterName
        }));
    }

    if (!uploadData.listings && !uploadData.entries && !uploadData.contentID && !uploadData.characterName) {
        ctx.throw(418);
    }

    await Promise.all(promises);

    ctx.body = "Success";
});

universalis.use(router.routes());

// Start server
const port = 4000;
universalis.listen(port);
logger.info(`Server started on port ${port}.`);
