import beep from "beepbeep";
import fs from "fs";
import path from "path";
import * as R from "remeda";
import sha from "sha.js";
import util from "util";

import { materiaIDToValueAndTier } from "./materiaUtils";

import { Context, ParameterizedContext } from "koa";

import { Logger } from "winston";
import { City } from "./models/City";
import { HttpStatusCodes } from "./models/HttpStatusCodes";
import { ItemMateria } from "./models/ItemMateria";
import { MarketBoardHistoryEntry } from "./models/MarketBoardHistoryEntry";
import { MarketBoardItemListing } from "./models/MarketBoardItemListing";
import { MarketBoardItemListingUpload } from "./models/MarketBoardItemListingUpload";
import { MarketTaxRates } from "./models/MarketTaxRates";
import { ValidateUploadDataArgs } from "./models/ValidateUploadDataArgs";

const exists = util.promisify(fs.exists);
const mkdir = util.promisify(fs.mkdir);
const writeFile = util.promisify(fs.writeFile);

const gameReleaseDateSeconds = Math.floor(
	new Date(2013, 7, 27).valueOf() / 1000,
);

export default {
	cleanHistoryEntry: (
		ctx: ParameterizedContext,
		entry: MarketBoardHistoryEntry,
		sourceName?: string,
	): MarketBoardHistoryEntry => {
		const stringifiedEntry = JSON.stringify(entry);
		if (hasHtmlTags(stringifiedEntry)) {
			ctx.throw(HttpStatusCodes.BAD_REQUEST);
		}

		const newEntry = R.pipe(
			entry,
			R.pick(["pricePerUnit", "quantity", "timestamp"]),
			R.merge({
				buyerName: removeUnsafeCharacters(entry.buyerName),
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
		ctx: ParameterizedContext,
		entry: MarketBoardHistoryEntry,
	): MarketBoardHistoryEntry => {
		const stringifiedEntry = JSON.stringify(entry);
		if (hasHtmlTags(stringifiedEntry)) {
			ctx.throw(HttpStatusCodes.BAD_REQUEST);
		}

		if (entry.quantity == null || entry.pricePerUnit == null) {
			return null;
		}

		return R.pipe(
			entry,
			R.pick(["hq", "pricePerUnit", "quantity", "timestamp", "worldName"]),
			R.merge({
				buyerName: removeUnsafeCharacters(entry.buyerName),
				total: entry.pricePerUnit * entry.quantity,
			}),
		);
	},

	cleanListing: (
		ctx: ParameterizedContext,
		listing: MarketBoardItemListingUpload,
		sourceName?: string,
	): MarketBoardItemListingUpload => {
		const stringifiedListing = JSON.stringify(listing);
		if (hasHtmlTags(stringifiedListing)) {
			ctx.throw(HttpStatusCodes.BAD_REQUEST);
		}

		const securedFields = {
			creatorID: parseSha256(listing.creatorID),
			listingID: parseSha256(listing.listingID),
			retainerID: parseSha256(listing.retainerID),
			sellerID: parseSha256(listing.sellerID),
		};

		const cleanedListing = {
			creatorName: removeUnsafeCharacters(listing.creatorName),
			hq: listing.hq || false,
			materia: listing.materia || [],
			onMannequin: listing.onMannequin || false,
			retainerCity:
				typeof listing.retainerCity === "number"
					? listing.retainerCity
					: City[listing.retainerCity],
			retainerName: removeUnsafeCharacters(listing.retainerName),
			uploadApplication: sourceName || listing.uploadApplication,
			lastReviewTime:
				listing.lastReviewTime < gameReleaseDateSeconds
					? Math.floor(new Date().valueOf() / 1000) - listing.lastReviewTime
					: listing.lastReviewTime,
		};

		const newListing = R.pipe(
			listing,
			R.pick([
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
		ctx: ParameterizedContext,
		listing: MarketBoardItemListing,
	): MarketBoardItemListing => {
		const stringifiedListing = JSON.stringify(listing);
		if (hasHtmlTags(stringifiedListing)) {
			ctx.throw(HttpStatusCodes.BAD_REQUEST);
		}

		if (listing.quantity == null || listing.pricePerUnit == null) {
			return null;
		}

		const formattedListing = R.pipe(
			listing,
			R.pick([
				"lastReviewTime",
				"pricePerUnit",
				"quantity",
				"stainID",
				"worldName",
			]),
			R.merge({
				creatorName: removeUnsafeCharacters(listing.creatorName),
				creatorID: isHash(listing.creatorID) ? listing.creatorID : parseSha256(listing.creatorID),
				hq: listing.hq || false,
				isCrafted:
					listing.creatorID !==
						"5feceb66ffc86f38d952786c6d696c79c2dbc239dd4e91b46729d73a27fb57e9" && // 0n
					listing.creatorID !==
						"e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855", // ""
				listingID: isHash(listing.listingID) ? listing.listingID : parseSha256(listing.listingID),
				materia: listing.materia || [],
				onMannequin: listing.onMannequin || false,
				retainerCity:
					typeof listing.retainerCity === "number"
						? listing.retainerCity
						: City[listing.retainerCity],
				retainerID: isHash(listing.retainerID) ? listing.retainerID : parseSha256(listing.retainerID),
				retainerName: removeUnsafeCharacters(listing.retainerName),
				sellerID: isHash(listing.sellerID) ? listing.sellerID : parseSha256(listing.sellerID),
				total: listing.pricePerUnit * listing.quantity,
				lastReviewTime:
					listing.lastReviewTime < gameReleaseDateSeconds
						? Math.floor(new Date().valueOf() / 1000) - listing.lastReviewTime
						: listing.lastReviewTime,
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
		logger: Logger,
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
		// This does include Korean worlds for the time being.
		if (
			(args.uploadData.listings ||
				args.uploadData.entries ||
				args.uploadData.marketTaxRates) &&
			!isValidWorld(args.uploadData.worldID)
		) {
			logger.warn(
				`Data received for unsupported world ${args.uploadData.worldID}, rejecting.`,
			);
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
				logger.warn(
					`Data received for unsupported item ${args.uploadData.itemID}, rejecting.`,
				);
				args.ctx.body = "Unsupported Item";
				args.ctx.throw(HttpStatusCodes.NOT_FOUND);
			}
		}

		// Listings
		if (args.uploadData.listings) {
			if (!(await areListingsValid(logger, args.uploadData.listings))) {
				args.ctx.throw(
					HttpStatusCodes.UNPROCESSABLE_ENTITY,
					"Bad Listing Data",
				);
			}
		}

		// History entries
		if (args.uploadData.entries) {
			if (!(await areHistoryEntriesValid(logger, args.uploadData.entries))) {
				args.ctx.throw(
					HttpStatusCodes.UNPROCESSABLE_ENTITY,
					"Bad History Data",
				);
			}
		}

		// Market tax rates
		if (args.uploadData.marketTaxRates) {
			if (!(await areTaxRatesValid(logger, args.uploadData.marketTaxRates))) {
				args.ctx.throw(
					HttpStatusCodes.UNPROCESSABLE_ENTITY,
					"Bad Market Tax Rate Data",
				);
			}
		}

		// Crafter data
		if (
			args.uploadData.contentID &&
			!isValidName(args.uploadData.characterName)
		) {
			logger.warn("Recieved invalid character name, rejecting.");
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

function isHash(maybeHash: any): boolean {
	let maybierHash = "" + maybeHash
	return !parseInt(maybierHash) || maybierHash.length > 20
}

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

export async function areListingsValid(
	logger: Logger,
	listings: any[],
): Promise<boolean> {
	for (const listing of listings) {
		if (
			listing.hq == null ||
			!isValidUInt32(listing.pricePerUnit) ||
			!isValidUInt32(listing.quantity) ||
			listing.retainerID == null ||
			listing.retainerCity == null ||
			!isValidName(listing.retainerName) ||
			listing.sellerID == null
		) {
			logger.warn(
				`Received bad listing data, rejecting. Reason:\nlisting.hq == null: ${
					listing.hq == null
				}\n!isValidUInt32(listing.quantity): ${!isValidUInt32(
					listing.quantity,
				)}\nlisting.retainerID == null: ${
					listing.retainerID == null
				}\nlisting.retainerCity == null: ${
					listing.retainerCity == null
				}\n!isValidName(listing.retainerName): ${!isValidName(
					listing.retainerName,
				)}\nlisting.sellerID == null: ${listing.sellerID == null}`,
			);
			beep(2);
			return false;
		}
	}
	return true;
}

export async function areHistoryEntriesValid(
	logger: Logger,
	entries: MarketBoardHistoryEntry[],
): Promise<boolean> {
	for (const entry of entries) {
		if (
			entry.hq == null ||
			!isValidUInt32(entry.pricePerUnit) ||
			!isValidUInt32(entry.quantity) ||
			!isValidName(entry.buyerName)
		) {
			logger.warn(
				`Received bad history data, rejecting. Reason:\nentry.hq == null: ${
					entry.hq == null
				}\n!isValidUInt32(entry.pricePerUnit): ${!isValidUInt32(
					entry.pricePerUnit,
				)}\n!isValidUInt32(entry.quantity): ${!isValidUInt32(
					entry.quantity,
				)}\n!isValidName(entry.buyerName): ${!isValidName(entry.buyerName)}`,
			);
			await writeOutObject(logger, entry);
			return false;
		}
	}
	return true;
}

export async function areTaxRatesValid(
	logger: Logger,
	rates: MarketTaxRates,
): Promise<boolean> {
	if (
		!isValidTaxRate(rates.crystarium) ||
		!isValidTaxRate(rates.gridania) ||
		!isValidTaxRate(rates.ishgard) ||
		!isValidTaxRate(rates.kugane) ||
		!isValidTaxRate(rates.limsaLominsa) ||
		!isValidTaxRate(rates.uldah)
	) {
		logger.warn(
			`Recieved bad tax rate data, rejecting. Reason:\n!isValidTaxRate(rates.crystarium): ${!isValidTaxRate(
				rates.crystarium,
			)}\n!isValidTaxRate(rates.gridania): ${!isValidTaxRate(
				rates.gridania,
			)}\n!isValidTaxRate(rates.ishgard): ${!isValidTaxRate(
				rates.ishgard,
			)}\n!isValidTaxRate(rates.kugane): ${!isValidTaxRate(
				rates.kugane,
			)}\n!isValidTaxRate(rates.limsaLominsa): ${!isValidTaxRate(
				rates.limsaLominsa,
			)}\n!isValidTaxRate(rates.uldah): ${!isValidTaxRate(rates.uldah)}`,
		);
		return false;
	}
	return true;
}

function hasHtmlTags(input: string): boolean {
	if (input.match(/<[\s\S]*?>/)) return true;
	return false;
}

export function isValidName(input: any): boolean {
	if (typeof input !== "string") return false;
	if (input.length > 32) return false;
	if (removeUnsafeCharacters(input) !== input) return false;
	return true;
}

function isValidTaxRate(input: any): boolean {
	if (typeof input !== "number") return false;
	if (input < 0 || input > 5) return false;
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
export function isValidWorld(input: any): boolean {
	if (typeof input !== "number") return false;
	if (
		input <= 16 ||
		input >= 100 ||
		input === 25 ||
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

export async function writeOutObject(logger: Logger, obj: any) {
	const outDir = path.join(__dirname, "..", "badUploads");
	if (!(await exists(outDir))) {
		await mkdir(outDir);
	}

	const outFile = path.join(outDir, `${obj.itemID}.json`); // sloppy but eh
	if (!(await exists(outFile))) {
		await writeFile(outFile, JSON.stringify(obj));
	}

	logger.info(
		`Wrote out ${obj.itemID}.json. Please examine the contents of this file.`,
	);
	beep();
}

export function removeUnsafeCharacters(input: string): string {
	return input.replace(
		/[^a-zA-Z0-9'\- ·⺀-⺙⺛-⻳⼀-⿕々〇〡-〩〸-〺〻㐀-䶵一-鿃豈-鶴侮-頻並-龎]/gu,
		"",
	);
}

function parseSha256(input: any): string {
	return sha("sha256")
		.update("" + input)
		.digest("hex");
}
