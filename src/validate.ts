import { Context } from "koa";

import { BlacklistManager } from "./BlacklistManager";

import { CharacterContentIDUpload } from "./models/CharacterContentIDUpload";
import { MarketBoardListingsUpload } from "./models/MarketBoardListingsUpload";
import { MarketBoardSaleHistoryUpload } from "./models/MarketBoardSaleHistoryUpload";

export default {
    validateUploadDataPreCast: (ctx: Context) => {
        if (!ctx.params.apiKey) {
            return ctx.throw(401);
        }

        if (!ctx.is("json")) {
            ctx.body = "Unsupported content type";
            return ctx.throw(422);
        }
    },

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

        // Listings
        if (uploadData.listings) uploadData.listings.forEach((listing) => {
            if (!listing.listingID ||
                    typeof(listing.hq) === "undefined" ||
                    !listing.pricePerUnit ||
                    !listing.quantity ||
                    !listing.retainerID ||
                    !listing.retainerName ||
                    !listing.sellerID ||
                    !listing.lastReviewTime) {
                ctx.throw(422);
            }
        });

        // History entries
        if (uploadData.entries) uploadData.entries.forEach((entry) => {
            if (typeof(entry.hq) === "undefined" ||
                    !entry.pricePerUnit ||
                    !entry.quantity ||
                    !entry.buyerName ||
                    !entry.timestamp ||
                    !entry.sellerID) {
                ctx.throw(422);
            }
        });

        // General filters
        if (!uploadData.worldID && !uploadData.itemID && !uploadData.contentID) {
            return ctx.throw(422);
        }

        if (!uploadData.listings && !uploadData.entries && !uploadData.contentID) {
            return ctx.throw(418);
        }
    }
}
