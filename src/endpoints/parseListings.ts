/**
 * @name Market Listings
 * @url /api/:world/:item
 * @param world string | number The world or DC to retrieve data from.
 * @param item number The item to retrieve data for.
 */

import * as R from "remeda";

import {
	appendWorldDC,
	calcSaleVelocity,
	calcStandardDeviation,
	calcTrimmedAverage,
	getWorldDC,
	makeDistrTable,
} from "../util";
import validation from "../validate";

import { ParameterizedContext } from "koa";
import { Collection } from "mongodb";

import { AveragePrices } from "../models/AveragePrices";
import { MarketBoardHistoryEntry } from "../models/MarketBoardHistoryEntry";
import { MarketBoardItemListingUpload } from "../models/MarketBoardItemListingUpload";
import { MarketBoardListingsEndpoint } from "../models/MarketBoardListingsEndpoint";
import { SaleVelocitySeries } from "../models/SaleVelocitySeries";
import { StackSizeHistograms } from "../models/StackSizeHistograms";
import { WorldDCQuery } from "../models/WorldDCQuery";
import { TransportManager } from "../transports/TransportManager";
import { getResearch } from "./parseEorzeanMarketNote";
import { HttpStatusCodes } from "../models/HttpStatusCodes";
import { RemoteDataManager } from "../remote/RemoteDataManager";

export async function parseListings(
	ctx: ParameterizedContext,
	rdm: RemoteDataManager,
	worldMap: Map<string, number>,
	recentData: Collection,
	transportManager: TransportManager,
) {
	const itemIDs: number[] = (ctx.params.item as string)
		.split(",")
		.map((id, index) => {
			if (index > 100) return;
			return parseInt(id);
		});

	const marketableItems = await rdm.getMarketableItemIDs();
	const diff = R.difference(itemIDs, marketableItems);
	if (diff.length !== 0) {
		ctx.throw(HttpStatusCodes.NOT_FOUND, `Not Found (${diff.toString()})`);
	}

	// Query construction
	const query: WorldDCQuery = { itemID: { $in: itemIDs } };
	appendWorldDC(query, worldMap, ctx);

	// Request database info
	let data = {
		itemIDs,
		items: await recentData
			.find(query, { projection: { _id: 0, uploaderID: 0 } })
			.toArray(),
	};
	appendWorldDC(data, worldMap, ctx);

	// Do some post-processing on resolved item listings.
	for (let i = 0; i < data.items.length; i++) {
		const item: MarketBoardListingsEndpoint = data.items[i];

		if (item.listings) {
			const dc = await getWorldDC(item.worldID); // For conditional tax factoring, remove on CN 5.2
			const cnDCs = ["陆行鸟", "莫古力", "猫小胖"];
			item.listings = R.pipe(
				item.listings,
				R.sort((a, b) => a.pricePerUnit - b.pricePerUnit),
				R.map((listing) => {
					if (
						!listing.retainerID.length ||
						!listing.sellerID.length ||
						!listing.creatorID.length
					) {
						listing = validation.cleanListing(
							(listing as unknown) as MarketBoardItemListingUpload,
						) as any; // Something needs to be done about this
					}
					listing.materia = validation.cleanMateriaArray(listing.materia);
					if (!cnDCs.includes(dc) && !cnDCs.includes(item.dcName)) {
						listing.pricePerUnit = Math.ceil(listing.pricePerUnit * 1.05);
					}
					listing = validation.cleanListingOutput(listing);
					return listing;
				}),
			);
		} else {
			item.listings = [];
		}

		if (item.recentHistory) {
			/*const emnData = await getResearch(
				transportManager,
				item.itemID,
				item.worldID,
			);*/

			item.recentHistory = R.pipe(
				item.recentHistory,
				R.map((entry) => {
					return validation.cleanHistoryEntryOutput(entry);
				}),
			);

			const nqItems = item.recentHistory.filter((entry) => !entry.hq);
			const hqItems = item.recentHistory.filter((entry) => entry.hq);

			// Average sale velocities with EMN data
			const saleVelocities = calculateSaleVelocities(
				item.recentHistory,
				nqItems,
				hqItems,
			);
			/*saleVelocities.nqSaleVelocity =
				(saleVelocities.nqSaleVelocity + emnData.turnoverPerDayNQ) / 2;
			saleVelocities.hqSaleVelocity =
				(saleVelocities.hqSaleVelocity + emnData.turnoverPerDayHQ) / 2;*/

			data.items[i] = R.pipe(
				item,
				R.merge(saleVelocities),
				R.merge(calculateAveragePrices(item.recentHistory, nqItems, hqItems)),
				R.merge(makeStackSizeHistograms(item.recentHistory, nqItems, hqItems)),
			);
		} else {
			item.recentHistory = [];
		}
	}

	// Fill in unresolved items
	const resolvedItems: number[] = data.items.map((item) => item.itemID);
	const unresolvedItems: number[] = R.difference(itemIDs, resolvedItems);
	data["unresolvedItems"] = unresolvedItems;

	for (const item of unresolvedItems) {
		const unresolvedItemData = {
			itemID: item,
			lastUploadTime: 0,
			listings: [],
			recentHistory: [],
		};
		appendWorldDC(unresolvedItemData, worldMap, ctx);
		data.items.push(unresolvedItemData);
	}

	// If only one item is requested we just turn the whole thing into the one item.
	if (data.itemIDs.length === 1) {
		data = data.items[0];
	} else if (!unresolvedItems) {
		delete data["unresolvedItems"];
	}

	ctx.body = data;
}

/////////////////////
// PRIVATE METHODS //
/////////////////////
function calculateSaleVelocities(
	regularSeries: MarketBoardHistoryEntry[],
	nqSeries: MarketBoardHistoryEntry[],
	hqSeries: MarketBoardHistoryEntry[],
): SaleVelocitySeries {
	// Per day
	const regularSaleVelocity = calcSaleVelocity(
		...regularSeries.map((entry) => entry.timestamp),
	);
	const nqSaleVelocity = calcSaleVelocity(
		...nqSeries.map((entry) => entry.timestamp),
	);
	const hqSaleVelocity = calcSaleVelocity(
		...hqSeries.map((entry) => entry.timestamp),
	);
	return {
		regularSaleVelocity,
		nqSaleVelocity,
		hqSaleVelocity,
	};
}

function calculateAveragePrices(
	regularSeries: MarketBoardHistoryEntry[],
	nqSeries: MarketBoardHistoryEntry[],
	hqSeries: MarketBoardHistoryEntry[],
): AveragePrices {
	const ppu = regularSeries.map((entry) => entry.pricePerUnit);
	const nqPpu = nqSeries.map((entry) => entry.pricePerUnit);
	const hqPpu = hqSeries.map((entry) => entry.pricePerUnit);
	const averagePrice = calcTrimmedAverage(
		calcStandardDeviation(...ppu),
		...ppu,
	);
	const averagePriceNQ = calcTrimmedAverage(
		calcStandardDeviation(...nqPpu),
		...nqPpu,
	);
	const averagePriceHQ = calcTrimmedAverage(
		calcStandardDeviation(...hqPpu),
		...hqPpu,
	);
	return {
		averagePrice,
		averagePriceNQ,
		averagePriceHQ,
	};
}

function makeStackSizeHistograms(
	regularSeries: MarketBoardHistoryEntry[],
	nqSeries: MarketBoardHistoryEntry[],
	hqSeries: MarketBoardHistoryEntry[],
): StackSizeHistograms {
	const stackSizeHistogram = makeDistrTable(
		...regularSeries.map((entry) => entry.quantity),
	);
	const stackSizeHistogramNQ = makeDistrTable(
		...nqSeries.map((entry) => entry.quantity),
	);
	const stackSizeHistogramHQ = makeDistrTable(
		...hqSeries.map((entry) => entry.quantity),
	);
	return {
		stackSizeHistogram,
		stackSizeHistogramNQ,
		stackSizeHistogramHQ,
	};
}
