/**
 * @name Upload
 * @url /upload
 */

import * as R from "remeda";
import sha from "sha.js";

// Utils
import validation from "../validate";

// Load models
import { City } from "../models/City";
import { GenericUpload } from "../models/GenericUpload";
import { HttpStatusCodes } from "../models/HttpStatusCodes";
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

	validation.validateUploadDataPreCast(ctx);

	// Accept identity via API key.
	const trustedSource: TrustedSource = await trustedSourceManager.get(
		ctx.params.apiKey,
	);
	if (!trustedSource) return ctx.throw(HttpStatusCodes.FORBIDDEN);
	const sourceName = trustedSource.sourceName;
	await trustedSourceManager.increaseUploadCount(ctx.params.apiKey);

	logger.info(
		"Received upload from " +
			sourceName +
			":\n" +
			JSON.stringify(ctx.request.body),
	);
	await extraDataManager.incrementDailyUploads();

	// Preliminary data processing and metadata stuff
	if (ctx.request.body.retainerCity)
		ctx.request.body.retainerCity = City[ctx.request.body.retainerCity];
	const uploadData: GenericUpload = ctx.request.body;

	uploadData.uploaderID = sha("sha256")
		.update(uploadData.uploaderID.toString())
		.digest("hex");

	await validation.validateUploadData(logger, {
		ctx,
		uploadData,
		blacklistManager,
		remoteDataManager,
	});

	// Metadata
	if (uploadData.worldID) {
		await extraDataManager.incrementWorldUploads(
			worldIDMap.get(uploadData.worldID),
		);
	}

	if (uploadData.itemID) {
		await extraDataManager.incrementPopularUploads(uploadData.itemID);
		await extraDataManager.addRecentlyUpdatedItem(uploadData.itemID);
	}

	// Hashing and passing data
	if (uploadData.listings) {
		const dataArray: MarketBoardItemListing[] = [];

		for (const listing of uploadData.listings) {
			// Ensures retainer and listing information exists
			const cleanListing = validation.cleanListing(ctx, listing, sourceName);

			// Needs to be called separately because... reasons
			cleanListing.materia = validation.cleanMateriaArray(cleanListing.materia);

			if (cleanListing.creatorID && cleanListing.creatorName) {
				contentIDCollection.set(cleanListing.creatorID, "player", {
					characterName: cleanListing.creatorName,
				});
			}

			contentIDCollection.set(cleanListing.retainerID, "retainer", {
				characterName: cleanListing.retainerName,
			});

			dataArray.push(listing as any);
		}

		// Post listing to DB
		await priceTracker.set(
			uploadData.uploaderID,
			sourceName,
			uploadData.itemID,
			uploadData.worldID,
			dataArray as MarketBoardItemListing[],
		);
	}

	if (uploadData.entries) {
		const dataArray: MarketBoardHistoryEntry[] = [];
		uploadData.entries = uploadData.entries.map((entry) => {
			return validation.cleanHistoryEntry(ctx, entry, sourceName);
		});

		for (const entry of uploadData.entries) {
			dataArray.push(entry);
		}

		await historyTracker.set(
			uploadData.uploaderID,
			uploadData.itemID,
			uploadData.worldID,
			dataArray as MarketBoardHistoryEntry[],
		);
	}

	if (uploadData.marketTaxRates) {
		await extraDataManager.setTaxRates(
			uploadData.worldID,
			R.merge(uploadData.marketTaxRates, {
				uploaderID: uploadData.uploaderID,
				sourceName,
			}),
		);
	}

	if (uploadData.contentID && uploadData.characterName) {
		await contentIDCollection.set(uploadData.contentID, "player", {
			characterName: uploadData.characterName,
		});
	}

	// Bulk operations
	if (uploadData.op) {
		const op = uploadData.op;
		if (uploadData.itemIDs && uploadData.worldID && op.listings === -1) {
			if (uploadData.itemIDs.length <= 100) {
				for (const itemID of uploadData.itemIDs) {
					await priceTracker.set(
						uploadData.uploaderID,
						sourceName,
						itemID,
						uploadData.worldID,
						[],
					);
				}
			} else {
				logger.warn(
					"Attempted to run a bulk delisting of over 100 items, rejecting.",
				);
				return ctx.throw(HttpStatusCodes.UNPROCESSABLE_ENTITY);
			}
		}
	}

	ctx.body = "Success";
}
