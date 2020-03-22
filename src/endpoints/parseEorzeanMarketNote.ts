/**
 * @url /api/transports/eorzea-market-note/:world/:item
 * @param world string | number The world or DC to retrieve data from.
 * @param item string The item to retrieve data for.
 * @experimental
 * @disabled
 */

import { ParameterizedContext } from "koa";

import { TransportManager } from "../transports/TransportManager";
import { IEorzeanMarketNoteResearch } from "../models/transports/IEorzeanMarketNoteResearch";

import { getWorldDC, getDCWorlds } from "../util";

export async function parseEorzeanMarketNote(ctx: ParameterizedContext, transportManager: TransportManager) {
    const dc = await getWorldDC(ctx.params.world);
    const dcWorlds = dc ? await getDCWorlds(dc) : null;

    const transport = transportManager.getTransport("Eorzean Market Note");

    const data: IEorzeanMarketNoteResearch = await transport.fetchData(ctx.params.item, dc ? dcWorlds[0] : ctx.params.world);

    if (dc) {
        delete data.world;
        data.dc = dc;
        data.priceNQWorld = data.priceHQWorld = data.researchedTimeWorld = dcWorlds.shift();
        for (const world of dcWorlds) {
            const nextData: IEorzeanMarketNoteResearch = await transport.fetchData(ctx.params.item, world);

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