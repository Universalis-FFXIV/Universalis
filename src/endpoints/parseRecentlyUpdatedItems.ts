/**
 * @name Recently-Updated Items
 * @url /api/extra/stats/recently-updated
 * @param entries number The number of entries to return.
 * @returns items RecentlyUpdated The most-recently-updated items, up to "entries". This endpoint is not very useful, and as such should be considered deprecated in favor of /api/extra/stats/most-recently-updated.
 */

import { ParameterizedContext } from "koa";

import { ExtraDataManager } from "../db/ExtraDataManager";

import { RecentlyUpdated } from "../models/RecentlyUpdated";

export async function parseRecentlyUpdatedItems(
	ctx: ParameterizedContext,
	extraDataManager: ExtraDataManager,
) {
	let entriesToReturn: any = ctx.queryParams.entries;
	if (entriesToReturn)
		entriesToReturn = parseInt(entriesToReturn.replace(/[^0-9]/g, ""));

	const data: RecentlyUpdated = await extraDataManager.getRecentlyUpdatedItems(
		entriesToReturn,
	);

	if (!data) {
		ctx.body = {
			items: [],
		} as RecentlyUpdated;
		return;
	}

	ctx.body = data;
}
