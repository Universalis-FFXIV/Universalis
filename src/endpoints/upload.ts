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
import { MarketTaxRatesUpload } from "../models/MarketTaxRatesUpload";
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
        MarketBoardSaleHistoryUpload &
        MarketTaxRatesUpload
    = ctx.request.body;

    uploadData.uploaderID = sha("sha256").update(uploadData.uploaderID + "").digest("hex");

    err = await validation.validateUploadData({ ctx, uploadData, blacklistManager });
    if (err) {
        return err;
    }

    // Metadata
    if (uploadData.worldID) {
        promises.push(extraDataManager.incrementWorldUploads(worldIDMap.get(uploadData.worldID)));
    }

    if (uploadData.itemID) {
        promises.push(extraDataManager.addRecentlyUpdatedItem(uploadData.itemID));
    }

    // Hashing and passing data
    if (uploadData.listings) {
        const dataArray: MarketBoardItemListing[] = [];

        for (const listing of uploadData.listings) {
            // Ensures retainer and listing information exists
            const cleanListing = validation.cleanListing(listing);

            // Needs to be called separately because... reasons
            cleanListing.materia = validation.cleanMateria(cleanListing.materia);

            if (cleanListing.creatorID && cleanListing.creatorName) {
                contentIDCollection.set(cleanListing.creatorID, "player", {
                    characterName: cleanListing.creatorName
                });
            }

            contentIDCollection.set(cleanListing.retainerID, "retainer", {
                characterName: cleanListing.retainerName
            });

            dataArray.push(listing as any);
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

    if (uploadData.marketTaxRates) {
        promises.push(extraDataManager.setTaxRates(uploadData.uploaderID, uploadData.marketTaxRates));
    }

    if (uploadData.contentID && uploadData.characterName) {
        promises.push(contentIDCollection.set(uploadData.contentID, "player", {
            characterName: uploadData.characterName
        }));
    }

    await Promise.all(promises);

    ctx.body = "Success";
}
