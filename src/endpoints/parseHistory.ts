/**
 * @name Market History
 * @url /api/history/:world/:item
 * @param world string | number The world or DC to retrieve data from.
 * @param item number The item to retrieve data for.
 */
import * as R from "remeda";

import {
	appendWorldDC,
	calcSaleVelocity,
	getItemIdEn,
	getItemNameEn,
	makeDistrTable,
} from "../util";

import { ParameterizedContext } from "koa";
import { Collection } from "mongodb";

import { HttpStatusCodes } from "../models/HttpStatusCodes";
import { MinimizedHistoryEntry } from "../models/MinimizedHistoryEntry";
import { RemoteDataManager } from "../remote/RemoteDataManager";

export async function parseHistory(
	ctx: ParameterizedContext,
	rdm: RemoteDataManager,
	worldMap: Map<string, number>,
	history: Collection,
) {
	let entriesToReturn: any = ctx.queryParams.entries;
	if (entriesToReturn)
		entriesToReturn = parseInt(entriesToReturn.replace(/[^0-9]/g, ""));

	const itemIDs: number[] = (ctx.params.item as string)
		.split(",")
		.map((id, index) => {
			if (index > 100) return null;
			return parseInt(id);
		})
		.filter((id) => id != null)
		.map((id) => {
			// Special-casing for Firmament items
			// This is really shit and should be done differently.
			const name = getItemNameEn(id);
			const approvedId = getItemIdEn("Approved " + name);
			if (approvedId != null) return approvedId;
			return id;
		});

	if (itemIDs.length === 1) {
		const marketableItems = await rdm.getMarketableItemIDs();
		if (!marketableItems.includes(itemIDs[0])) {
			ctx.throw(HttpStatusCodes.NOT_FOUND);
		}
	}

	// Query construction
	const query = {
		itemID: {
			$in: itemIDs,
		},
	};
	appendWorldDC(query, worldMap, ctx);

	// Request database info
	let data = {
		itemIDs,
		items: await history
			.find(query, {
				projection: { _id: 0, uploaderID: 0 },
			})
			.toArray(),
	};
	appendWorldDC(data, worldMap, ctx);

	// Data filtering
	data.items = data.items.map(
		(item: {
			entries: MinimizedHistoryEntry[];
			stackSizeHistogram: { [key: number]: number };
			stackSizeHistogramNQ: { [key: number]: number };
			stackSizeHistogramHQ: { [key: number]: number };
			regularSaleVelocity: number;
			nqSaleVelocity: number;
			hqSaleVelocity: number;
			lastUploadTime: number;
		}) => {
			if (entriesToReturn)
				item.entries = item.entries.slice(0, Math.min(1500, entriesToReturn));
			item.entries = item.entries.map((entry: MinimizedHistoryEntry) => {
				delete entry.uploaderID;
				return entry;
			});

			item.entries.sort((a, b) => b.pricePerUnit - a.pricePerUnit); // Sort in descending order

			const nqItems = item.entries.filter((entry) => !entry.hq);
			const hqItems = item.entries.filter((entry) => entry.hq);

			item.stackSizeHistogram = makeDistrTable(
				...item.entries.map((entry) =>
					entry.quantity != null ? entry.quantity : 0,
				),
			);
			item.stackSizeHistogramNQ = makeDistrTable(
				...nqItems.map((entry) =>
					entry.quantity != null ? entry.quantity : 0,
				),
			);
			item.stackSizeHistogramHQ = makeDistrTable(
				...hqItems.map((entry) =>
					entry.quantity != null ? entry.quantity : 0,
				),
			);

			item.regularSaleVelocity = calcSaleVelocity(
				...item.entries.map((entry) => entry.timestamp),
			);
			item.nqSaleVelocity = calcSaleVelocity(
				...nqItems.map((entry) => entry.timestamp),
			);
			item.hqSaleVelocity = calcSaleVelocity(
				...hqItems.map((entry) => entry.timestamp),
			);

			// Error handling
			if (!item.lastUploadTime) item.lastUploadTime = 0;
			return item;
		},
	);

	// Fill in unresolved items
	const resolvedItems: number[] = data.items.map((item) => item.itemID);
	const unresolvedItems: number[] = R.difference(itemIDs, resolvedItems);
	data["unresolvedItems"] = unresolvedItems;

	for (const item of unresolvedItems) {
		const unresolvedItemData = {
			entries: [],
			itemID: item,
			lastUploadTime: 0,
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
