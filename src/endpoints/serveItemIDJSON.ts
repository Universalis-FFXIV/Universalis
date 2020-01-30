import { ParameterizedContext } from "koa";

import { RemoteDataManager } from "../remote/RemoteDataManager";

export async function serveItemIDJSON(ctx: ParameterizedContext, rdm: RemoteDataManager): Promise<any> {
    ctx.body = await rdm.getMarketableItemIDs();
}
