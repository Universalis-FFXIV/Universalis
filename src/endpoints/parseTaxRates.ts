import { ParameterizedContext } from "koa";

import { ExtraDataManager } from "../db/ExtraDataManager";

import { TaxRates } from "../models/TaxRates";

export async function parseTaxRates(ctx: ParameterizedContext, extraDataManager: ExtraDataManager) {
    const taxRates: TaxRates = await extraDataManager.getTaxRates();

    if (!taxRates) {
        ctx.body = {
            "Crystarium": null,
            "Gridania": null,
            "Ishgard": null,
            "Kugane": null,
            "Limsa Lominsa": null,
            "Ul'dah": null
        };
        return;
    }

    ctx.body = taxRates;
}
