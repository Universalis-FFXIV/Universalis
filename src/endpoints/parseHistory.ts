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
	getDCWorlds,
	getItemIdEn,
	getItemNameEn,
	makeDistrTable,
} from "../util";

import { ParameterizedContext } from "koa";
import { Collection } from "mongodb";

import { HttpStatusCodes } from "../models/HttpStatusCodes";
import { RemoteDataManager } from "../remote/RemoteDataManager";
import { WorldDCQuery } from "../models/WorldDCQuery";
import { MinimizedDCHistoryEntry } from "../models/MinimizedDCHistoryEntry";

interface BodgeHistoryResponseData {
	dcName?: string;
	worldID?: number;
	itemID: number;
	entries: MinimizedDCHistoryEntry[];
	stackSizeHistogram: { [key: number]: number };
	stackSizeHistogramNQ: { [key: number]: number };
	stackSizeHistogramHQ: { [key: number]: number };
	regularSaleVelocity: number;
	nqSaleVelocity: number;
	hqSaleVelocity: number;
	lastUploadTime: number;
}

export async function parseHistory(
	ctx: ParameterizedContext,
	rdm: RemoteDataManager,
	worldMap: Map<string, number>,
	worldIDMap: Map<number, string>,
	history: Collection,
) {
	let entriesToReturn: string | number = ctx.queryParams.entries;
	if (entriesToReturn) {
		// Maximum response items is 999999
		entriesToReturn = parseInt(
			(entriesToReturn as string).replace(/[^0-9]/g, "").substring(0, 6),
		);
	}

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
	const query: WorldDCQuery = {
		itemID: {
			$in: itemIDs,
		},
	};
	appendWorldDC(query, worldMap, ctx);

	const dcName = query.dcName;
	const isDC = !!query.dcName;
	if (isDC) {
		const worlds = await getDCWorlds(query.dcName);
		delete query.dcName;
		query.worldID = {
			$in: worlds.map((w) => worldMap.get(w)),
		};
	}

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

	const requestIsHq: boolean = (ctx.queryParams.hq as string) === "1";

	// Do some post-processing on resolved item histories.
	for (let i = data.items.length - 1; i >= 0; i--) {
		const item: BodgeHistoryResponseData = data.items[i];

		if (isDC) {
			// Add the world name to all listings
			const worldName = worldIDMap.get(item.worldID);
			item.entries = item.entries.map((l) => {
				if (!l.worldName) {
					l.worldName = worldName;
				}

				return l;
			});

			const otherItemOnDC: BodgeHistoryResponseData = data.items.find(
				(it) => it.itemID === item.itemID && it.worldID !== item.worldID,
			);
			if (otherItemOnDC) {
				// Merge this into the next applicable response item
				otherItemOnDC.entries = otherItemOnDC.entries.concat(item.entries);
				otherItemOnDC.lastUploadTime = Math.max(
					otherItemOnDC.lastUploadTime,
					item.lastUploadTime,
				);
				// Remove this item from the array and continue
				data.items.splice(i, 1);
				continue;
			} else {
				// Delete the world ID so it doesn't show up for the user
				delete item.worldID;
				// Add the DC name to the response
				item.dcName = dcName;
			}
		}

		if (item.entries) {
			item.entries = R.pipe(
				item.entries,
				R.sort((a, b) => b.timestamp - a.timestamp), // Sort in descending order
				R.filter((entry) => !requestIsHq || entry.hq),
				R.take(entriesToReturn ? Math.max(0, entriesToReturn as number) : 1800), // Limit entries, default 1800
				R.map((entry) => {
					delete entry.uploaderID;
					return entry;
				}),
			);

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
		} else {
			item.entries = [];
		}
	}

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
