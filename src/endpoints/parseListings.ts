import difference from "lodash.difference";

import { appendWorldDC, calcSaleVelocity, calcTrimmedAverage, makeDistrTable } from "../util";
import validation from "../validate";

import { ParameterizedContext } from "koa";
import { Collection } from "mongodb";

import { MarketBoardItemListingUpload } from "../models/MarketBoardItemListingUpload";
import { MarketBoardHistoryEntry } from "../models/MarketBoardHistoryEntry";
import { MarketBoardListingsEndpoint } from "../models/MarketBoardListingsEndpoint";
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
    for (const item of data.items as MarketBoardListingsEndpoint[]) {
        // Regular stuff
        if (item.listings) {
            if (item.listings.length > 0) {
                item.listings = item.listings.sort((a, b) => {
                    if (a.pricePerUnit > b.pricePerUnit) return 1;
                    if (a.pricePerUnit < b.pricePerUnit) return -1;
                    return 0;
                });
            }
            item.listings = item.listings.map((listing) => {
                if (!listing.retainerID.length ||
                    !listing.sellerID.length ||
                    !listing.creatorID.length) {
                    listing = <any> validation.cleanListing(listing as unknown as MarketBoardItemListingUpload);
                }
                listing.materia = validation.cleanMateria(listing.materia);
                listing = validation.cleanListingOutput(listing);
                return listing;
            });
        } else {
            item.listings = [];
        }

        if (item.recentHistory) {
            item.recentHistory = item.recentHistory.map((entry) => {
                return validation.cleanHistoryEntryOutput(entry);
            });

            item.averagePrice = calcTrimmedAverage(...item.recentHistory
                .map((entry) => entry.pricePerUnit)
            );
            item.averagePriceNQ = calcTrimmedAverage(...item.recentHistory
                .filter((entry) => !entry.hq)
                .map((entry) => entry.pricePerUnit)
            );
            item.averagePriceHQ = calcTrimmedAverage(...item.recentHistory
                .filter((entry) => entry.hq)
                .map((entry) => entry.pricePerUnit)
            );

            item.saleVelocity = calcSaleVelocity(...item.recentHistory
                .map((entry) => entry.timestamp)
            );
            item.saleVelocityNQ = calcSaleVelocity(...item.recentHistory
                .filter((entry) => !entry.hq)
                .map((entry) => entry.timestamp)
            );
            item.saleVelocityHQ = calcSaleVelocity(...item.recentHistory
                .filter((entry) => entry.hq)
                .map((entry) => entry.timestamp)
            );
            item.saleVelocityUnits = "per day";

            item.stackSizeHistogram = makeDistrTable(
                ...item.recentHistory.map((entry: MarketBoardHistoryEntry) => entry.quantity)
            );
            item.stackSizeHistogramNQ = makeDistrTable(...item.recentHistory
                .filter((entry) => !entry.hq)
                .map((entry: MarketBoardHistoryEntry) => entry.quantity)
            );
            item.stackSizeHistogramHQ = makeDistrTable(...item.recentHistory
                .filter((entry) => entry.hq)
                .map((entry: MarketBoardHistoryEntry) => entry.quantity)
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
