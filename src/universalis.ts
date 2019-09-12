// Dependencies
import Router from "@koa/router";
import Koa from "koa";
import bodyParser from "koa-bodyparser";
import serve from "koa-static";
import { MongoClient } from "mongodb";
import sha from "sha.js";
import winston from "winston";
import DailyRotateFile from "winston-daily-rotate-file";

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
import { ExtendedHistory } from "./models/ExtendedHistory";
import { MarketBoardHistoryEntry } from "./models/MarketBoardHistoryEntry";
import { MarketBoardItemListing } from "./models/MarketBoardItemListing";
import { MarketBoardListingsUpload } from "./models/MarketBoardListingsUpload";
import { MarketBoardSaleHistoryUpload } from "./models/MarketBoardSaleHistoryUpload";
import { RecentlyUpdated } from "./models/RecentlyUpdated";
import { TrustedSource } from "./models/TrustedSource";

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
var recentData: Collection;
var extendedHistory: Collection;

var extraData: Collection;
var extraDataManager: ExtraDataManager;

var contentIDCollection: ContentIDCollection;

var historyTracker: HistoryTracker;
var priceTracker: PriceTracker;
const init = (async () => {
    const universalisDB = (await db).db("universalis");

    const contentCollection = universalisDB.collection("content");

    recentData = universalisDB.collection("recentData");
    extendedHistory = universalisDB.collection("extendedHistory");

    extraData = universalisDB.collection("extraData");

    contentIDCollection = new ContentIDCollection(contentCollection);

    historyTracker = new HistoryTracker(recentData, extendedHistory);
    priceTracker = new PriceTracker(recentData);

    extraDataManager = new ExtraDataManager(extraData);

    logger.info("Connected to database and started trackers.");
})();

const universalis = new Koa();
universalis.use(bodyParser({
    enableTypes: ["json"],
    jsonLimit: "1mb"
}));

/*const cronManager = new CronJobManager({ logger });
cronManager.startAll();*/
const remoteDataManager = new RemoteDataManager({ logger });
remoteDataManager.fetchAll();

universalis.use(async (ctx, next) => {
    console.log(`${ctx.method} ${ctx.url}`);
    await next();
});

// Publish public resources
universalis.use(serve("./public"));

// Routing
const router = new Router();

router.get("/api/:world/:item", async (ctx) => { // Normal data
    await init;

    const query = { itemID: parseInt(ctx.params.item) };
    if (!parseInt(ctx.params.world)) {
        query["dcName"] = ctx.params.world;
    } else {
        query["worldID"] = parseInt(ctx.params.world);
    }

    const data = await recentData.findOne(query, { projection: { _id: 0 } });

    if (!data) {
        ctx.body = {
            itemID: parseInt(ctx.params.item),
            lastUploadTime: 0,
            listings: [],
            recentHistory: []
        };
        if (!parseInt(ctx.params.world)) {
            ctx.body["dcName"] = ctx.params.world;
        } else {
            ctx.body["worldID"] = parseInt(ctx.params.world);
        }
        return;
    }

    if (!data.lastUploadTime) data.lastUploadTime = 0;

    ctx.body = data;
});

router.get("/api/history/:world/:item", async (ctx) => { // Extended history
    await init;

    const queryParameters: string[] = ctx.params.item.split(/[?&]+/g);

    const itemID = parseInt(queryParameters[0]);
    let entriesToReturn: any = queryParameters.find((param) => param.startsWith("entries"));
    if (entriesToReturn) entriesToReturn = parseInt(entriesToReturn.replace(/[^0-9]/g, ""));

    const query = { itemID: itemID };
    if (!parseInt(ctx.params.world)) {
        query["dcName"] = ctx.params.world;
    } else {
        query["worldID"] = parseInt(ctx.params.world);
    }

    const data: ExtendedHistory = await extendedHistory.findOne(query, { projection: { _id: 0 } });

    if (!data) {
        ctx.body = {
            entries: [],
            itemID: itemID,
            lastUploadTime: 0,
        };
        if (!parseInt(ctx.params.world)) {
            ctx.body["dcName"] = ctx.params.world;
        } else {
            ctx.body["worldID"] = parseInt(ctx.params.world);
        }
        return;
    }

    if (!data.lastUploadTime) data.lastUploadTime = 0;
    if (entriesToReturn) data.entries = data.entries.slice(0, Math.min(500, entriesToReturn));

    ctx.body = data;
});

router.get("/api/extra/content/:contentID", async (ctx) => { // Content IDs
    await init;

    const content = contentIDCollection.get(sha("sha256").update(ctx.params.contentID + "").digest("hex"));

    if (!content) {
        ctx.body = {};
        return;
    }

    ctx.body = content;
});

router.get("/api/extra/stats/recently-updated", async (ctx) => { // Recently updated items
    await init;

    const data: RecentlyUpdated = await extraDataManager.getRecentlyUpdatedItems();

    if (!data) {
        ctx.body =  {
            items: []
        } as RecentlyUpdated;
        return;
    }

    ctx.body = data;
});

router.post("/upload/:apiKey", async (ctx) => {
    if (!ctx.params.apiKey) {
        return ctx.throw(401);
    }

    if (!ctx.is("json")) {
        return ctx.throw(415);
    }

    await init;

    // Accept identity via API key.
    const dbo = (await db).db("universalis");
    const apiKey = sha("sha512").update(ctx.params.apiKey).digest("hex");
    const trustedSource: TrustedSource = await dbo.collection("trustedSources").findOne({ apiKey });
    if (!trustedSource) return ctx.throw(401);

    const sourceName = trustedSource.sourceName;

    if (trustedSource.uploadCount) await dbo.collection("trustedSources").updateOne({ apiKey }, {
        $inc: {
            uploadCount: 1
        }
    });
    else await dbo.collection("trustedSources").updateOne({ apiKey }, {
        $set: {
            uploadCount: 1
        }
    });

    logger.info("Received upload from " + sourceName + ":\n" + JSON.stringify(ctx.request.body));

    // Data processing
    ctx.request.body.retainerCity = City[ctx.request.body.retainerCity];
    const uploadData:
        CharacterContentIDUpload &
        MarketBoardListingsUpload &
        MarketBoardSaleHistoryUpload
    = ctx.request.body;

    // You can't upload data for these worlds because you can't scrape it.
    // This does include Chinese and Korean worlds for the time being.
    if (!uploadData.worldID || !uploadData.itemID) return ctx.throw(415);
    if (uploadData.worldID <= 16 || uploadData.worldID >= 100) return ctx.throw(415);

    // TODO sanitation
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

        uploadData.uploaderID = sha("sha256").update(uploadData.uploaderID + "").digest("hex");

        for (const listing of uploadData.listings) {
            listing.total = listing.pricePerUnit * listing.quantity;
            dataArray.push(listing as any);
        }

        await priceTracker.set(
            uploadData.uploaderID,
            uploadData.itemID,
            uploadData.worldID,
            dataArray as MarketBoardItemListing[]
        );
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

        uploadData.uploaderID = sha("sha256").update(uploadData.uploaderID + "").digest("hex");

        for (const entry of uploadData.entries) {
            entry.total = entry.pricePerUnit * entry.quantity;
            dataArray.push(entry);
        }

        await historyTracker.set(
            uploadData.uploaderID,
            uploadData.itemID,
            uploadData.worldID,
            dataArray as MarketBoardHistoryEntry[]
        );
    }

    if (uploadData.itemID) {
        await extraDataManager.addRecentlyUpdatedItem(uploadData.itemID);
    }

    if (uploadData.contentID && uploadData.characterName) {
        uploadData.contentID = sha("sha256").update(uploadData.contentID + "").digest("hex");

        await contentIDCollection.set(uploadData.contentID, "player", {
            characterName: uploadData.characterName
        });
    }

    if (!uploadData.listings && !uploadData.entries && !uploadData.contentID && !uploadData.characterName) {
        ctx.throw(418);
    }

    ctx.body = "Success";
});

universalis.use(router.routes());

// Start server
const port = 4000;
universalis.listen(port);
logger.info(`Server started on port ${port}.`);
