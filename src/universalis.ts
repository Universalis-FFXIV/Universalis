// Dependencies
import cors from "@koa/cors";
import Router from "@koa/router";
import difference from "lodash.difference";
import Koa from "koa";
import bodyParser from "koa-bodyparser";
import serve from "koa-static";
import queryParams from "koa-queryparams";
import { MongoClient } from "mongodb";
import sha from "sha.js";
import winston from "winston";
import DailyRotateFile from "winston-daily-rotate-file";

import { CronJobManager } from "./cron/CronJobManager";
import { BlacklistManager } from "./db/BlacklistManager";
import { ContentIDCollection } from "./db/ContentIDCollection";
import { ExtraDataManager } from "./db/ExtraDataManager";
import { RemoteDataManager } from "./remote/RemoteDataManager";
import { HistoryTracker } from "./trackers/HistoryTracker";
import { PriceTracker } from "./trackers/PriceTracker";
import { appendWorldDC } from "./util";
import validation from "./validate";

// Load models
import { Collection } from "mongodb";

import { CharacterContentIDUpload } from "./models/CharacterContentIDUpload";
import { City } from "./models/City";
import { DailyUploadStatistics } from "./models/DailyUploadStatistics";
import { MarketBoardHistoryEntry } from "./models/MarketBoardHistoryEntry";
import { MarketBoardItemListing } from "./models/MarketBoardItemListing";
import { MarketBoardListingsUpload } from "./models/MarketBoardListingsUpload";
import { MarketBoardSaleHistoryUpload } from "./models/MarketBoardSaleHistoryUpload";
import { RecentlyUpdated } from "./models/RecentlyUpdated";
import { TrustedSource } from "./models/TrustedSource";
import { WorldItemPairList } from "./models/WorldItemPairList";

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

const worldMap: Map<string, number> = new Map();

const init = (async () => {
    // DB Data Managers
    const universalisDB = (await db).db("universalis");

    extendedHistory = universalisDB.collection("extendedHistory");
    recentData = universalisDB.collection("recentData");

    remoteDataManager = new RemoteDataManager({ logger });
    await remoteDataManager.fetchAll();
    logger.info("Loaded all remote resources.");

    blacklistManager = await BlacklistManager.create(logger, universalisDB);
    contentIDCollection = await ContentIDCollection.create(logger, universalisDB);
    extraDataManager = await ExtraDataManager.create(remoteDataManager, universalisDB);
    historyTracker = await HistoryTracker.create(universalisDB);
    priceTracker = await PriceTracker.create(universalisDB);

    // World-ID conversions
    const worldList = await remoteDataManager.parseCSV("World.csv");
	for (const worldEntry of worldList) {
        if (worldEntry[0] === "25") continue;
        worldMap.set(worldEntry[1], parseInt(worldEntry[0]));
	}

    logger.info("Connected to database and started data managers.");
})();

const universalis = new Koa();
// CORS support
universalis.use(cors());
// POST endpoint enabling
universalis.use(bodyParser({
    enableTypes: ["json"],
    jsonLimit: "3mb"
}));
// Query parameters
universalis.use(queryParams());

// Logging
universalis.use(async (ctx, next) => {
    console.log(`${ctx.method} ${ctx.url}`);
    await next();
});

// Use single init await
universalis.use(async (ctx, next) => {
    await init;
    await next();
});

// Convert worldDC strings (numbers or names) to world IDs or DC names
universalis.use(async (ctx, next) => {
    if (ctx.params && ctx.params.world) {
        const worldName = ctx.params.world.charAt(0).toUpperCase() + ctx.params.world.substr(1);
        if (!parseInt(ctx.params.world) && !worldMap.get(worldName)) {
            ctx.params.dcName = ctx.params.world;
        } else {
            if (parseInt(ctx.params.world)) {
                ctx.params.worldID = parseInt(ctx.params.world);
            } else {
                ctx.params.worldID = worldMap.get(worldName);
            }
        }
    }

    await next();
});

// Publish public resources
universalis.use(serve("./public"));

// Routing
const router = new Router();

// Documentation page (temporary)
router.get("/docs", async (ctx) => {
    ctx.redirect("/docs/index.html");
});

// REST API
router.get("/api/:world/:item", async (ctx) => { // Normal data
    const itemIDs: number[] = (ctx.params.item as string).split(",").map((id, index) => {
        if (index > 20) return;
        return parseInt(id);
    });

    // Query construction
    const query = { itemID: { $in: itemIDs } };
    appendWorldDC(query, ctx);

    // Request database info
    let data = {
        itemIDs,
        items: await recentData.find(query, { projection: { _id: 0, uploaderID: 0 } }).toArray()
    };
    appendWorldDC(data, ctx);

    // Do some post-processing on resolved item listings.
    for (const item of data.items) {
        if (item.listings.length > 0) {
            item.listings = item.listings.sort((a: MarketBoardItemListing, b: MarketBoardItemListing) => {
                if (a.pricePerUnit > b.pricePerUnit) return 1;
                if (a.pricePerUnit < b.pricePerUnit) return -1;
                return 0;
            });
        }
        for (const listing of item.listings) {
            listing.isCrafted =
                listing.creatorID !== "5feceb66ffc86f38d952786c6d696c79c2dbc239dd4e91b46729d73a27fb57e9";
        }
        for (const entry of item.recentHistory) {
            if (entry.uploaderID) delete entry.uploaderID;
        }
    }

    // Fill in unresolved items
    const resolvedItems: number[] = data.items.map((item) => item.itemID);
    const unresolvedItems: number[] = difference(itemIDs, resolvedItems);
    data["unresolvedItems"] = unresolvedItems;

    for (const item of unresolvedItems) {
        const unresolvedItemData = {
            itemID: item,
            lastUploadTime: 0,
            listings: [],
            recentHistory: []
        };
        appendWorldDC(unresolvedItemData, ctx);
        data.items.push(unresolvedItemData);
    }

    // If only one item is requested we just turn the whole thing into the one item.
    if (data.itemIDs.length === 1) {
        data = data.items[0];
    } else if (!unresolvedItems) {
        delete data["unresolvedItems"];
    }

    ctx.body = data;
});

router.get("/api/history/:world/:item", async (ctx) => { // Extended history
    let entriesToReturn: any = ctx.queryParams.entries;
    if (entriesToReturn) entriesToReturn = parseInt(entriesToReturn.replace(/[^0-9]/g, ""));

    const itemIDs: number[] = (ctx.params.item as string).split(",").map((id, index) => {
        if (index > 20) return;
        return parseInt(id);
    });

    // Query construction
    const query = { itemID: { $in: itemIDs } };
    appendWorldDC(query, ctx);

    // Request database info
    let data = {
        itemIDs,
        items: await extendedHistory.find(query, {
            projection: { _id: 0, uploaderID: 0 }
        }).toArray()
    };
    appendWorldDC(data, ctx);

    // Data filtering
    data.items = data.items.map((item) => {
        if (entriesToReturn) item.entries = item.entries.slice(0, Math.min(500, entriesToReturn));
        item.entries = item.entries.map((entry) => {
            delete entry.uploaderID;
            return entry;
        });
        if (!item.lastUploadTime) item.lastUploadTime = 0;
        return item;
    });

    // Fill in unresolved items
    const resolvedItems: number[] = data.items.map((item) => item.itemID);
    const unresolvedItems: number[] = difference(itemIDs, resolvedItems);
    data["unresolvedItems"] = unresolvedItems;

    for (const item of unresolvedItems) {
        const unresolvedItemData = {
            entries: [],
            itemID: item,
            lastUploadTime: 0
        };
        appendWorldDC(unresolvedItemData, ctx);

        data.items.push(unresolvedItemData);
    }

    // If only one item is requested we just turn the whole thing into the one item.
    if (data.itemIDs.length === 1) {
        data = data.items[0];
    } else if (!unresolvedItems) {
        delete data["unresolvedItems"];
    }

    ctx.body = data;
});

router.get("/api/extra/content/:contentID", async (ctx) => { // Content IDs
    const content = contentIDCollection.get(ctx.params.contentID);

    if (!content) {
        ctx.body = {
            contentID: null,
            contentType: null
        };
        return;
    }

    ctx.body = content;
});

router.get("/api/extra/stats/upload-history", async (ctx) => { // Upload rate
    let daysToReturn: any = ctx.queryParams.entries;
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
    let entriesToReturn: any = ctx.queryParams.entries;
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
    let worldID = ctx.queryParams.world ? ctx.queryParams.world.charAt(0).toUpperCase() +
        ctx.queryParams.world.substr(1).toLowerCase() : null;
    let dcName = ctx.queryParams.dcName ? ctx.queryParams.dcName.charAt(0).toUpperCase() +
        ctx.queryParams.dcName.substr(1).toLowerCase() : null;

    if (worldID && !parseInt(worldID)) {
        worldID = worldMap.get(worldID);
    } else if (parseInt(worldID)) {
        worldID = parseInt(worldID);
    }

    if (worldID && dcName && worldID !== 0) {
        dcName = null;
    } else if (worldID && dcName && worldID === 0) {
        worldID = null;
    }

    let entriesToReturn: any = ctx.queryParams.entries;
    if (entriesToReturn) entriesToReturn = parseInt(entriesToReturn.replace(/[^0-9]/g, ""));

    const data: WorldItemPairList =
        await extraDataManager.getLeastRecentlyUpdatedItems(worldID || dcName, entriesToReturn);

    if (!data) {
        ctx.body =  {
            items: []
        } as WorldItemPairList;
        return;
    }

    ctx.body = data;
});

router.post("/upload/:apiKey", async (ctx) => { // Kinda like a main loop
    let err = validation.validateUploadDataPreCast(ctx);
    if (err) {
        return err;
    }

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
    if (ctx.request.body.retainerCity) ctx.request.body.retainerCity = City[ctx.request.body.retainerCity];
    const uploadData:
        CharacterContentIDUpload &
        MarketBoardListingsUpload &
        MarketBoardSaleHistoryUpload
    = ctx.request.body;

    uploadData.uploaderID = sha("sha256").update(uploadData.uploaderID + "").digest("hex");

    err = await validation.validateUploadData({ ctx, uploadData, blacklistManager });
    if (err) {
        return err;
    }

    // Hashing and passing data
    if (uploadData.listings) {
        const dataArray: MarketBoardItemListing[] = [];
        uploadData.listings = uploadData.listings.map((listing) => {
            return validation.cleanListing(listing);
        });

        for (const listing of uploadData.listings) {
            if (listing.creatorID && listing.creatorName) {
                contentIDCollection.set(listing.creatorID, "player", {
                    characterName: listing.creatorName
                });
            }

            if (listing.retainerID && listing.retainerName) {
                contentIDCollection.set(listing.retainerID, "retainer", {
                    characterName: listing.retainerName
                });
            }

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
            return validation.cleanHistoryEntry(entry);
        });

        for (const entry of uploadData.entries) {
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

    await Promise.all(promises);

    ctx.body = "Success";
});

universalis.use(router.routes());

// Start server
const port = 4000;
universalis.listen(port);
logger.info(`Server started on port ${port}.`);
