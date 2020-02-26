import request from "request-promise";

import { ITransport } from "./ITransport";
import { MarketBoardItemListing } from "../models/MarketBoardItemListing";

import { getWorldDC } from "../util";

const BASE_URL = "https://ff14marketnoteapi.ownway.info/research/1/market_research";

export class EorzeanMarketNoteTransport implements ITransport {
    name = "Eorzean Market Note";
    cachedData: Map<string, any>;

    constructor() {
        this.cachedData = new Map();
    }

    async fetchData(itemId: number, world: string): Promise<MarketBoardItemListing[]> {
        const dc = await getWorldDC(world);

        let data = this.cachedData.get(dc);
        if (!data || Date.now() - data.requestTime > 180000) {
            data.apiResponse = await request(BASE_URL + `?dc=${dc}`);
            data.aggregateDate = new Date(data.apiResponse.headers['last-modified']);
            data.requestTime = Date.now();

            this.cachedData.set(dc, data);
        }

        // Convert item ID to Lodestone key
        // Map format to local structure
        return [];
    }
}
