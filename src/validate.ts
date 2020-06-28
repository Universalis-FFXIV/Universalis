import * as R from "remeda";
import sha from "sha.js";

import { materiaIDToValueAndTier } from "./materiaUtils";

import { Context } from "koa";

import { City } from "./models/City";
import { HttpStatusCodes } from "./models/HttpStatusCodes";
import { ItemMateria } from "./models/ItemMateria";
import { MarketBoardHistoryEntry } from "./models/MarketBoardHistoryEntry";
import { MarketBoardItemListing } from "./models/MarketBoardItemListing";
import { MarketBoardItemListingUpload } from "./models/MarketBoardItemListingUpload";
import { ValidateUploadDataArgs } from "./models/ValidateUploadDataArgs";

export default {
	cleanHistoryEntry: (
		entry: MarketBoardHistoryEntry,
		sourceName?: string,
	): MarketBoardHistoryEntry => {
		const stringifiedEntry = JSON.stringify(entry);
		if (hasHtmlTags(stringifiedEntry)) {
			entry = JSON.parse(stringifiedEntry.replace(/<[\s\S]*?>/, ""));
		}

		const newEntry = R.pipe(
			entry,
			R.pick(["pricePerUnit", "quantity", "timestamp"]),
			R.merge({
				buyerName: entry.buyerName.replace(/[^a-zA-Z0-9'\- ]/g, ""),
				hq: entry.hq || false,
				uploadApplication: entry.uploadApplication || sourceName,
			}),
		);

		if (typeof newEntry.hq === "number") {
			// newListing.hq as a conditional will be truthy if not 0
			newEntry.hq = newEntry.hq ? true : false;
		}

		return newEntry;
	},

	cleanHistoryEntryOutput: (
		entry: MarketBoardHistoryEntry,
	): MarketBoardHistoryEntry => {
		const stringifiedEntry = JSON.stringify(entry);
		if (hasHtmlTags(stringifiedEntry)) {
			entry = JSON.parse(stringifiedEntry.replace(/<[\s\S]*?>/, ""));
		}

		return R.pipe(
			entry,
			R.pick(["hq", "pricePerUnit", "quantity", "timestamp", "worldName"]),
			R.merge({
				buyerName: entry.buyerName.replace(/[^a-zA-Z0-9'\- ]/g, ""),
				total: entry.pricePerUnit * entry.quantity,
			}),
		);
	},

	cleanListing: (
		listing: MarketBoardItemListingUpload,
		sourceName?: string,
	): MarketBoardItemListingUpload => {
		const stringifiedListing = JSON.stringify(listing);
		if (hasHtmlTags(stringifiedListing)) {
			listing = JSON.parse(stringifiedListing.replace(/<[\s\S]*?>/, ""));
		}

		const securedFields = {
			creatorID: parseSha256(listing.creatorID),
			listingID: parseSha256(listing.listingID),
			retainerID: parseSha256(listing.retainerID),
			sellerID: parseSha256(listing.sellerID),
		};

		const cleanedListing = {
			creatorName: listing.creatorName.replace(/[^a-zA-Z0-9'\- ]/g, ""),
			hq: listing.hq || false,
			materia: listing.materia || [],
			onMannequin: listing.onMannequin || false,
			retainerCity:
				typeof listing.retainerCity === "number"
					? listing.retainerCity
					: City[listing.retainerCity],
			retainerName: listing.retainerName.replace(/[^a-zA-Z0-9'\- ]/g, ""),
			uploadApplication: sourceName || listing.uploadApplication,
		};

		const newListing = R.pipe(
			listing,
			R.pick([
				"lastReviewTime",
				"pricePerUnit",
				"quantity",
				"stainID",
				"uploaderID",
				"worldName",
			]),
			R.merge(securedFields),
			R.merge(cleanedListing),
		);

		if (typeof newListing.hq === "number") {
			// newListing.hq as a conditional will be truthy if not 0
			newListing.hq = newListing.hq ? true : false;
		}

		return newListing;
	},

	cleanListingOutput: (
		listing: MarketBoardItemListing,
	): MarketBoardItemListing => {
		const stringifiedListing = JSON.stringify(listing);
		if (hasHtmlTags(stringifiedListing)) {
			listing = JSON.parse(stringifiedListing.replace(/<[\s\S]*?>/, ""));
		}

		const formattedListing = R.pipe(
			listing,
			R.pick([
				"creatorID",
				"lastReviewTime",
				"listingID",
				"pricePerUnit",
				"quantity",
				"retainerID",
				"sellerID",
				"stainID",
				"worldName",
			]),
			R.merge({
				creatorName: listing.creatorName.replace(/[^a-zA-Z0-9'\- ]/g, ""),
				hq: listing.hq || false,
				isCrafted:
					listing.creatorID !==
						"5feceb66ffc86f38d952786c6d696c79c2dbc239dd4e91b46729d73a27fb57e9" && // 0n
					listing.creatorID !==
						"e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855", // ""
				materia: listing.materia || [],
				onMannequin: listing.onMannequin || false,
				retainerCity:
					typeof listing.retainerCity === "number"
						? listing.retainerCity
						: City[listing.retainerCity],
				retainerName: listing.retainerName.replace(/[^a-zA-Z0-9'\- ]/g, ""),
				total: listing.pricePerUnit * listing.quantity,
			}),
		);

		return formattedListing;
	},

	cleanMateriaArray: (materiaArray: ItemMateria[]): ItemMateria[] => {
		return R.pipe(materiaArray, R.map(cleanMateria), R.compact);
	},

	validateUploadDataPreCast: (ctx: Context): void | never => {
		if (!ctx.params.apiKey) {
			ctx.throw(HttpStatusCodes.UNAUTHENTICATED);
		}

		if (!ctx.is("json") || hasHtmlTags(JSON.stringify(ctx.request.body))) {
			ctx.throw(HttpStatusCodes.UNSUPPORTED_MEDIA_TYPE);
		}
	},

	validateUploadData: async (
		args: ValidateUploadDataArgs,
	): Promise<void | never> => {
		// Check blacklisted uploaders (people who upload fake data)
		if (
			args.uploadData.uploaderID == null ||
			(await args.blacklistManager.has(args.uploadData.uploaderID as string))
		) {
			args.ctx.throw(HttpStatusCodes.FORBIDDEN);
		}

		// You can't upload data for these worlds because you can't scrape it.
		// This does include Chinese and Korean worlds for the time being.
		if (!isValidWorld(args.uploadData.worldID)) {
			args.ctx.body = "Unsupported World";
			args.ctx.throw(HttpStatusCodes.NOT_FOUND);
		}

		// Filter out junk item IDs.
		if (args.uploadData.itemID) {
			if (
				!(await args.remoteDataManager.getMarketableItemIDs()).includes(
					args.uploadData.itemID,
				)
			) {
				args.ctx.body = "Unsupported Item";
				args.ctx.throw(HttpStatusCodes.NOT_FOUND);
			}
		}

		// Listings
		if (args.uploadData.listings)
			args.uploadData.listings.forEach((listing) => {
				if (
					listing.hq == null ||
					!isValidUInt16(listing.lastReviewTime) ||
					!isValidUInt32(listing.pricePerUnit) ||
					!isValidUInt32(listing.quantity) ||
					listing.retainerID == null ||
					listing.retainerCity == null ||
					!isValidName(listing.retainerName) ||
					listing.sellerID == null
				) {
					args.ctx.throw(
						HttpStatusCodes.UNPROCESSABLE_ENTITY,
						"Bad Listing Data",
					);
				}
			});

		// History entries
		if (args.uploadData.entries)
			args.uploadData.entries.forEach((entry) => {
				if (
					entry.hq == null ||
					!isValidUInt32(entry.pricePerUnit) ||
					!isValidUInt32(entry.quantity) ||
					!isValidName(entry.buyerName)
				) {
					args.ctx.throw(
						HttpStatusCodes.UNPROCESSABLE_ENTITY,
						"Bad History Data",
					);
				}
			});

		// Market tax rates
		if (args.uploadData.marketTaxRates) {
			if (
				!isValidTaxRate(args.uploadData.marketTaxRates.crystarium) ||
				!isValidTaxRate(args.uploadData.marketTaxRates.gridania) ||
				!isValidTaxRate(args.uploadData.marketTaxRates.ishgard) ||
				!isValidTaxRate(args.uploadData.marketTaxRates.kugane) ||
				!isValidTaxRate(args.uploadData.marketTaxRates.limsaLominsa) ||
				!isValidTaxRate(args.uploadData.marketTaxRates.uldah)
			) {
				args.ctx.throw(
					HttpStatusCodes.UNPROCESSABLE_ENTITY,
					"Bad Market Tax Rate Data",
				);
			}
		}

		// Crafter data
		if (
			args.uploadData.characterName == null ||
			args.uploadData.contentID == null
		) {
			args.ctx.throw(HttpStatusCodes.UNPROCESSABLE_ENTITY);
		} else if (!isValidName(args.uploadData.characterName)) {
			args.ctx.throw(HttpStatusCodes.UNPROCESSABLE_ENTITY);
		}

		// General filters
		if (
			!args.uploadData.worldID &&
			!args.uploadData.itemID &&
			!args.uploadData.itemIDs &&
			!args.uploadData.marketTaxRates &&
			!args.uploadData.contentID &&
			!args.uploadData.op
		) {
			args.ctx.throw(HttpStatusCodes.UNPROCESSABLE_ENTITY);
		}

		if (
			!args.uploadData.listings &&
			!args.uploadData.entries &&
			!args.uploadData.marketTaxRates &&
			!args.uploadData.contentID &&
			!args.uploadData.op
		) {
			args.ctx.throw(HttpStatusCodes.IM_A_TEAPOT);
		}
	},
};

/////////////////////
// PRIVATE METHODS //
/////////////////////
function cleanMateria(materiaSlot: ItemMateria): ItemMateria {
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

	const materiaID = parseInt((materiaSlot.materiaID as unknown) as string);
	if (materiaID > 973) {
		const materiaData = materiaIDToValueAndTier(materiaID);
		return {
			materiaID: materiaData.materiaID,
			slotID: materiaData.tier,
		};
	}

	return materiaSlot;
}

function hasHtmlTags(input: string): boolean {
	if (input.match(/<[\s\S]*?>/)) return true;
	return false;
}

function isValidName(input: any): boolean {
	if (typeof input !== "string") return false;
	if (input.length > 32) return false;
	if (input.match(/[^a-zA-Z0-9'\- ]/g)) return false;
	return true;
}

function isValidTaxRate(input: any): boolean {
	if (typeof input !== "number") return false;
	if (input < 0 || input > 5) return false;
	return true;
}

function isValidUInt16(input: any): boolean {
	if (typeof input !== "number") return false;
	if (input < 0 || input > 65535) return false;
	return true;
}

function isValidUInt32(input: any): boolean {
	if (typeof input !== "number") return false;
	if (input < 0 || input > 4294967295) return false;
	return true;
}

const chineseWorldIds = [
	1042,
	1043,
	1044,
	1045,
	1060,
	1076,
	1081,
	1106,
	1113,
	1121,
	1166,
	1167,
	1169,
	1170,
	1171,
	1172,
	1173,
	1174,
	1175,
	1176,
	1177,
	1178,
	1179,
]; // Put this somewhere proper later
function isValidWorld(input: any): boolean {
	if (typeof input !== "number") return false;
	if (
		input <= 16 ||
		input >= 100 ||
		input === 26 ||
		input === 27 ||
		input === 38 ||
		input === 84
	) {
		if (!chineseWorldIds.includes(input)) {
			return false;
		}
	}
	return true;
}

function parseSha256(input: any): string {
	return sha("sha256")
		.update(input + "")
		.digest("hex");
}
