/**
 * @url /api/extra/stats/upload-history
 * @param entries The number of entries to return.
 */

import { ParameterizedContext } from "koa";

import { ExtraDataManager } from "../db/ExtraDataManager";

import { DailyUploadStatistics } from "../models/DailyUploadStatistics";

export async function parseUploadHistory(ctx: ParameterizedContext, extraDataManager: ExtraDataManager) {
    let daysToReturn: any = ctx.queryParams.entries;
    if (daysToReturn) daysToReturn = parseInt(daysToReturn.replace(/[^0-9]/g, ""));

    const data: DailyUploadStatistics = await extraDataManager.getDailyUploads(daysToReturn);

    if (!data) {
        ctx.body =  {
            uploadCountByDay: []
        } as DailyUploadStatistics;
        return;
    }

    ctx.body = data;
}
