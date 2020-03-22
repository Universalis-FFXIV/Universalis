/**
 * @url /api/tax-rates
 * @param world The world to retrieve data from.
 */

import { ParameterizedContext } from "koa";

import { ExtraDataManager } from "../db/ExtraDataManager";

import { MarketTaxRates } from "../models/MarketTaxRates";
import { capitalise } from "../util";

export async function parseTaxRates(ctx: ParameterizedContext, worldMap: Map<string, number>, extraDataManager: ExtraDataManager) {
    let worldID: string | number = ctx.queryParams.world ? capitalise(ctx.queryParams.world) : null;

    if (worldID && !parseInt(worldID)) {
        worldID = worldMap.get(worldID);
    } else if (parseInt(worldID)) {
        worldID = parseInt(worldID);
    }
    if (!worldID) return ctx.throw(404, "Invalid World");

    const taxRates: MarketTaxRates = await extraDataManager.getTaxRates(worldID as number);

    if (!taxRates) {
        ctx.body = {
            "Crystarium": null,
            "Gridania": null,
            "Ishgard": null,
            "Kugane": null,
            "Limsa Lominsa": null,
            "Ul'dah": null
        };
    } else {
        ctx.body = {
            "Crystarium": taxRates.crystarium,
            "Gridania": taxRates.gridania,
            "Ishgard": taxRates.ishgard,
            "Kugane": taxRates.kugane,
            "Limsa Lominsa": taxRates.limsaLominsa,
            "Ul'dah": taxRates.uldah
        };
    }
}
