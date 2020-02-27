import request from "request-promise";

import { ITransport } from "./ITransport";
import { MarketBoardItemListing } from "../models/MarketBoardItemListing";

import { getWorldDC } from "../util";
import { Logger } from "winston";

const BASE_URL = "https://ff14marketnoteapi.ownway.info/research/1/market_research";

const lodestoneKeys = require("../resources/lodestoneKeys.json");

export class EorzeanMarketNoteTransport implements ITransport {
    name = "Eorzean Market Note";
    cachedData: Map<string, any>;
    logger: Logger;

    constructor(logger: Logger) {
        this.cachedData = new Map();
        this.logger = logger;
    }

    async fetchData(itemId: number, world: string): Promise<MarketBoardItemListing[]> {
        let dc = await getWorldDC(world);

        let data = this.cachedData.get(dc);
        if (!data || Date.now() - data.requestTime > 180000) {
            try {
                data.apiResponse = await request(BASE_URL + `?dc=${dc}`);
            } catch (err) {
                this.logger.error(err);
                return [];
            }
            data.aggregateDate = new Date(data.apiResponse.headers['last-modified']);
            data.requestTime = Date.now();

            this.cachedData.set(dc, data);
        }

        const dataInWorld = data.apiResponse.data[world];
        const latestMarketResearches = dataInWorld["l"] || {};

        const itemKey = lodestoneKeys[itemId];
        const itemL = latestMarketResearches[itemKey] || null;

        if (itemL) {
            const priceNQ = itemL[0];
            const priceHQ = itemL[1];
            const stockNQ = itemL[2];
            const stockHQ = itemL[3];
            const circulation1NQ = itemL[4];
            const circulation1HQ = itemL[5];
            const circulation2NQ = itemL[6];
            const circulation2HQ = itemL[7];
            const circulationNQ = circulation1NQ && circulation2NQ ? Math.round(24 * circulation1NQ / circulation2NQ) : null;
            const circulationHQ = circulation1HQ && circulation2HQ ? Math.round(24 * circulation1HQ / circulation2HQ) : null;
            const researchedTime = new Date(itemL[8]);

            // Map format to local structure somehow
        }

        return [];
    }
}
