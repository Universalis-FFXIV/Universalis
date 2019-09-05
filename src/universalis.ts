// Dependencies
import fs from "fs";
import Koa from "koa";
import bodyParser from "koa-bodyparser";
import Router from "koa-router";
import serve from "koa-static";
import views from "koa-views";
import path from "path";
import util from "util";

import remoteDataManager from "./remoteDataManager";

// Load models
import { MarketBoardHistoryEntry } from "./models/MarketBoardHistoryEntry";
import { MarketBoardItemListing } from "./models/MarketBoardItemListing";
import { MarketBoardListingsUpload } from "./models/MarketBoardListingsUpload";
import { MarketBoardSaleHistoryUpload } from "./models/MarketBoardSaleHistoryUpload";

import { HistoryTracker } from "./trackers/HistoryTracker";
import { PriceTracker } from "./trackers/PriceTracker";

const readFile = util.promisify(fs.readFile);

// Define application and its internal resources
const historyTracker = new HistoryTracker();
const priceTracker = new PriceTracker();

const universalis = new Koa();
universalis.use(bodyParser({
    enableTypes: ["json"],
    jsonLimit: "500kb"
}));

remoteDataManager.fetchAll(); // Fetch remote files asynchronously

// Create directories
if (!fs.existsSync(path.join(__dirname, "./branches"))) {
    fs.mkdirSync(path.join(__dirname, "./branches"));
}

// Logger TODO
universalis.use(async (ctx, next) => {
    await next();
    const rt = ctx.response.get("X-Response-Time");
    console.log(`${ctx.method} ${ctx.url} - ${rt}`);
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
    let data = JSON.parse((await readFile(
        path.join(__dirname, "../data", ctx.params.world, ctx.params.item, "0.json")
    )).toString()); // Files are buffers
    if (!data) {
        ctx.throw(404);
        return;
    }
    ctx.body = data;
});

router.get("/api/history/:world/:item", async (ctx) => { // Extended history
    let data = JSON.parse((await readFile(
        path.join(__dirname, "../history", ctx.params.world, ctx.params.item, "0.json")
    )).toString());
    if (!data) {
        ctx.throw(404);
        return;
    }
    ctx.body = data;
});

router.post("/upload", async (ctx) => {
    if (!ctx.is("json")) {
        ctx.throw(415);
        return;
    }

    let marketBoardData: MarketBoardListingsUpload & MarketBoardSaleHistoryUpload = ctx.request.body;

    // You can't upload data for these worlds because you can't scrape it.
    // This does include Chinese and Korean worlds for the time being.
    if (!marketBoardData.worldID || !marketBoardData.itemID) return ctx.throw(415);
    if (marketBoardData.worldID <= 16 || marketBoardData.worldID >= 100) return ctx.throw(415);

    // TODO sanitation
    let dataArray: MarketBoardItemListing[] & MarketBoardHistoryEntry[] = [];
    if (marketBoardData.listings) {
        for (let listing of marketBoardData.listings) {
            listing.total = listing.pricePerUnit * listing.quantity;
            dataArray.push(listing);
        }
        priceTracker.set(marketBoardData.itemID, marketBoardData.worldID, dataArray as MarketBoardItemListing[]);
    } else if (marketBoardData.entries) {
        for (let entry of marketBoardData.entries) {
            entry.total = entry.pricePerUnit * entry.quantity;
            dataArray.push(entry);
        }
        historyTracker.set(marketBoardData.itemID, marketBoardData.worldID, dataArray as MarketBoardHistoryEntry[]);
    } else {
        ctx.throw(418);
    }
});

universalis.use(router.routes());

// Start server
universalis.listen(3000);
