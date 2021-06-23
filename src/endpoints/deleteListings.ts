/**
 * @name Delete Listings
 * @url /api/:world/:item/delete
 */

import { ParameterizedContext } from "koa";
import { Collection } from "mongodb";
import { TrustedSourceManager } from "../db/TrustedSourceManager";
import { HttpStatusCodes } from "../models/HttpStatusCodes";
import { MarketBoardListingsEndpoint } from "../models/MarketBoardListingsEndpoint";
import { removeOld } from "../util";

export async function deleteListings(
	ctx: ParameterizedContext,
	trustedSourceManager: TrustedSourceManager,
	worldMap: Map<string, number>,
	recentData: Collection,
) {
	if (!ctx.params.listingID) {
		ctx.throw(HttpStatusCodes.BAD_REQUEST);
	}

	// validate API key
	const apiKey = ctx.get("Authorization"); // TODO: require the authorization header on uploads, too
	if (!apiKey || !(await trustedSourceManager.get(apiKey))) {
		ctx.throw(HttpStatusCodes.FORBIDDEN);
	}

	// look for listing ID
	let worldID = ctx.query.world;

	if (worldID && !parseInt(worldID)) {
		worldID = worldMap.get(worldID.charAt(0).toUpperCase() + worldID.substr(1));
		if (!worldID && typeof worldID === "string") {
			worldID = worldMap.get(
				worldID.charAt(0).toUpperCase() + worldID.substr(1).toLowerCase(),
			);
		}
	} else if (parseInt(worldID)) {
		worldID = parseInt(worldID);
	}

	if (typeof worldID !== "number") {
		return;
	}

	const itemID = parseInt(ctx.params.item as string);

	await removeOld(recentData, worldID as number, itemID); // Remove old records while we're at it

	const itemInfo: MarketBoardListingsEndpoint = await recentData.findOne({
		worldID,
		itemID,
	});
	if (itemInfo == null || itemInfo.listings == null) {
		return;
	}

	// delete the listing
	const toDelete = itemInfo.listings.find(
		(l) => l.listingID == ctx.params.listing,
	);
	if (toDelete == null) {
		return;
	}

	await recentData.findOneAndUpdate(
		{ worldID, itemID },
		{
			$set: {
				listings: itemInfo.listings.filter((l) => l != ctx.params.listing),
			},
		},
	);
}
