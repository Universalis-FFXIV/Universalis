import bent from "bent";

import { IEorzeanMarketNoteResearch } from "../models/transports/IEorzeanMarketNoteResearch";
import { ITransport } from "../models/transports/ITransport";

import { Logger } from "winston";
import { getWorldDC } from "../util";

const lodestoneKeys = require("../../public/json/lodestoneKeys.json");

const marketNote = bent(
	"https://ff14marketnoteapi.ownway.info/research/1/market_research",
	"GET",
	"json",
);

export class EorzeanMarketNoteTransport implements ITransport {
	public name = "Eorzean Market Note";
	private cachedData: Map<string, any>;
	private logger: Logger;

	constructor(logger: Logger) {
		this.cachedData = new Map();
		this.logger = logger;
	}

	public async fetchData(
		itemID: number,
		world: string,
	): Promise<IEorzeanMarketNoteResearch> {
		const result: any = {
			itemID,
			world,
		};

		// Get the data, or load it from the cache if it's recent enough.
		const dc = await getWorldDC(world);
		const data = this.cachedData.get(dc);
		if (!data || Date.now() - data.requestTime > 180000) {
			try {
				data.apiResponse = await marketNote(`?dc=${dc}`);
			} catch (err) {
				this.logger.error(err);
				return null;
			}
			data.aggregateDate = new Date(data.apiResponse.headers["last-modified"]);
			data.requestTime = Date.now();

			this.cachedData.set(dc, data);
		}

		const dataInWorld = data.apiResponse.data[world];
		const latestMarketResearches = dataInWorld["l"] || {};

		const itemKey: string = lodestoneKeys[itemID];
		const itemL: number[] = latestMarketResearches[itemKey] || null;

		// Map the data to a useful structure.
		if (itemL) {
			result.priceNQ = itemL[0];
			result.priceHQ = itemL[1];
			result.stockNQ = itemL[2];
			result.stockHQ = itemL[3];
			const circulation1NQ = itemL[4];
			const circulation1HQ = itemL[5];
			const circulation2NQ = itemL[6];
			const circulation2HQ = itemL[7];
			result.circulationNQ =
				circulation1NQ && circulation2NQ
					? Math.round((24 * circulation1NQ) / circulation2NQ)
					: null;
			result.circulationHQ =
				circulation1HQ && circulation2HQ
					? Math.round((24 * circulation1HQ) / circulation2HQ)
					: null;
			result.turnoverPerDayNQ = result.circulationNQ * result.priceNQ;
			result.turnoverPerDayHQ = result.circulationHQ * result.priceHQ;
			result.researchedTime = new Date(itemL[8]);

			return result as IEorzeanMarketNoteResearch;
		}

		return null;
	}
}
