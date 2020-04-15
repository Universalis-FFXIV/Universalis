/**
 * @name Highest Sale Velocity Items
 * @url /api/extra/highest-sale-velocity
 * @param world string | number The world to retrieve data from.
 * @experimental
 * @disabled
 */

import { ParameterizedContext } from "koa";

import { Collection, Cursor } from "mongodb";
import { WorldItemPair } from "../models/WorldItemPair";
import { calcSaleVelocity, capitalise } from "../util";
import validation from "../validate";

export async function parseHighestSaleVelocityItems(
	ctx: ParameterizedContext,
	worldMap: Map<string, number>,
	worldIDMap: Map<number, string>,
	collection: Collection,
) {
	let worldID: string | number = ctx.queryParams.world
		? capitalise(ctx.queryParams.world)
		: null;

	if (worldID && !parseInt(worldID)) {
		worldID = worldMap.get(worldID);
	} else if (parseInt(worldID)) {
		worldID = parseInt(worldID);
	}
	if (!worldID) return ctx.throw(404, "Invalid World");
}

/** Get the so-and-so items with the highest sale velocity. */
async function getHighestSaleVelocity(
	worldID: number,
	count: number,
	worldIDMap: Map<number, string>,
	collection: Collection,
): Promise<WorldItemPair[]> {
	if (count <= 0) return [];

	const out: WorldItemPair[] = [];

	const query = { worldID };

	const highest: Cursor = collection
		.find(query, {
			projection: {
				itemID: 1,
				worldID: 1,
				recentHistory: 1,
				lastUploadTime: 1,
			},
		})
		.map((item) => {
			item.recentHistory = item.recentHistory.map((entry) => {
				return validation.cleanHistoryEntryOutput(entry);
			});

			item.saleVelocity = calcSaleVelocity(
				...item.recentHistory.map((entry) => entry.timestamp),
			);
		})
		.sort("saleVelocity", 1)
		.limit(Math.min(count, 20));

	highest.forEach((item) => {
		const outItem: WorldItemPair = {
			itemID: item.itemID,
			lastUploadTime: item.lastUploadTime,
			worldID,
			worldName: worldIDMap.get(worldID),
		};
		outItem["saleVelocity"] = item.saleVelocity;
		out.push(outItem);
	});

	return out;
}
