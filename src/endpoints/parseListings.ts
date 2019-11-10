import difference from "lodash.difference";

import { appendWorldDC, calcAverage } from "../util";
import validation from "../validate";

import { ParameterizedContext } from "koa";
import { Collection } from "mongodb";
import { Logger } from "winston";

import { City } from "../models/City";
import { MarketBoardItemListing } from "../models/MarketBoardItemListing";
import { WorldDCQuery } from "../models/WorldDCQuery";

export async function parseListings(logger: Logger, ctx: ParameterizedContext, worldMap: Map<string, number>,
                                    worldIDMap: Map<number, string>, recentData: Collection) {
    const itemIDs: number[] = (ctx.params.item as string).split(",").map((id, index) => {
        if (index > 100) return;
        return parseInt(id);
    });

    // Query construction
    const query: WorldDCQuery = { itemID: { $in: itemIDs } };
    appendWorldDC(query, worldMap, ctx);

    // Request database info
    let data = {
        itemIDs,
        items: await recentData.find(query, { projection: { _id: 0, uploaderID: 0 } }).toArray()
    };
    appendWorldDC(data, worldMap, ctx);

    // Do some post-processing on resolved item listings.
    for (const item of data.items) {
        // Recovering from an error that screwed up merging world data into the DC file
        if (query.dcName) {
            const dcJSON = require("../../../public/dc.json");
            const worldIDs: number[] = [];
            dcJSON[query.dcName].forEach((worldName: string) => {
                worldIDs.push(worldMap.get(worldName));
            });
            const newQuery: WorldDCQuery = { worldID: { $in: worldIDs }, itemID: item.itemID };
            const newData = await recentData.find(newQuery, { projection: { _id: 0, uploaderID: 0 } }).toArray();
            item.listings = newData.map((worldData, index) => {
                return worldData.listings.map((listing: MarketBoardItemListing) => {
                    listing.worldName = worldIDMap.get(worldIDs[index]);
                    return listing;
                });
            });
            item.recentHistory = newData.map((worldData, index) => {
                return worldData.recentHistory.map((entry: MarketBoardItemListing) => {
                    entry.worldName = worldIDMap.get(worldIDs[index]);
                    return entry;
                });
            });
        }
        // Regular stuff
        if (item.listings) {
            if (item.listings.length > 0) {
                item.listings = item.listings.sort((a: MarketBoardItemListing, b: MarketBoardItemListing) => {
                    if (a.pricePerUnit > b.pricePerUnit) return 1;
                    if (a.pricePerUnit < b.pricePerUnit) return -1;
                    return 0;
                });
            }
            item.averagePrice = calcAverage(...item.listings.map((listing: MarketBoardItemListing) => {
                return listing.pricePerUnit;
            }));
            item.averagePriceNQ = calcAverage(...item.listings
                .filter((listing: MarketBoardItemListing) => !listing.hq)
                .map((listing: MarketBoardItemListing) => listing.pricePerUnit)
            );
            item.averagePriceHQ = calcAverage(...item.listings
                .filter((listing: MarketBoardItemListing) => listing.hq)
                .map((listing: MarketBoardItemListing) => listing.pricePerUnit)
            );
            item.listings = item.listings.map((listing) => {
                if (!listing.retainerID.length ||
                    !listing.sellerID.length ||
                    !listing.creatorID.length) {
                    listing = validation.cleanListing(listing);
                }
                listing.isCrafted =
                    listing.creatorID !== "5feceb66ffc86f38d952786c6d696c79c2dbc239dd4e91b46729d73a27fb57e9" && // 0n
                    listing.creatorID !== "e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855";   // ""
                listing.materia = validation.cleanMateria(listing.materia);
                listing.total = listing.pricePerUnit * listing.quantity;
                if (!parseInt(listing.retainerCity)) {
                    listing.retainerCity = City[listing.retainerCity];
                }
                return listing;
            });
        } else {
            item.listings = [];
        }

        if (item.recentHistory) {
            for (const entry of item.recentHistory) {
                if (entry.uploaderID) delete entry.uploaderID;
                entry.total = entry.pricePerUnit * entry.quantity;
            }
        } else {
            item.recentHistory = [];
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
        appendWorldDC(unresolvedItemData, worldMap, ctx);
        data.items.push(unresolvedItemData);
    }

    // If only one item is requested we just turn the whole thing into the one item.
    if (data.itemIDs.length === 1) {
        data = data.items[0];
    } else if (!unresolvedItems) {
        delete data["unresolvedItems"];
    }

    ctx.body = data;
}
