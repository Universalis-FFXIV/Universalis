import { ParameterizedContext } from "koa";

import { appendWorldDC } from "../util";

import { ExtraDataManager } from "../db/ExtraDataManager";

import { MarketTaxRates } from "../models/MarketTaxRates";

export async function parseTaxRates(ctx: ParameterizedContext,
                                    worldMap: Map<string, number>, extraDataManager: ExtraDataManager) {
    appendWorldDC({}, worldMap, ctx);
    if (!ctx.params.worldID) return ctx.throw(404, "Invalid World");
    const taxRates: MarketTaxRates = await extraDataManager.getTaxRates(ctx.params.worldID);

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
