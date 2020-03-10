/**
 * @url /api/extra/stats/world-upload-counts
 */

import { ParameterizedContext } from "koa";

import { ExtraDataManager } from "../db/ExtraDataManager";

import { WorldUploadCount } from "../models/WorldUploadCount";

export async function parseWorldUploadCounts(ctx: ParameterizedContext, extraDataManager: ExtraDataManager) {
    const worldUploadCounts = await extraDataManager.getWorldUploadCounts();

    const mergedEntries = {};

    let sum = 0;

    worldUploadCounts.forEach((worldUploadCount: WorldUploadCount) => {
        sum += worldUploadCount.count;
    });

    worldUploadCounts.forEach((worldUploadCount: WorldUploadCount) => {
        mergedEntries[worldUploadCount.worldName] = {
            count: worldUploadCount.count,
            proportion: worldUploadCount.count / sum
        };
    });

    ctx.body = mergedEntries;
}
