/**
 * @name Most Demanded Items
 * @url /api/extra/most-demanded
 * @param world string | number The world to retrieve data from.
 * @experimental
 * @disabled
 */

import { ParameterizedContext } from "koa";
import { Collection, Cursor } from "mongodb";
import { ItemDemand } from "../models/ItemDemand";
import { capitalise } from "../util";

export async function parseMostDemandedItems(
	ctx: ParameterizedContext,
	worldMap: Map<string, number>,
	worldIDMap: Map<number, string>,
	history: Collection,
) {
	let worldID: string | number = ctx.queryParams.world
		? capitalise(ctx.queryParams.world)
		: null;

	if (worldID && !parseInt(worldID)) {
		worldID = worldMap.get(worldID);
	} else if (parseInt(worldID)) {
		worldID = parseInt(worldID);
	}

	let entriesToReturn: any = ctx.queryParams.entries;
	if (entriesToReturn)
		entriesToReturn = parseInt(entriesToReturn.replace(/[^0-9]/g, ""));

	ctx.body = await getMostDemandedItems(
		worldID as number,
		entriesToReturn,
		history,
	);
}

async function getMostDemandedItems(
	worldID: number,
	count: number,
	history: Collection,
): Promise<ItemDemand[]> {
	let out: ItemDemand[] = [];

	const query = {
		worldID,
		timestamp: {
			$gte: new Date().valueOf() - 86400000,
		},
	};

	const items = await history.find(query).forEach((item) => {
		const total: number = item.pricePerUnit * item.quantity;
		const existing = out.find((id) => id.itemID === item.itemID);
		if (!existing) {
			out.push({
				itemID: item.itemID,
				gilTradeVolumePerDay: total,
			});
			return;
		}
		existing.gilTradeVolumePerDay += total;
	});

	out = out
		.sort((a, b) => b.gilTradeVolumePerDay - a.gilTradeVolumePerDay) // Sort in descending order
		.slice(0, count);

	return out;
}
