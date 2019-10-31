import commontags from "common-tags";
import sha from "sha.js";

// Utils
import validation from "../validate";

// Load models
import { CharacterContentIDUpload } from "../models/CharacterContentIDUpload";
import { City } from "../models/City";
import { MarketBoardHistoryEntry } from "../models/MarketBoardHistoryEntry";
import { MarketBoardItemListing } from "../models/MarketBoardItemListing";
import { MarketBoardListingsUpload } from "../models/MarketBoardListingsUpload";
import { MarketBoardSaleHistoryUpload } from "../models/MarketBoardSaleHistoryUpload";
import { TrustedSource } from "../models/TrustedSource";
import { UploadProcessParameters } from "../models/UploadProcessParameters";

export async function upload(parameters: UploadProcessParameters) {
    const blacklistManager = parameters.blacklistManager;
    const contentIDCollection = parameters.contentIDCollection;
    const ctx = parameters.ctx;
    const extraDataManager = parameters.extraDataManager;
    const historyTracker = parameters.historyTracker;
    const logger = parameters.logger;
    const priceTracker = parameters.priceTracker;
    const trustedSourceManager = parameters.trustedSourceManager;
    const worldIDMap = parameters.worldIDMap;

    let err = validation.validateUploadDataPreCast(ctx);
    if (err) {
        return err;
    }

    const promises: Array<Promise<any>> = []; // Sort of like a thread list.

    // Accept identity via API key.
    const trustedSource: TrustedSource = await trustedSourceManager.get(ctx.params.apiKey);
    if (!trustedSource) return ctx.throw(401);

    const sourceName = trustedSource.sourceName;

    promises.push(trustedSourceManager.increaseUploadCount(ctx.params.apiKey));

    logger.info("Received upload from " + sourceName + ":\n" + JSON.stringify(ctx.request.body));

    promises.push(extraDataManager.incrementDailyUploads());

    // Preliminary data processing and metadata stuff
    if (ctx.request.body.retainerCity) ctx.request.body.retainerCity = City[ctx.request.body.retainerCity];
    const uploadData:
        CharacterContentIDUpload &
        MarketBoardListingsUpload &
        MarketBoardSaleHistoryUpload
    = ctx.request.body;

    uploadData.uploaderID = sha("sha256").update(uploadData.uploaderID + "").digest("hex");

    err = await validation.validateUploadData({ ctx, uploadData, blacklistManager });
    if (err) {
        return err;
    }

    if (uploadData.worldID) {
        promises.push(extraDataManager.incrementWorldUploads(worldIDMap.get(uploadData.worldID)));
    }

    // Hashing and passing data
    if (uploadData.listings) {
        const dataArray: MarketBoardItemListing[] = [];
        uploadData.listings = uploadData.listings.map((listing) => {
            const newListing = validation.cleanListing(listing);
            newListing.materia = validation.cleanMateria(newListing.materia);
            return newListing;
        });

        for (const listing of uploadData.listings) {
            if (listing.creatorID && listing.creatorName) {
                contentIDCollection.set(listing.creatorID, "player", {
                    characterName: listing.creatorName
                });
            }

            if (listing.retainerID && listing.retainerName) {
                contentIDCollection.set(listing.retainerID, "retainer", {
                    characterName: listing.retainerName
                });
            }

            dataArray.push(listing as any);

            // Set tax rates
            if (listing.totalTax) {
                const city: string = Object.keys(City).find((c) => City[c] === listing.retainerCity);
                const total = listing.total;
                const totalWithTax = total + listing.totalTax;
                const taxRate = Math.floor((totalWithTax - total) / total * 100);
                logger.info(`Setting tax rate for ${city}: ${taxRate}%`);
                promises.push(extraDataManager.setTaxRate(city, taxRate));
            } else {
                logger.error(commontags.stripIndents`
                    totalTax not found, please ask ${trustedSource.sourceName} to upload it!
                    Listing: ${JSON.stringify(listing)}
                    totalTax: ${listing.totalTax}
                `);
            }
        }

        // Post listing to DB
        promises.push(priceTracker.set(
            uploadData.uploaderID,
            uploadData.itemID,
            uploadData.worldID,
            dataArray as MarketBoardItemListing[]
        ));
    }

    if (uploadData.entries) {
        const dataArray: MarketBoardHistoryEntry[] = [];
        uploadData.entries = uploadData.entries.map((entry) => {
            return validation.cleanHistoryEntry(entry);
        });

        for (const entry of uploadData.entries) {
            dataArray.push(entry);
        }

        promises.push(historyTracker.set(
            uploadData.uploaderID,
            uploadData.itemID,
            uploadData.worldID,
            dataArray as MarketBoardHistoryEntry[]
        ));
    }

    if (uploadData.itemID) {
        promises.push(extraDataManager.addRecentlyUpdatedItem(uploadData.itemID));
    }

    if (uploadData.contentID && uploadData.characterName) {
        uploadData.contentID = sha("sha256").update(uploadData.contentID + "").digest("hex");

        promises.push(contentIDCollection.set(uploadData.contentID, "player", {
            characterName: uploadData.characterName
        }));
    }

    await Promise.all(promises);

    ctx.body = "Success";
}
