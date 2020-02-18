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
        const newEntry = {
            buyerName: entry.buyerName,
            hq: entry.hq == null ? false : entry.hq,
            pricePerUnit: entry.pricePerUnit,
            quantity: entry.quantity,
            timestamp: entry.timestamp,
            uploadApplication: entry.uploadApplication ? entry.uploadApplication : sourceName,
        };

        if (typeof newEntry.hq === "number") {
            // newListing.hq as a conditional will be truthy if not 0
            newEntry.hq = newEntry.hq ? true : false;
        }

        return newEntry;
    },

    cleanHistoryEntryOutput: (entry: MarketBoardHistoryEntry): MarketBoardHistoryEntry => {
        return {
            buyerName: entry.buyerName,
            hq: entry.hq,
            pricePerUnit: entry.pricePerUnit,
            quantity: entry.quantity,
            timestamp: entry.timestamp,
            total: entry.pricePerUnit * entry.quantity,
            worldName: entry.worldName,
        };
    },

    cleanListing: (listing: MarketBoardItemListingUpload, sourceName?: string): MarketBoardItemListingUpload => {
        const newListing = {
            creatorID: sha("sha256").update(listing.creatorID + "").digest("hex"),
            creatorName: listing.creatorName,
            hq: listing.hq == null ? false : listing.hq,
            lastReviewTime: listing.lastReviewTime,
            listingID: sha("sha256").update(listing.listingID + "").digest("hex"),
            materia: listing.materia == null ? [] : listing.materia,
            onMannequin: listing.onMannequin == null ? false : listing.onMannequin,
            pricePerUnit: listing.pricePerUnit,
            quantity: listing.quantity,
            retainerCity: typeof listing.retainerCity === "number" ?
                listing.retainerCity : City[listing.retainerCity],
            retainerID: sha("sha256").update(listing.retainerID + "").digest("hex"),
            retainerName: listing.retainerName,
            sellerID: sha("sha256").update(listing.sellerID + "").digest("hex"),
            stainID: listing.stainID,
            uploadApplication: sourceName ? sourceName : listing.uploadApplication,
            uploaderID: listing.uploaderID,
            worldName: listing.worldName,
        };

        if (typeof newListing.hq === "number") {
            // newListing.hq as a conditional will be truthy if not 0
            newListing.hq = newListing.hq ? true : false;
        }

        return newListing;
    },

    cleanListingOutput: (listing: MarketBoardItemListing): MarketBoardItemListing => {
        const formattedListing = {
            creatorID: listing.creatorID,
            creatorName: listing.creatorName,
            hq: listing.hq == null ? false : listing.hq,
            isCrafted:
                listing.creatorID !== "5feceb66ffc86f38d952786c6d696c79c2dbc239dd4e91b46729d73a27fb57e9" && // 0n
                listing.creatorID !== "e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855",   // ""
            lastReviewTime: listing.lastReviewTime,
            listingID: listing.listingID,
            materia: listing.materia == null ? [] : listing.materia,
            onMannequin: listing.onMannequin == null ? false : listing.onMannequin,
            pricePerUnit: listing.pricePerUnit,
            quantity: listing.quantity,
            retainerCity: typeof listing.retainerCity === "number" ?
                listing.retainerCity : City[listing.retainerCity],
            retainerID: listing.retainerID,
            retainerName: listing.retainerName,
            sellerID: listing.sellerID,
            stainID: listing.stainID,
            total: listing.pricePerUnit * listing.quantity,
            worldName: listing.worldName,
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
                    return;
                }

                if (!materiaSlot.slotID && materiaSlot["slotId"]) {
                    materiaSlot.slotID = materiaSlot["slotId"];
                    delete materiaSlot["slotId"];
                } else if (!materiaSlot.slotID) {
                    return;
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
        }

        if (!ctx.is("json")) {
            ctx.body = "Unsupported content type";
            ctx.throw(415);
        }
    },

    validateUploadData: async (args: ValidateUploadDataArgs): Promise<boolean> => {
        // Check blacklisted uploaders (people who upload fake data)
        if (args.uploadData.uploaderID == null ||
            await args.blacklistManager.has(args.uploadData.uploaderID as string)) {
                args.ctx.throw(403);
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

        // Filter out junk item IDs.
        if (args.uploadData.itemID) {
            if (!(await args.remoteDataManager.getMarketableItemIDs()).includes(args.uploadData.itemID)) {
                args.ctx.body = "Unsupported Item";
                args.ctx.throw(404);
                return true;
            }
        }

        // Listings
        if (args.uploadData.listings) args.uploadData.listings.forEach((listing) => {
            if (listing.hq == null ||
                    listing.lastReviewTime == null ||
                    listing.pricePerUnit == null ||
                    listing.quantity == null ||
                    listing.retainerID == null ||
                    listing.retainerCity == null ||
                    listing.retainerName == null ||
                    listing.sellerID == null) {
                args.ctx.throw(422, "Bad Listing Data");
                return true;
            }
        });

        // History entries
        if (args.uploadData.entries) args.uploadData.entries.forEach((entry) => {
            if (entry.hq == null ||
                    entry.pricePerUnit == null ||
                    entry.quantity == null ||
                    entry.buyerName == null) {
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
        if (args.uploadData.contentID && args.uploadData.characterName == null) {
            args.ctx.throw(422);
            return true;
        }
        if (args.uploadData.characterName && args.uploadData.contentID == null) {
            args.ctx.throw(422);
            return true;
        }

        // General filters
        if (!args.uploadData.worldID &&
                !args.uploadData.itemID &&
                !args.uploadData.itemIDs &&
                !args.uploadData.marketTaxRates &&
                !args.uploadData.contentID &&
                !args.uploadData.op) {
            args.ctx.throw(422);
            return true;
        }

        if (!args.uploadData.listings &&
                !args.uploadData.entries &&
                !args.uploadData.marketTaxRates &&
                !args.uploadData.contentID &&
                !args.uploadData.op) {
            args.ctx.throw(418);
            return true;
        }

        return false;
    }
};
