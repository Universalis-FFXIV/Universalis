// Dependencies
import fs from "fs";
import Koa from "koa";
import bodyParser from "koa-bodyparser";
import Router from "koa-router";
import serve from "koa-static";
import views from "koa-views";
import request from "request-promise";

// Load models
import { MarketBoardHistoryEntry } from "./models/MarketBoardHistoryEntry";
import { MarketBoardItemListing } from "./models/MarketBoardItemListing";
import { MarketBoardListingsUpload } from "./models/MarketBoardListingsUpload";
import { MarketBoardSaleHistoryUpload } from "./models/MarketBoardSaleHistoryUpload";

import { HistoryTracker } from "./trackers/HistoryTracker";
import { PriceTracker } from "./trackers/PriceTracker";

// Define application and its internal resources
const historyTracker = new HistoryTracker();
const priceTracker = new PriceTracker();

const universalis = new Koa();
universalis.use(bodyParser({
    enableTypes: ["json"],
    jsonLimit: "500kb"
}));

// Get local copies of certain remote files, should they not exist locally
const remoteData = new Map();
remoteData.set("./public/json/data.json", "https://www.garlandtools.org/db/doc/core/en/3/data.json");
remoteData.set("./public/json/dc.json", "https://xivapi.com/servers/dc");
remoteData.forEach((url, filePath) => {
    if (!fs.existsSync(filePath)) {
        (async () => {
            let data = await request(url);
            fs.writeFileSync(filePath, data);
        })();
    }
});

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

router.post("/upload", async (ctx) => {
    if (!ctx.is("json")) {
        ctx.throw(415);
        return;
    }

    let input = ctx.request.body;

    // TODO sanitation
    try {
        let marketBoardData = <MarketBoardListingsUpload> input;
        let listingArray: MarketBoardItemListing[] = [];
        for (let i = 1; i <= 10; i++) {
            if (marketBoardData[`listing${i}`]) {
                listingArray.push(marketBoardData[`listing${i}`]);
            }
        }
        priceTracker.set(marketBoardData.itemID, marketBoardData.worldID, listingArray);
    } catch {
        let marketBoardData = <MarketBoardSaleHistoryUpload> input;
        let entryArray: MarketBoardHistoryEntry[] = [];
        for (let i = 1; i <= 10; i++) {
            if (marketBoardData[`entry${i}`]) {
                entryArray.push(marketBoardData[`entry${i}`]);
            }
        }
        historyTracker.set(marketBoardData.itemID, marketBoardData.worldID, entryArray);
    }
});

universalis.use(router.routes());

// Start server
universalis.listen(3000);
