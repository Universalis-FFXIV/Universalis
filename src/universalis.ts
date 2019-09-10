// Dependencies
import Router from "@koa/router";
import Koa from "koa";
import bodyParser from "koa-bodyparser";
import serve from "koa-static";
import views from "koa-views";
import { MongoClient } from "mongodb";
import sha from "sha.js";
import winston from "winston";
import DailyRotateFile from "winston-daily-rotate-file";

import { ContentIDCollection } from "./ContentIDCollection";
import { CronJobManager } from "./CronJobManager";
import { RemoteDataManager } from "./RemoteDataManager";

// Scripts
// import createGarbageData from "../scripts/createGarbageData";
// createGarbageData();

// Load models
import { Collection } from "mongodb";

import { CharacterContentIDUpload } from "./models/CharacterContentIDUpload";
import { City } from "./models/City";
import { MarketBoardHistoryEntry } from "./models/MarketBoardHistoryEntry";
import { MarketBoardItemListing } from "./models/MarketBoardItemListing";
import { MarketBoardListingsUpload } from "./models/MarketBoardListingsUpload";
import { MarketBoardSaleHistoryUpload } from "./models/MarketBoardSaleHistoryUpload";

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
        })
    ]
});
logger.info("Process started.");

const db = MongoClient.connect("mongodb://localhost:27017/", { useNewUrlParser: true, useUnifiedTopology: true });
var recentData: Collection;
var extendedHistory: Collection;

var contentIDCollection: ContentIDCollection;

var historyTracker: HistoryTracker;
var priceTracker: PriceTracker;
const init = (async () => {
    const universalisDB = (await db).db("universalis");

    const contentCollection = universalisDB.collection("content");

    recentData = universalisDB.collection("recentData");
    extendedHistory = universalisDB.collection("extendedHistory");

    contentIDCollection = new ContentIDCollection(
        contentCollection
    );

    historyTracker = new HistoryTracker(
        recentData,
        extendedHistory
    );
    priceTracker = new PriceTracker(
        recentData
    );

    logger.info("Connected to database and started trackers.");
})();

const universalis = new Koa();
universalis.use(bodyParser({
    enableTypes: ["json"],
    jsonLimit: "1mb"
}));

const cronManager = new CronJobManager({ logger });
cronManager.startAll();
const remoteDataManager = new RemoteDataManager({ logger });
remoteDataManager.fetchAll();

universalis.use(async (ctx, next) => {
    console.log(`${ctx.method} ${ctx.url}`);
    await next();
});

// Set up renderer
universalis.use(views("./views", {
    extension: "pug"
}));

// Publish public resources
universalis.use(serve("./public"));

// Routing
const router = new Router();

router.get("/", async (ctx) => {
    await ctx.render("index.pug", {
        name: "Universalis - Crowdsourced Market Board Aggregator",
        version: require("../package.json").version
    });
});

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
            itemID: ctx.params.item,
            listings: [],
            recentHistory: [],
            worldID: ctx.params.world
        };
        return;
    }

    ctx.body = data;
});

router.get("/api/history/:world/:item", async (ctx) => { // Extended history
    await init;

    const query = { itemID: parseInt(ctx.params.item) };
    if (!parseInt(ctx.params.world)) {
        query["dcName"] = ctx.params.world;
    } else {
        query["worldID"] = parseInt(ctx.params.world);
    }

    const data = await extendedHistory.findOne(query, { projection: { _id: 0 } });

    if (!data) {
        ctx.body = {
            entries: [],
            itemID: ctx.params.item,
            worldID: ctx.params.world
        };
        return;
    }

    ctx.body = data;
});

router.get("/api/content/:contentID", async (ctx) => { // Normal data
    await init;

    const content = contentIDCollection.get(parseInt(ctx.params.contentID));

    if (!content) {
        ctx.body = {
            contentID: parseInt(ctx.params.contentID)
        };
        return;
    }

    ctx.body = content;
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
    const trustedSource = await dbo.collection("trustedSources").findOne({
        apiKey: sha("sha512").update(ctx.params.apiKey).digest("hex")
    });
    if (!trustedSource) return ctx.throw(401);

    const sourceName = trustedSource.sourceName;

    logger.info("Received upload from " + sourceName + ":\n" + ctx.request.body);

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
        uploadData.listings.map((listing) => {
            return {
                creatorID: listing.creatorID,
                creatorName: listing.creatorName,
                hq: listing.hq,
                lastReviewTime: listing.lastReviewTime,
                listingID: listing.listingID,
                materia: listing.materia ? listing.materia : [],
                onMannequin: listing.onMannequin,
                pricePerUnit: listing.pricePerUnit,
                quantity: listing.quantity,
                retainerCity: listing.retainerCity,
                retainerID: listing.retainerID,
                retainerName: listing.retainerName,
                sellerID: listing.sellerID,
                stainID: listing.stainID
            };
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
    } else if (uploadData.entries) {
        const dataArray: MarketBoardHistoryEntry[] = [];
        uploadData.entries.map((entry) => {
            return {
                buyerName: entry.buyerName,
                hq: entry.hq,
                pricePerUnit: entry.pricePerUnit,
                quantity: entry.quantity,
                sellerID: entry.sellerID,
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
    } else if (uploadData.contentID && uploadData.characterName) {
        await contentIDCollection.set(uploadData.contentID, "player", {
            characterName: uploadData.characterName
        });
    } else {
        ctx.throw(418);
    }
});

universalis.use(router.routes());

// Start server
universalis.listen(3000);
logger.info("Server started on port 3000.");
