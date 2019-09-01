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
const historyTracker = new HistoryTracker(); // TODO
const priceTracker = new PriceTracker();

const universalis = new Koa();
universalis.use(bodyParser({
    enableTypes: ["json"],
    jsonLimit: "500kb"
}));

remoteDataManager.fetchAll();

// Create directories
if (!fs.existsSync(path.join(__dirname, "./branches"))) {
    fs.mkdirSync(path.join(__dirname, "./branches"));
}

// Logger
universalis.use(async (ctx, next) => {
    await next();
    const rt = ctx.response.get("X-Response-Time");
    console.log(`${ctx.method} ${ctx.url} - ${rt}`);
});

// Define views
universalis.use(views("./views", {
    extension: "pug"
}));

// Publish public resources
universalis.use(serve("./public"));

// Define routes
const router = new Router();

router.get("/", async (ctx) => {
    await ctx.render("index.pug", {
        name: "Universalis - Crowdsourced Market Board Aggregator",
        version: process.env.npm_package_version
    });
});

router.get("/api/:world/:item", async (ctx) => {
    let data = JSON.parse((await readFile(
        path.join(__dirname, "../data", ctx.params.world, ctx.params.item, "0.json")
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

    // TODO sanitation
    let dataArray: MarketBoardItemListing[] & MarketBoardHistoryEntry[] = [];
    if (marketBoardData.listings[0]) {
        for (let i = 0; i < marketBoardData.listings.length; i++) {
            dataArray.push(marketBoardData.listings[i]);
        }
        priceTracker.set(marketBoardData.itemID, marketBoardData.worldID, dataArray as MarketBoardItemListing[]);
    } else if (marketBoardData.entries[0]) {
        for (let i = 0; i < marketBoardData.entries.length; i++) {
            dataArray.push(marketBoardData.entries[i]);
        }
        historyTracker.set(marketBoardData.itemID, marketBoardData.worldID, dataArray as MarketBoardHistoryEntry[]);
    } else {
        ctx.throw(418);
    }
});

universalis.use(router.routes());

// Start server
universalis.listen(3000);
