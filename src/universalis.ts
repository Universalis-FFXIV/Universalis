// Dependencies
import cors from "@koa/cors";
import Router from "@koa/router";
import Koa from "koa";
import bodyParser from "koa-bodyparser";
import queryParams from "koa-queryparams";
import serve from "koa-static";
import { Collection, MongoClient } from "mongodb";

// Data managers
// import { CronJobManager } from "./cron/CronJobManager";
import { BlacklistManager } from "./db/BlacklistManager";
import { ContentIDCollection } from "./db/ContentIDCollection";
import { ExtraDataManager } from "./db/ExtraDataManager";
import { TrustedSourceManager } from "./db/TrustedSourceManager";
import { RemoteDataManager } from "./remote/RemoteDataManager";
import { HistoryTracker } from "./trackers/HistoryTracker";
import { PriceTracker } from "./trackers/PriceTracker";

// Endpoint parsers
import { parseContentID } from "./endpoints/parseContentID";
import { parseHistory } from "./endpoints/parseHistory";
import { parseLeastRecentlyUpdatedItems } from "./endpoints/parseLeastRecentlyUpdatedItems";
import { parseListings } from "./endpoints/parseListings";
import { parseRecentlyUpdatedItems } from "./endpoints/parseRecentlyUpdatedItems";
import { parseTaxRates } from "./endpoints/parseTaxRates";
import { parseUploadHistory } from "./endpoints/parseUploadHistory";
import { parseWorldUploadCounts } from "./endpoints/parseWorldUploadCounts";
import { upload } from "./endpoints/upload";

// Utils
import { createLogger } from "./util";

// Define application and its resources
const logger = createLogger();
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
var trustedSourceManager: TrustedSourceManager;

const worldMap: Map<string, number> = new Map();
const worldIDMap: Map<number, string> = new Map();

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
    trustedSourceManager = await TrustedSourceManager.create(universalisDB);

    // World-ID conversions
    const worldList = await remoteDataManager.parseCSV("World.csv");
	for (const worldEntry of worldList) {
        if (!parseInt(worldEntry[0]) || worldEntry[0] === "25") continue;
        worldMap.set(worldEntry[1], parseInt(worldEntry[0]));
        worldIDMap.set(parseInt(worldEntry[0]), worldEntry[1]);
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

// Publish public resources
universalis.use(serve("./public"));

// Routing
const router = new Router();

// Documentation page (temporary)
router.get("/docs", async (ctx) => {
    ctx.redirect("/docs/index.html");
});

// REST API
router
    .get("/api/:world/:item", async (ctx) => { // Normal data
        await parseListings(ctx, worldMap, recentData);
    })
    .get("/api/history/:world/:item", async (ctx) => { // Extended history
        await parseHistory(ctx, worldMap, extendedHistory);
    })
    .get("/api/tax-rates/:world", async (ctx) => { // Tax rates
        await parseTaxRates(ctx, worldMap, extraDataManager);
    })
    .get("/api/extra/content/:contentID", async (ctx) => { // Content IDs
        await parseContentID(ctx, contentIDCollection);
    })
    .get("/api/extra/stats/least-recently-updated", async (ctx) => { // Recently updated items
        await parseLeastRecentlyUpdatedItems(ctx, worldMap, extraDataManager);
    })
    .get("/api/extra/stats/recently-updated", async (ctx) => { // Recently updated items
        await parseRecentlyUpdatedItems(ctx, extraDataManager);
    })
    .get("/api/extra/stats/upload-history", async (ctx) => { // Upload rate
        await parseUploadHistory(ctx, extraDataManager);
    })
    .get("/api/extra/stats/world-upload-counts", async (ctx) => { // World upload counts
        await parseWorldUploadCounts(ctx, extraDataManager);
    })
    .post("/upload/:apiKey", async (ctx) => { // Upload process
        await upload({
            blacklistManager,
            contentIDCollection,
            ctx,
            extraDataManager,
            historyTracker,
            logger,
            priceTracker,
            trustedSourceManager,
            worldIDMap
        });
    });

universalis.use(router.routes());

// Start server
const port = 4000;
universalis.listen(port);
logger.info(`Server started on port ${port}.`);
