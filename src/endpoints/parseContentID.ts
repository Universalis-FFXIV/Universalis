import { ParameterizedContext } from "koa";

import { ContentIDCollection } from "../db/ContentIDCollection";

export async function parseContentID(ctx: ParameterizedContext, contentIDCollection: ContentIDCollection) {
    const content = await contentIDCollection.get(ctx.params.contentID);

    if (!content) {
        ctx.body = {
            contentID: null,
            contentType: null
        };
        return;
    }

    ctx.body = content;
}
