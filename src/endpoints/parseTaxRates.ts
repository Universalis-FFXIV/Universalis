import { ParameterizedContext } from "koa";

import { ExtraDataManager } from "../db/ExtraDataManager";

import { MarketTaxRates } from "../models/MarketTaxRates";

export async function parseTaxRates(ctx: ParameterizedContext, extraDataManager: ExtraDataManager) {
    const taxRates: MarketTaxRates = await extraDataManager.getTaxRates();

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
