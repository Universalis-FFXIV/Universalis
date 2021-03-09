/**
 * @name Delete Listings
 * @url /api/:world/:item/:listing
 */

import { ParameterizedContext } from "koa";
import { Collection } from "mongodb";
import { TrustedSourceManager } from "../db/TrustedSourceManager";
import { HttpStatusCodes } from "../models/HttpStatusCodes";
import { MarketBoardListingsEndpoint } from "../models/MarketBoardListingsEndpoint";
import { capitalise, removeOld } from "../util";

export async function deleteListings(ctx: ParameterizedContext, trustedSourceManager: TrustedSourceManager, worldMap: Map<string, number>, recentData: Collection) {
	if (!ctx.params.listingID) {
		ctx.throw(HttpStatusCodes.BAD_REQUEST);
	}

	// validate API key
	const apiKey = ctx.get("Authorization"); // TODO: require the authorization header on uploads, too
	if (!apiKey || !await trustedSourceManager.get(apiKey)) {
		ctx.throw(HttpStatusCodes.FORBIDDEN);
	}

	// look for listing ID
	let worldID: string | number = ctx.params.world
		? capitalise(ctx.params.world)
		: null;

	if (worldID && !parseInt(worldID)) {
		worldID = worldMap.get(worldID);
	} else if (parseInt(worldID)) {
		worldID = parseInt(worldID);
	}

	if (typeof worldID !== "number") {
		return;
	}

	let itemID = parseInt(ctx.params.item as string);

	await removeOld(recentData, worldID, itemID); // Remove old records while we're at it

	const itemInfo: MarketBoardListingsEndpoint = await recentData.findOne({ worldID, itemID });
	if (itemInfo == null) {
		return;
	}
	
	// delete the listing
	const toDelete = itemInfo.listings?.find(l => l.listingID == ctx.params.listing);
	if (toDelete == null) {
		return;
	}

	await recentData.findOneAndUpdate({ worldID, itemID }, {
		$set: {
			listings: itemInfo.listings.filter(l => l != ctx.params.listing),
		}
	});
}
