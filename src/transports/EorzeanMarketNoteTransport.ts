import request from "request-promise";

import { IEorzeanMarketNoteResearch } from "../models/transports/IEorzeanMarketNoteResearch"
import { ITransport } from "../models/transports/ITransport";

import { getWorldDC } from "../util";
import { Logger } from "winston";

const BASE_URL = "https://ff14marketnoteapi.ownway.info/research/1/market_research";

const lodestoneKeys = {};//require("../resources/lodestoneKeys.json");

export class EorzeanMarketNoteTransport implements ITransport {
    name = "Eorzean Market Note";
    cachedData: Map<string, any>;
    logger: Logger;

    constructor(logger: Logger) {
        this.cachedData = new Map();
        this.logger = logger;
    }

    async fetchData(itemID: number, world: string): Promise<IEorzeanMarketNoteResearch> {
        const result: any = {
            itemID,
            world,
        };

        // Get the data, or load it from the cache if it's recent enough.
        let dc = await getWorldDC(world);
        let data = this.cachedData.get(dc);
        if (!data || Date.now() - data.requestTime > 180000) {
            try {
                data.apiResponse = await request(BASE_URL + `?dc=${dc}`);
            } catch (err) {
                this.logger.error(err);
                return null;
            }
            data.aggregateDate = new Date(data.apiResponse.headers['last-modified']);
            data.requestTime = Date.now();

            this.cachedData.set(dc, data);
        }

        const dataInWorld = data.apiResponse.data[world];
        const latestMarketResearches = dataInWorld["l"] || {};

        const itemKey: string = (lodestoneKeys[itemID] as string).slice(29, 41);
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
            result.circulationNQ = circulation1NQ && circulation2NQ ? Math.round(24 * circulation1NQ / circulation2NQ) : null;
            result.circulationHQ = circulation1HQ && circulation2HQ ? Math.round(24 * circulation1HQ / circulation2HQ) : null;
            result.researchedTime = new Date(itemL[8]);

            return result as IEorzeanMarketNoteResearch;
        }

        return null;
    }
}
