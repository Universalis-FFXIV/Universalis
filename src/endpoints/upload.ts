import sha from "sha.js";

// Utils
import validation from "../validate";

// Load models
import { City } from "../models/City";
import { GenericUpload } from "../models/GenericUpload";
import { MarketBoardHistoryEntry } from "../models/MarketBoardHistoryEntry";
import { MarketBoardItemListing } from "../models/MarketBoardItemListing";
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
    const remoteDataManager = parameters.remoteDataManager;
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
    const uploadData: GenericUpload = ctx.request.body;

    uploadData.uploaderID = sha("sha256").update(uploadData.uploaderID + "").digest("hex");

    err = await validation.validateUploadData({ ctx, uploadData, blacklistManager, remoteDataManager });
    if (err) {
        return err;
    }

    // Metadata
    if (uploadData.worldID) {
        promises.push(extraDataManager.incrementWorldUploads(worldIDMap.get(uploadData.worldID)));
    }

    if (uploadData.itemID) {
        promises.push(extraDataManager.incrementPopularUploads(uploadData.itemID));
        promises.push(extraDataManager.addRecentlyUpdatedItem(uploadData.itemID));
    }

    // Hashing and passing data
    if (uploadData.listings) {
        const dataArray: MarketBoardItemListing[] = [];

        for (const listing of uploadData.listings) {
            // Ensures retainer and listing information exists
            const cleanListing = validation.cleanListing(listing, sourceName);

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
            sourceName,
            uploadData.itemID,
            uploadData.worldID,
            dataArray as MarketBoardItemListing[]
        ));
    }

    if (uploadData.entries) {
        const dataArray: MarketBoardHistoryEntry[] = [];
        uploadData.entries = uploadData.entries.map((entry) => {
            return validation.cleanHistoryEntry(entry, sourceName);
        });

        for (const entry of uploadData.entries) {
            dataArray.push(entry);
        }

        promises.push(historyTracker.set(
            uploadData.uploaderID,
            uploadData.itemID,
            uploadData.worldID,
            dataArray as MarketBoardHistoryEntry[],
        ));
    }

    if (uploadData.marketTaxRates) {
        promises.push(extraDataManager.setTaxRates(
            uploadData.uploaderID,
            sourceName,
            uploadData.worldID,
            uploadData.marketTaxRates,
        ));
    }

    if (uploadData.contentID && uploadData.characterName) {
        promises.push(contentIDCollection.set(uploadData.contentID, "player", {
            characterName: uploadData.characterName
        }));
    }

    // Bulk operations
    if (uploadData.op) {
        const op = uploadData.op;
        if (uploadData.itemIDs && uploadData.worldID && op.listings === -1) {
            if (uploadData.itemIDs.length <= 100) {
                for (const itemID of uploadData.itemIDs) {
                    promises.push(priceTracker.set(
                        uploadData.uploaderID,
                        sourceName,
                        itemID,
                        uploadData.worldID,
                        [],
                    ));
                }
            } else {
                logger.info("Attempted to run a bulk delisting of over 100 items, returning.");
                return ctx.throw(422);
            }
        }
    }

    await Promise.all(promises);

    ctx.body = "Success";
}
