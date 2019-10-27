import { ParameterizedContext } from "koa";

import { ExtraDataManager } from "../db/ExtraDataManager";

import { TaxRates } from "../models/TaxRates";

export async function parseTaxRates(ctx: ParameterizedContext, extraDataManager: ExtraDataManager) {
    const taxRates: TaxRates = await extraDataManager.getTaxRates();

    if (!taxRates) {
        ctx.body = {
            "Limsa Lominsa": null,
            "Gridania": null,
            "Ul'dah": null,
            "Ishgard": null,
            "Kugane": null,
            "Crystarium": null
        };
        return;
    }

    ctx.body = taxRates;
}
