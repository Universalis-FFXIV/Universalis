// Dependencies
import fs from "fs";
import Koa from "koa";
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

// Get local copies of certain remote files, should they not exist locally
if (!fs.existsSync("./public/json/dc.json")) {
    (async () => {
        let data = await request("https://xivapi.com/servers/dc");
        fs.writeFileSync("./public/json/dc.json", data);
    })();
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
        time: Date.now()
    });
});

router.post("/upload", async (ctx) => {
    if (!ctx.is("json")) {
        ctx.throw(415);
        return;
    }

    let data = [];
    ctx.on("data", (chunk: string) => {
        data.push(chunk); // Concatenate the data stream
    });
    ctx.on("end", () => {
        let input = data.join("");
        // TODO sanitation
        try {
            let marketBoardData = <MarketBoardListingsUpload> JSON.parse(input);
            let listingArray: MarketBoardItemListing[] = [];
            for (let i = 1; i <= 10; i++) {
                if (marketBoardData[`listing${i}`]) {
                    listingArray.push(marketBoardData[`listing${i}`]);
                }
            }
            priceTracker.set(marketBoardData.itemID, marketBoardData.worldID, listingArray);
        } catch {
            let marketBoardData = <MarketBoardSaleHistoryUpload> JSON.parse(input);
            let entryArray: MarketBoardHistoryEntry[] = [];
            for (let i = 1; i <= 10; i++) {
                if (marketBoardData[`entry${i}`]) {
                    entryArray.push(marketBoardData[`entry${i}`]);
                }
            }
            historyTracker.set(marketBoardData.itemID, marketBoardData.worldID, entryArray);
        }
    });
});

universalis.use(router.routes());

// Start server
universalis.listen(3000);
