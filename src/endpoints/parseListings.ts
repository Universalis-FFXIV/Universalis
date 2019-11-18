import difference from "lodash.difference";

import { appendWorldDC, calcTrimmedAverage } from "../util";
import validation from "../validate";

import { ParameterizedContext } from "koa";
import { Collection } from "mongodb";

import { MarketBoardItemListing } from "../models/MarketBoardItemListing";
import { MarketBoardHistoryEntry } from "../models/MarketBoardHistoryEntry";
import { WorldDCQuery } from "../models/WorldDCQuery";

export async function parseListings(ctx: ParameterizedContext, worldMap: Map<string, number>, recentData: Collection) {
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
        // Regular stuff
        if (item.listings) {
            if (item.listings.length > 0) {
                item.listings = item.listings.sort((a: MarketBoardItemListing, b: MarketBoardItemListing) => {
                    if (a.pricePerUnit > b.pricePerUnit) return 1;
                    if (a.pricePerUnit < b.pricePerUnit) return -1;
                    return 0;
                });
            }
            item.listings = item.listings.map((listing) => {
                if (!listing.retainerID.length ||
                    !listing.sellerID.length ||
                    !listing.creatorID.length) {
                    listing = validation.cleanListing(listing);
                }
                listing.materia = validation.cleanMateria(listing.materia);
                listing = validation.cleanListingOutput(listing);
                return listing;
            });
        } else {
            item.listings = [];
        }

        if (item.recentHistory) {
            item.recentHistory = item.recentHistory.map((entry: MarketBoardHistoryEntry) => {
                return validation.cleanHistoryEntryOutput(entry);
            });

            const oneWeekRecentHistory = item.recentHistory.filter((entry) => entry.timestamp <= 604800000);

            item.averagePrice = calcTrimmedAverage(...oneWeekRecentHistory
                .map((entry: MarketBoardHistoryEntry) => entry.pricePerUnit)
            );
            item.averagePriceNQ = calcTrimmedAverage(...oneWeekRecentHistory
                .filter((entry: MarketBoardHistoryEntry) => !entry.hq)
                .map((entry: MarketBoardHistoryEntry) => entry.pricePerUnit)
            );
            item.averagePriceHQ = calcTrimmedAverage(...oneWeekRecentHistory
                .filter((entry: MarketBoardHistoryEntry) => entry.hq)
                .map((entry: MarketBoardHistoryEntry) => entry.pricePerUnit)
            );
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
