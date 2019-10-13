import { Context } from "koa";
import sha from "sha.js";

import { City } from "./models/City";
import { MarketBoardHistoryEntry } from "./models/MarketBoardHistoryEntry";
import { MarketBoardItemListingUpload } from "./models/MarketBoardItemListingUpload";
import { ValidateUploadDataArgs } from "./models/ValidateUploadDataArgs";

export default {
    cleanHistoryEntry: (entry: MarketBoardHistoryEntry) => {
        return {
            buyerName: entry.buyerName,
            hq: entry.hq,
            pricePerUnit: entry.pricePerUnit,
            quantity: entry.quantity,
            sellerID: sha("sha256").update(entry.sellerID + "").digest("hex"),
            timestamp: entry.timestamp,
            total: entry.pricePerUnit * entry.quantity
        };
    },

    cleanListing: (listing: MarketBoardItemListingUpload): MarketBoardItemListingUpload => {
        const newListing = {
            creatorID: sha("sha256").update(listing.creatorID + "").digest("hex"),
            creatorName: listing.creatorName,
            hq: typeof listing.hq === "undefined" ? false : listing.hq,
            lastReviewTime: listing.lastReviewTime,
            listingID: sha("sha256").update(listing.listingID + "").digest("hex"),
            materia: typeof listing.materia === "undefined" ? [] : listing.materia,
            onMannequin: typeof listing.onMannequin === "undefined" ? false : listing.onMannequin,
            pricePerUnit: listing.pricePerUnit,
            quantity: listing.quantity,
            retainerCity: typeof listing.retainerCity === "number" ?
                listing.retainerCity : City[listing.retainerCity],
            retainerID: sha("sha256").update(listing.retainerID + "").digest("hex"),
            retainerName: listing.retainerName,
            sellerID: sha("sha256").update(listing.sellerID + "").digest("hex"),
            stainID: listing.stainID,
            total: listing.pricePerUnit * listing.quantity
        };

        return newListing;
    },

    validateUploadDataPreCast: (ctx: Context) => {
        if (!ctx.params.apiKey) {
            ctx.throw(401);
            return true;
        }

        if (!ctx.is("json")) {
            ctx.body = "Unsupported content type";
            ctx.throw(415);
            return true;
        }
    },

    validateUploadData: async (args: ValidateUploadDataArgs) => {
        // Check blacklisted uploaders (people who upload fake data)
        if (typeof args.uploadData.uploaderID === "undefined" ||
            await args.blacklistManager.has(args.uploadData.uploaderID as string)) {
                args.ctx.throw(403);
                return true;
        }

        // You can't upload data for these worlds because you can't scrape it.
        // This does include Chinese and Korean worlds for the time being.
        if (args.uploadData.worldID && args.uploadData.worldID <= 16 ||
                args.uploadData.worldID >= 100 ||
                args.uploadData.worldID === 26 ||
                args.uploadData.worldID === 27 ||
                args.uploadData.worldID === 38 ||
                args.uploadData.worldID === 84) {
            args.ctx.body = "Unsupported World";
            args.ctx.throw(404);
            return true;
        }

        // Listings
        if (args.uploadData.listings) args.uploadData.listings.forEach((listing) => {
            if (typeof listing.hq === "undefined" ||
                    typeof listing.lastReviewTime === "undefined" ||
                    typeof listing.pricePerUnit === "undefined" ||
                    typeof listing.quantity === "undefined" ||
                    typeof listing.retainerID === "undefined" ||
                    typeof listing.retainerCity === "undefined" ||
                    typeof listing.retainerName === "undefined" ||
                    typeof listing.sellerID === "undefined") {
                args.ctx.throw(422, "Bad Listing Data");
                return true;
            }
        });

        // History entries
        if (args.uploadData.entries) args.uploadData.entries.forEach((entry) => {
            if (typeof entry.hq === "undefined" ||
                    typeof entry.pricePerUnit === "undefined" ||
                    typeof entry.quantity === "undefined" ||
                    typeof entry.buyerName === "undefined" ||
                    typeof entry.sellerID === "undefined") {
                args.ctx.throw(422, "Bad History Data");
                return true;
            }
        });

        // Crafter data
        if (args.uploadData.contentID && typeof args.uploadData.characterName === "undefined") {
            args.ctx.throw(422);
            return true;
        }
        if (args.uploadData.characterName && typeof args.uploadData.contentID === "undefined") {
            args.ctx.throw(422);
            return true;
        }

        // General filters
        if (!args.uploadData.worldID && !args.uploadData.itemID && typeof args.uploadData.contentID === "undefined") {
            args.ctx.throw(422);
            return true;
        }

        if (!args.uploadData.listings && !args.uploadData.entries && typeof args.uploadData.contentID === "undefined") {
            args.ctx.throw(418);
            return true;
        }

        return false;
    }
};
