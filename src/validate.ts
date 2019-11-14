import compact from "lodash.compact";
import sha from "sha.js";

import { materiaIDToValueAndTier } from "./materiaUtils";

import { Context } from "koa";

import { City } from "./models/City";
import { ItemMateria } from "./models/ItemMateria";
import { MarketBoardHistoryEntry } from "./models/MarketBoardHistoryEntry";
import { MarketBoardItemListing } from "./models/MarketBoardItemListing";
import { MarketBoardItemListingUpload } from "./models/MarketBoardItemListingUpload";
import { ValidateUploadDataArgs } from "./models/ValidateUploadDataArgs";

export default {
    cleanHistoryEntry: (entry: MarketBoardHistoryEntry, sourceName?: string): MarketBoardHistoryEntry => {
        return {
            buyerName: entry.buyerName,
            hq: entry.hq,
            pricePerUnit: entry.pricePerUnit,
            quantity: entry.quantity,
            timestamp: entry.timestamp,
            uploadApplication: entry.uploadApplication ? entry.uploadApplication : sourceName,
        };
    },

    cleanHistoryEntryOutput: (entry: MarketBoardHistoryEntry): MarketBoardHistoryEntry => {
        return {
            buyerName: entry.buyerName,
            hq: entry.hq,
            pricePerUnit: entry.pricePerUnit,
            quantity: entry.quantity,
            timestamp: entry.timestamp,
            total: entry.pricePerUnit * entry.quantity,
            worldName: entry.worldName ? entry.worldName : undefined,
        };
    },

    cleanListing: (listing: MarketBoardItemListingUpload, sourceName?: string): MarketBoardItemListingUpload => {
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
            uploaderID: listing.uploaderID,
            worldName: listing["worldName"] ? listing["worldName"] : undefined,
        };

        if (sourceName) newListing["uploadApplication"] = sourceName;
        else if (listing["uploadApplication"]) newListing["uploadApplication"] = listing["uploadApplication"];

        return newListing;
    },

    cleanListingOutput: (listing: MarketBoardItemListing): MarketBoardItemListing => {
        const formattedListing = {
            creatorID: listing.creatorID,
            creatorName: listing.creatorName,
            hq: typeof listing.hq === "undefined" ? false : listing.hq,
            isCrafted:
                listing.creatorID !== "5feceb66ffc86f38d952786c6d696c79c2dbc239dd4e91b46729d73a27fb57e9" && // 0n
                listing.creatorID !== "e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855",   // ""
            lastReviewTime: listing.lastReviewTime,
            listingID: listing.listingID,
            materia: typeof listing.materia === "undefined" ? [] : listing.materia,
            onMannequin: typeof listing.onMannequin === "undefined" ? false : listing.onMannequin,
            pricePerUnit: listing.pricePerUnit,
            quantity: listing.quantity,
            retainerCity: typeof listing.retainerCity === "number" ?
                listing.retainerCity : City[listing.retainerCity],
            retainerID: listing.retainerID,
            retainerName: listing.retainerName,
            sellerID: listing.sellerID,
            stainID: listing.stainID,
            total: listing.pricePerUnit * listing.quantity,
            worldName: listing.worldName ? listing.worldName : undefined,
        };

        return formattedListing;
    },

    cleanMateria: (materiaArray: ItemMateria[]): ItemMateria[] => {
        if (materiaArray.length > 0) {
            materiaArray = materiaArray.map((materiaSlot) => {
                if (!materiaSlot.materiaID && materiaSlot["materiaId"]) {
                    materiaSlot.materiaID = materiaSlot["materiaId"];
                    delete materiaSlot["materiaId"];
                } else if (!materiaSlot.materiaID) {
                    return undefined;
                }

                if (!materiaSlot.slotID && materiaSlot["slotId"]) {
                    materiaSlot.slotID = materiaSlot["slotId"];
                    delete materiaSlot["slotId"];
                } else if (!materiaSlot.slotID) {
                    return undefined;
                }

                const materiaID = parseInt(materiaSlot.materiaID as unknown as string);
                if (materiaID > 973) {
                    const materiaData = materiaIDToValueAndTier(materiaID);
                    return {
                        materiaID: materiaData.materiaID,
                        slotID: materiaData.tier
                    };
                }

                return materiaSlot;
            });

            materiaArray = compact(materiaArray);
        }

        return materiaArray;
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
                    typeof entry.buyerName === "undefined") {
                args.ctx.throw(422, "Bad History Data");
                return true;
            }
        });

        // Market tax rates
        if (args.uploadData.marketTaxRates) {
            if (typeof args.uploadData.marketTaxRates.crystarium !== "number" ||
                    typeof args.uploadData.marketTaxRates.gridania !== "number" ||
                    typeof args.uploadData.marketTaxRates.ishgard !== "number" ||
                    typeof args.uploadData.marketTaxRates.kugane !== "number" ||
                    typeof args.uploadData.marketTaxRates.limsaLominsa !== "number" ||
                    typeof args.uploadData.marketTaxRates.uldah !== "number" ||
                    args.uploadData.marketTaxRates.crystarium < 0 ||
                    args.uploadData.marketTaxRates.crystarium > 5 ||
                    args.uploadData.marketTaxRates.gridania < 0 ||
                    args.uploadData.marketTaxRates.gridania > 5 ||
                    args.uploadData.marketTaxRates.ishgard < 0 ||
                    args.uploadData.marketTaxRates.ishgard > 5 ||
                    args.uploadData.marketTaxRates.kugane < 0 ||
                    args.uploadData.marketTaxRates.kugane > 5 ||
                    args.uploadData.marketTaxRates.limsaLominsa < 0 ||
                    args.uploadData.marketTaxRates.limsaLominsa > 5 ||
                    args.uploadData.marketTaxRates.uldah < 0 ||
                    args.uploadData.marketTaxRates.uldah > 5 ) {
                args.ctx.throw(422, "Bad Market Tax Rate Data");
                return true;
            }
        }

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
        if (!args.uploadData.worldID &&
                !args.uploadData.itemID &&
                !args.uploadData.marketTaxRates &&
                !args.uploadData.contentID) {
            args.ctx.throw(422);
            return true;
        }

        if (!args.uploadData.listings &&
                !args.uploadData.entries &&
                !args.uploadData.marketTaxRates &&
                !args.uploadData.contentID) {
            args.ctx.throw(418);
            return true;
        }

        return false;
    }
};
