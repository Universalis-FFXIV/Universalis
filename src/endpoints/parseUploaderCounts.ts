/**
 * @name Uploader Application Counts
 * @url /api/extra/stats/uploader-upload-counts
 * @returns  [{sourceName:string;uploadCount:number;}] An array of uploader source names and their respective total upload counts.
 */

import { ParameterizedContext } from "koa";

import { TrustedSourceManager } from "../db/TrustedSourceManager"

export async function parseUploaderCounts(ctx: ParameterizedContext, tsm: TrustedSourceManager): Promise<any> {
    const data = await tsm.getUploadersCount();

    ctx.body = data;
}
