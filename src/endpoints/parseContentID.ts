/**
 * @name Content IDs
 * @url /api/extra/content/:contentID
 * @param contentID string The content ID of the content you wish to retrieve from the content database.
 * @returns contentID string The content ID of the object retrieved.
 * @returns contentType string The category of the object retrieved.
 * @experimental
 */

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
