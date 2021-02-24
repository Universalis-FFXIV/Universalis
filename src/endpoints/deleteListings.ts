/**
 * @name Delete Listings
 * @url /api/:worldDC/:itemID
 */

import { ParameterizedContext } from "koa";
import { HttpStatusCodes } from "../models/HttpStatusCodes";

export interface DeleteListingsRequest {
	apiKey: string;
	listingID: string;
}

export async function deleteListings(ctx: ParameterizedContext) {
	const req: DeleteListingsRequest = ctx.request.body;
	if (!req.apiKey) {
		ctx.throw(HttpStatusCodes.UNAUTHENTICATED);
	}

	if (!req.listingID) {
		ctx.throw(HttpStatusCodes.BAD_REQUEST);
	}

	// validate API key
	// look for listing ID
	// delete the listing
}
