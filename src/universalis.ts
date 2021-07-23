// Dependencies
import cors from "@koa/cors";
import Router from "@koa/router";
import deasync from "deasync";
import Redis from "ioredis";
import Koa from "koa";
import bodyParser from "koa-bodyparser";
import queryParams from "koa-queryparams";
import ratelimit from "koa-ratelimit";
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
import { TransportManager } from "./transports/TransportManager";

import { EorzeanMarketNoteTransport } from "./transports/EorzeanMarketNoteTransport";

// Endpoint parsers
import v2 from "./endpoints/v2";

import { parseContentID } from "./endpoints/parseContentID";
import { parseHistory } from "./endpoints/parseHistory";
import { parseLeastRecentlyUpdatedItems } from "./endpoints/parseLeastRecentlyUpdatedItems";
import { parseListings } from "./endpoints/parseListings";
import { parseRecentlyUpdatedItems } from "./endpoints/parseRecentlyUpdatedItems";
import { parseTaxRates } from "./endpoints/parseTaxRates";
import { parseUploaderCounts } from "./endpoints/parseUploaderCounts";
import { parseUploadHistory } from "./endpoints/parseUploadHistory";
import { parseWorldUploadCounts } from "./endpoints/parseWorldUploadCounts";
import { serveItemIDJSON } from "./endpoints/serveItemIDJSON";

import { parseEorzeanMarketNote } from "./endpoints/parseEorzeanMarketNote";

import { upload } from "./endpoints/upload";

// Utils
import { FlaggedUploadManager } from "./db/FlaggedUploadManager";
import { UniversalisDiscordClient } from "./discord";
import { deleteListings } from "./endpoints/deleteListings";
import { parseHighestSaleVelocityItems } from "./endpoints/parseHighestSaleVelocityItems";
import { parseMostRecentlyUpdatedItems } from "./endpoints/parseMostRecentlyUpdatedItems";
import { initializeWorldMappings } from "./initializeWorldMappings";
import { createLogger } from "./util";

// Database
const db = MongoClient.connect("mongodb://localhost:27017/", {
	useNewUrlParser: true,
	useUnifiedTopology: true,
});
let dbo: MongoClient;

// Logger
const logger = createLogger("mongodb://localhost:27017/");
logger.info("Process started.");

// Redis
const redisClient = new Redis();
redisClient.on("error", (error) => {
	logger.error(error);
});

// Other stuff
let blacklistManager: BlacklistManager;
let contentIDCollection: ContentIDCollection;
let extendedHistory: Collection;
let extraDataManager: ExtraDataManager;
let historyTracker: HistoryTracker;
let priceTracker: PriceTracker;
let recentData: Collection;
let remoteDataManager: RemoteDataManager;
let trustedSourceManager: TrustedSourceManager;
let flaggedUploadManager: FlaggedUploadManager;
let discord: UniversalisDiscordClient;

const transportManager = new TransportManager();

const worldMap: Map<string, number> = new Map();
const worldIDMap: Map<number, string> = new Map();

const init = (async () => {
	dbo = await db;

	// DB Data Managers
	const universalisDB = dbo.db("universalis");
	logger.info(`Database connected: ${dbo.isConnected()}`);

	extendedHistory = universalisDB.collection("extendedHistory");
	recentData = universalisDB.collection("recentData");

	remoteDataManager = new RemoteDataManager({ logger });
	await remoteDataManager.fetchAll();
	logger.info("Loaded all remote resources.");

	blacklistManager = await BlacklistManager.create(logger, universalisDB);
	logger.info("Initialized BlacklistManager.");
	
	contentIDCollection = await ContentIDCollection.create(logger, universalisDB);
	logger.info("Initialized ContentIDCollections.");

	extraDataManager = await ExtraDataManager.create(
		remoteDataManager,
		worldIDMap,
		worldMap,
		universalisDB,
	);
	logger.info("Initialized ExtraDataManager.");
	
	historyTracker = await HistoryTracker.create(universalisDB);
	priceTracker = await PriceTracker.create(universalisDB);
	logger.info("Initialized HistoryTracker and PriceTracker.");

	trustedSourceManager = await TrustedSourceManager.create(universalisDB);
	logger.info("Initialized TrustedSourceManager.");

	flaggedUploadManager = await FlaggedUploadManager.create(universalisDB);
	logger.info("Initialized FlaggedUploadManager.");

	logger.info("Initialized all database managers.");

	transportManager.addTransport(new EorzeanMarketNoteTransport(logger));

	await initializeWorldMappings(worldMap, worldIDMap);

	discord = await UniversalisDiscordClient.create(
		process.env["UNIVERSALIS_DISCORD_BOT_TOKEN"],
		logger,
		blacklistManager,
		flaggedUploadManager
	);

	logger.info("Initialized Discord service.");
})();

const universalis = new Koa();
// CORS support
universalis.use(cors());
// POST endpoint enabling
universalis.use(
	bodyParser({
		enableTypes: ["json"],
		jsonLimit: "3mb",
	}),
);
// Query parameters
universalis.use(queryParams());

// Logging
universalis.use(async (ctx, next) => {
	if (!ctx.url.includes("upload")) {
		logger.info(`${ctx.method} ${ctx.url}`);
	}
	await next();
});

// Deletions can only be done 4 times per second, since that's at least how long
// it takes to buy an item and wait for confirmation from the server.
universalis.use(
	ratelimit({
		driver: "redis",
		db: redisClient,
		duration: 1000,
		errorMessage: "Rate limit exceeded (4/1s).",
		id: (ctx) => ctx.ip,
		headers: {
			remaining: "Rate-Limit-Remaining",
			reset: "Rate-Limit-Total",
			total: "Rate-Limit-Total",
		},
		max: 12,
		disableHeader: false,
		whitelist: (ctx) => {
			return ctx.method !== "DELETE";
		},
	}),
);

// Use single init await
universalis.use(async (_, next) => {
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
	.get("/api/:world/:item", async (ctx) => {
		// Normal data
		await parseListings(
			ctx,
			remoteDataManager,
			worldMap,
			worldIDMap,
			recentData,
			transportManager,
		);
	})
	.get("/api/v2/:world/:item", async (ctx) => {
		await v2.parseListings(
			ctx,
			remoteDataManager,
			worldMap,
			worldIDMap,
			recentData,
			transportManager,
		);
	})
	.post("/api/:world/:item/delete", async (ctx) => {
		await deleteListings(ctx, trustedSourceManager, worldMap, recentData);
	})
	.get("/api/history/:world/:item", async (ctx) => {
		// Extended history
		await parseHistory(
			ctx,
			remoteDataManager,
			worldMap,
			worldIDMap,
			extendedHistory,
		);
	})
	.get("/api/v2/history/:world/:item", async (ctx) => {
		await v2.parseHistory(
			ctx,
			remoteDataManager,
			worldMap,
			worldIDMap,
			extendedHistory,
		);
	})
	.get("/api/tax-rates", async (ctx) => {
		await parseTaxRates(ctx, worldMap, extraDataManager);
	})
	/*.get("/api/transports/eorzea-market-note/:world/:item", async (ctx) => {
		await parseEorzeanMarketNote(ctx, transportManager);
	})*/
	.get("/api/extra/content/:contentID", async (ctx) => {
		await parseContentID(ctx, contentIDCollection);
	})
	.get("/api/extra/stats/least-recently-updated", async (ctx) => {
		await parseLeastRecentlyUpdatedItems(
			ctx,
			worldMap,
			extraDataManager,
			redisClient,
		);
	})
	.get("/api/extra/stats/most-recently-updated", async (ctx) => {
		await parseMostRecentlyUpdatedItems(
			ctx,
			worldMap,
			extraDataManager,
			redisClient,
		);
	})
	.get("/api/extra/stats/recently-updated", async (ctx) => {
		await parseRecentlyUpdatedItems(ctx, extraDataManager);
	})
	.get("/api/extra/stats/highest-sale-velocity", async (ctx) => {
		// Note: disable this if it uses too much memory on staging.
		await parseHighestSaleVelocityItems(ctx, worldMap, worldIDMap, recentData);
	})
	.get("/api/extra/stats/upload-history", async (ctx) => {
		await parseUploadHistory(ctx, extraDataManager);
	})
	.get("/api/extra/stats/world-upload-counts", async (ctx) => {
		await parseWorldUploadCounts(ctx, extraDataManager);
	})
	.get("/api/extra/stats/uploader-upload-counts", async (ctx) => {
		await parseUploaderCounts(ctx, trustedSourceManager);
	})

	.get("/api/marketable", async (ctx) => {
		// Marketable item ID JSON
		await serveItemIDJSON(ctx, remoteDataManager);
	})

	.post("/upload/:apiKey", async (ctx) => {
		// Upload process
		await upload({
			blacklistManager,
			flaggedUploadManager,
			contentIDCollection,
			ctx,
			discord,
			extraDataManager,
			historyTracker,
			logger,
			priceTracker,
			remoteDataManager,
			trustedSourceManager,
			worldIDMap,
		});
	});

universalis.use(router.routes());

const dbTeardown = async () => {
	logger.info("Closing database connection.");
	if (dbo.isConnected()) {
		logger.info("Waiting for close completion.");
		await dbo.close();
	}
	logger.info("Database connection closed.");
	process.exit();
};
const dbTeardownSync = deasync(dbTeardown); // async methods cannot run while the process is closing.

process.on("SIGINT", dbTeardownSync);
process.on("SIGTERM", dbTeardownSync);

// Start server
const port = process.argv[2] ? parseInt(process.argv[2]) : 4000;
universalis.listen(port);
logger.info(`Server started on port ${port}.`);
