/**
 * @name Eorzean Market Note
 * @url /api/transports/eorzea-market-note/:world/:item
 * @param world string | number The world or DC to retrieve data from.
 * @param item string The item to retrieve data for.
 * @experimental
 * @disabled
 */

import { ParameterizedContext } from "koa";

import { IEorzeanMarketNoteResearch } from "../models/transports/IEorzeanMarketNoteResearch";
import { TransportManager } from "../transports/TransportManager";

import { getDCWorlds, getWorldDC } from "../util";

export async function parseEorzeanMarketNote(
	ctx: ParameterizedContext,
	transportManager: TransportManager,
) {
	const data: IEorzeanMarketNoteResearch = await getResearch(
		transportManager,
		ctx.params.item,
		ctx.params.world,
	);

	if (!data) {
		ctx.body = {
			itemID: ctx.params.item,
			world: ctx.params.world,
			priceNQ: null,
			priceHQ: null,
			stockNQ: null,
			stockHQ: null,
			circulationNQ: null,
			circulationHQ: null,
			researchedTime: null,
		} as IEorzeanMarketNoteResearch;
		return;
	}

	ctx.body = data;
}

/* Used to supplement statistics like sale velocity. */
export async function getResearch(
	transportManager: TransportManager,
	item: number,
	worldOrDc: number | string,
): Promise<IEorzeanMarketNoteResearch> {
	const dc = await getWorldDC(worldOrDc);
	const dcWorlds = dc ? await getDCWorlds(dc) : null;

	const transport = transportManager.getTransport("Eorzean Market Note");

	const data: IEorzeanMarketNoteResearch = await transport.fetchData(
		item,
		dc ? dcWorlds[0] : worldOrDc.toString(),
	);

	if (dc && data) {
		if ("world" in data) delete data.world;
		data.dc = dc;
		data.priceNQWorld = data.priceHQWorld = data.researchedTimeWorld = dcWorlds.shift();
		for (const world of dcWorlds) {
			const nextData: IEorzeanMarketNoteResearch = await transport.fetchData(
				item,
				world,
			);

			if (nextData.priceNQ < data.priceNQ) {
				data.priceNQ = nextData.priceNQ;
				data.priceNQWorld = world;
			}

			if (nextData.priceHQ < data.priceHQ) {
				data.priceHQ = nextData.priceHQ;
				data.priceHQWorld = world;
			}

			data.stockNQ += nextData.stockNQ;
			data.stockHQ += nextData.stockHQ;
			data.circulationNQ += nextData.circulationNQ;
			data.circulationHQ += nextData.circulationHQ;

			if (nextData.researchedTime < data.researchedTime) {
				data.researchedTime = nextData.researchedTime;
				data.researchedTimeWorld = world;
			}
		}
	}

	if (!data) {
		return {
			itemID: item,
			world: worldOrDc.toString(),
			priceNQ: null,
			priceHQ: null,
			stockNQ: null,
			stockHQ: null,
			circulationNQ: null,
			circulationHQ: null,
			turnoverPerDayNQ: null,
			turnoverPerDayHQ: null,
			researchedTime: null,
		};
	}

	return data;
}
