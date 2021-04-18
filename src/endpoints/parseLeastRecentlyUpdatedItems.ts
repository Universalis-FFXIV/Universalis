/**
 * @name Least-Recently-Updated Items
 * @url /api/extra/stats/least-recently-updated
 * @param world string | number The world or DC to retrieve data from.
 * @param entries number The number of entries to return.
 * @returns items WorldItemPairList[] An array of world-item pairs for the least-recently-updated items.
 */

import { Redis } from "ioredis";
import { ParameterizedContext } from "koa";

import { ExtraDataManager } from "../db/ExtraDataManager";

import { WorldItemPairList } from "../models/WorldItemPairList";

export async function parseLeastRecentlyUpdatedItems(
	ctx: ParameterizedContext,
	worldMap: Map<string, number>,
	edm: ExtraDataManager,
	redis: Redis,
) {
	let worldID = ctx.query.world;
	let dcName = ctx.query.dcName;

	if (worldID && !parseInt(worldID)) {
		worldID = worldMap.get(worldID);
		if (!worldID && typeof worldID === "string")  {
			worldID = worldMap.get(worldID.charAt(0).toLocaleUpperCase() + worldID.substr(1).toLocaleLowerCase())
		}
	} else if (parseInt(worldID)) {
		worldID = parseInt(worldID);
	}

	if (worldID && dcName && worldID !== 0) {
		dcName = null;
	} else if (worldID && dcName && worldID === 0) {
		worldID = null;
	}

	if (dcName === "LuXingNiao") dcName = "陆行鸟";
	if (dcName === "MoGuLi") dcName = "莫古力";
	if (dcName === "MaoXiaoPang") dcName = "猫小胖";

	let entriesToReturn: any = ctx.queryParams.entries;
	if (entriesToReturn)
		entriesToReturn = parseInt(entriesToReturn.replace(/[^0-9]/g, ""));
	
	const redisKey = "lru-" + (worldID || dcName) + "-" + entriesToReturn;
	const existing = await redis.get(redisKey);
	if (existing) {
		ctx.body = JSON.parse(existing);
		return;
	}

	const data: WorldItemPairList = await edm.getLeastRecentlyUpdatedItems(
		worldID || dcName,
		entriesToReturn,
	);

	if (!data) {
		ctx.body = {
			items: [],
		} as WorldItemPairList;
		return;
	}

	await redis.set(redisKey, JSON.stringify(data), "EX", 60);

	ctx.body = data;
}
