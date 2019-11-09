import difference from "lodash.difference";

import { appendWorldDC, calcAverage } from "../util";
import validation from "../validate";

import { ParameterizedContext } from "koa";
import { Collection } from "mongodb";

import { City } from "../models/City";
import { MarketBoardItemListing } from "../models/MarketBoardItemListing";

export async function parseListings(ctx: ParameterizedContext, worldMap: Map<string, number>, recentData: Collection) {
    const itemIDs: number[] = (ctx.params.item as string).split(",").map((id, index) => {
        if (index > 100) return;
        return parseInt(id);
    });

    // Query construction
    const query = { itemID: { $in: itemIDs } };
    appendWorldDC(query, worldMap, ctx);

    // Request database info
    let data = {
        itemIDs,
        items: await recentData.find(query, { projection: { _id: 0, uploaderID: 0 } }).toArray()
    };
    appendWorldDC(data, worldMap, ctx);

    // Do some post-processing on resolved item listings.
    for (const item of data.items) {
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
            for (let listing of item.listings) {
                if (typeof listing.retainerID !== "string" ||
                    typeof listing.sellerID !== "string" ||
                    typeof listing.creatorID !== "string") {
                    listing = validation.cleanListing(listing);
                }

                listing.isCrafted =
                    listing.creatorID !== "5feceb66ffc86f38d952786c6d696c79c2dbc239dd4e91b46729d73a27fb57e9";
                listing.materia = validation.cleanMateria(listing.materia);
                if (!parseInt(listing.retainerCity)) {
                    listing.retainerCity = City[listing.retainerCity];
                }
            }
        } else {
            item.listings = [];
        }

        if (item.recentHistory) {
            for (const entry of item.recentHistory) {
                if (entry.uploaderID) delete entry.uploaderID;
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
