import { Context } from "koa";

import { BlacklistManager } from "./BlacklistManager";

import { CharacterContentIDUpload } from "./models/CharacterContentIDUpload";
import { MarketBoardListingsUpload } from "./models/MarketBoardListingsUpload";
import { MarketBoardSaleHistoryUpload } from "./models/MarketBoardSaleHistoryUpload";

export default {
    validateUploadData: async (ctx: Context, uploadData: CharacterContentIDUpload & MarketBoardListingsUpload &
                         MarketBoardSaleHistoryUpload, blacklistManager: BlacklistManager) => {
        // Check blacklisted uploaders (people who upload fake data)
        if (await blacklistManager.has(uploadData.uploaderID as string)) return ctx.throw(403);

        // You can't upload data for these worlds because you can't scrape it.
        // This does include Chinese and Korean worlds for the time being.
        if (uploadData.worldID && uploadData.worldID <= 16 ||
                uploadData.worldID >= 100 ||
                uploadData.worldID === 26 ||
                uploadData.worldID === 27 ||
                uploadData.worldID === 38 ||
                uploadData.worldID === 84) {
            ctx.body = "Unsupported World";
            return ctx.throw(404);
        }

        // General filters
        if (!uploadData.worldID && !uploadData.itemID && !uploadData.contentID) {
            ctx.body = "Unsupported content type";
            return ctx.throw(415);
        }

        if (!uploadData.listings && !uploadData.entries && !uploadData.contentID) {
            return ctx.throw(418);
        }
    }
}
