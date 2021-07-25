/**
 * @name Delete Listings
 * @url /api/:world/:item/delete
 */

import { ParameterizedContext } from "koa";
import { Collection } from "mongodb";
import { TrustedSourceManager } from "../db/TrustedSourceManager";
import { GenericUpload } from "../models/GenericUpload";
import { HttpStatusCodes } from "../models/HttpStatusCodes";
import { MarketBoardListingsEndpoint } from "../models/MarketBoardListingsEndpoint";
import { removeOld } from "../util";

import sha from "sha.js";
import { BlacklistManager } from "../db/BlacklistManager";
import { DeleteRequest } from "../models/DeleteRequest";
import { MarketBoardItemListing } from "../models/MarketBoardItemListing";

export async function deleteListings(
	ctx: ParameterizedContext,
	blacklistManager: BlacklistManager,
	trustedSourceManager: TrustedSourceManager,
	worldMap: Map<string, number>,
	recentData: Collection,
) {
	// validate API key
	const apiKey = ctx.get("Authorization"); // TODO: require the authorization header on uploads, too
	if (!apiKey || !(await trustedSourceManager.get(apiKey))) {
		ctx.throw(HttpStatusCodes.FORBIDDEN);
	}

	// parse world ID
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
		ctx.throw(HttpStatusCodes.BAD_REQUEST);
	}

	const itemID = parseInt(ctx.params.item as string);

	await removeOld(recentData, worldID as number, itemID); // Remove old records while we're at it

	const itemInfo: MarketBoardListingsEndpoint = await recentData.findOne({
		worldID,
		itemID,
	});
	if (itemInfo == null || itemInfo.listings == null) {
		ctx.body = "Success";
		return;
	}

	// parse the request body
	const uploadData: GenericUpload = ctx.request.body;

	console.log(uploadData);

	if (!uploadData.uploaderID) {
		ctx.throw(HttpStatusCodes.BAD_REQUEST);
	}

	uploadData.uploaderID = sha("sha256")
		.update(uploadData.uploaderID.toString())
		.digest("hex");

	if (blacklistManager.has(uploadData.uploaderID)) {
		ctx.body = "Success";
		return;
	}

	const deleteRequest = uploadData as DeleteRequest;

	// delete the listing
	await recentData.findOneAndUpdate(
		{ worldID, itemID },
		{
			$pull: {
				listings: {
					retainerID: sha("sha256")
						.update(deleteRequest.retainerID.toString())
						.digest("hex"),
					quantity: deleteRequest.quantity,
					pricePerUnit: deleteRequest.pricePerUnit,
				} as MarketBoardItemListing,
			},
		},
	);

	ctx.body = "Success";
}
