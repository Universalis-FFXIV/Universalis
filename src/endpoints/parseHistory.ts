import difference from "lodash.difference";

import { appendWorldDC } from "../util";

import { ParameterizedContext } from "koa";
import { Collection } from "mongodb";

import { MinimizedHistoryEntry } from "../models/MinimizedHistoryEntry";

export async function parseHistory(ctx: ParameterizedContext, worldMap: Map<string, number>, history: Collection) {
    let entriesToReturn: any = ctx.queryParams.entries;
    if (entriesToReturn) entriesToReturn = parseInt(entriesToReturn.replace(/[^0-9]/g, ""));

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
        items: await history.find(query, {
            projection: { _id: 0, uploaderID: 0 }
        }).toArray()
    };
    appendWorldDC(data, worldMap, ctx);

    // Data filtering
    data.items = data.items.map((item) => {
        if (entriesToReturn) item.entries = item.entries.slice(0, Math.min(500, entriesToReturn));
        item.entries = item.entries.map((entry: MinimizedHistoryEntry) => {
            delete entry.uploaderID;
            return entry;
        });
        if (!item.lastUploadTime) item.lastUploadTime = 0;
        return item;
    });

    // Fill in unresolved items
    const resolvedItems: number[] = data.items.map((item) => item.itemID);
    const unresolvedItems: number[] = difference(itemIDs, resolvedItems);
    data["unresolvedItems"] = unresolvedItems;

    for (const item of unresolvedItems) {
        const unresolvedItemData = {
            entries: [],
            itemID: item,
            lastUploadTime: 0
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
