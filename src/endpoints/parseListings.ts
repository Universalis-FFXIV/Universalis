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
	calcTrimmedStats,
	getDCWorlds,
	getItemIdEn,
	getItemNameEn,
	makeDistrTable,
} from "../util";
import validation from "../validate";

import { ParameterizedContext } from "koa";
import { Collection } from "mongodb";

import { CurrentStats } from "../models/CurrentStats";
import { HttpStatusCodes } from "../models/HttpStatusCodes";
import { MarketBoardHistoryEntry } from "../models/MarketBoardHistoryEntry";
import { MarketBoardItemListing } from "../models/MarketBoardItemListing";
import { MarketBoardItemListingUpload } from "../models/MarketBoardItemListingUpload";
import { MarketBoardListingsEndpoint } from "../models/MarketBoardListingsEndpoint";
import { SaleVelocitySeries } from "../models/SaleVelocitySeries";
import { StackSizeHistograms } from "../models/StackSizeHistograms";
import { Stats } from "../models/Stats";
import { WorldDCQuery } from "../models/WorldDCQuery";
import { RemoteDataManager } from "../remote/RemoteDataManager";
import { TransportManager } from "../transports/TransportManager";
import { getResearch } from "./parseEorzeanMarketNote";

export async function parseListings(
	ctx: ParameterizedContext,
	rdm: RemoteDataManager,
	worldMap: Map<string, number>,
	worldIDMap: Map<number, string>,
	recentData: Collection,
	transportManager: TransportManager,
) {
	const itemIDs: number[] = (ctx.params.item as string)
		.split(",")
		.map((id, index) => {
			if (index > 100) return null;
			return parseInt(id);
		})
		.filter((id) => id != null)
		.map((id) => {
			// Special-casing for Firmament items
			// This is really shit and should be done differently.
			const name = getItemNameEn(id);
			const approvedId = getItemIdEn("Approved " + name);
			if (approvedId != null) return approvedId;
			return id;
		});

	if (itemIDs.length === 1) {
		const marketableItems = await rdm.getMarketableItemIDs();
		if (!marketableItems.includes(itemIDs[0])) {
			ctx.throw(HttpStatusCodes.NOT_FOUND);
		}
	}

	// Query construction
	const query: WorldDCQuery = {
		itemID: {
			$in: itemIDs,
		},
	};
	appendWorldDC(query, worldMap, ctx);

	const dcName = query.dcName;
	const isDC = !!query.dcName;
	if (isDC) {
		const worlds = await getDCWorlds(query.dcName);
		delete query.dcName;
		query.worldID = {
			$in: worlds.map(w => worldMap.get(w)),
		};
	}

	// Request database info
	let data = {
		itemIDs,
		items: await recentData
			.find(query, { projection: { _id: 0, uploaderID: 0 } })
			.toArray(),
	};
	appendWorldDC(data, worldMap, ctx);

	const requestIsHq: boolean = (ctx.queryParams.hq as string) === "1";

	// Do some post-processing on resolved item listings.
	for (let i = data.items.length - 1; i >= 0; i--) {
		const item: MarketBoardListingsEndpoint = data.items[i];
		
		if (isDC) {
			// Add the world name to all listings
			const worldName = worldIDMap.get(item.worldID);
			item.listings = item.listings.map(l => {
				if (!l.worldName) {
					l.worldName = worldName;
				}

				return l;
			});

			const otherItemOnDC: MarketBoardListingsEndpoint = data.items.find(it => it.itemID === item.itemID && it.worldID !== item.worldID);
			if (otherItemOnDC) {
				// Handle undefined fields?
				otherItemOnDC.listings = otherItemOnDC.listings || [];
				otherItemOnDC.recentHistory = otherItemOnDC.recentHistory || [];
				otherItemOnDC.lastUploadTime = otherItemOnDC.lastUploadTime || 0;
				// Merge this into the next applicable response item
				otherItemOnDC.listings = otherItemOnDC.listings.concat(item.listings);
				otherItemOnDC.recentHistory = otherItemOnDC.recentHistory.concat(item.recentHistory);
				otherItemOnDC.lastUploadTime = Math.max(otherItemOnDC.lastUploadTime, item.lastUploadTime);
				// Remove this item from the array and continue
				data.items.splice(i, 1);
				continue;
			} else {
				// Delete the world ID so it doesn't show up for the user 
				delete item.worldID;
				// Add the DC name to the response
				item.dcName = dcName;
			}
		}

		if (item.listings) {
			item.listings = R.pipe(
				item.listings,
				R.filter((listing) => listing != null),
				R.filter((listing) => !requestIsHq || listing.hq),
				R.sort((a, b) => a.pricePerUnit - b.pricePerUnit),
				R.map((listing) => {
					if (
						!listing.retainerID.length ||
						!listing.sellerID.length ||
						!listing.creatorID.length
					) {
						listing = validation.cleanListing(
							ctx,
							(listing as unknown) as MarketBoardItemListingUpload,
						) as any; // Something needs to be done about this
					}
					listing.materia = validation.cleanMateriaArray(listing.materia);
					listing.pricePerUnit = Math.ceil(listing.pricePerUnit * 1.05);
					listing = validation.cleanListingOutput(ctx, listing);
					return listing;
				}),
				R.filter((listing) => listing != null && listing.quantity !== 0),
			);

			const nqItems = item.listings.filter((listing) => !listing.hq);
			const hqItems = item.listings.filter((listing) => listing.hq);
			const curAvg = calculateCurrentAveragePrices(
				item.listings,
				nqItems,
				hqItems,
			);
			item.currentAveragePrice = curAvg.currentAveragePrice;
			item.currentAveragePriceNQ = curAvg.currentAveragePriceNQ;
			item.currentAveragePriceHQ = curAvg.currentAveragePriceHQ;
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
				R.filter((entry) => entry != null),
				R.map((entry) => {
					return validation.cleanHistoryEntryOutput(ctx, entry);
				}),
				R.filter((entry) => entry != null && entry.quantity !== 0),
			);

			const nqItems = item.listings.filter((entry) => !entry.hq);
			const hqItems = item.listings.filter((entry) => entry.hq);

			const nqItemsHistory = item.recentHistory.filter((entry) => !entry.hq);
			const hqItemsHistory = item.recentHistory.filter((entry) => entry.hq);

			const saleVelocities = calculateSaleVelocities(
				item.recentHistory,
				nqItemsHistory,
				hqItemsHistory,
			);

			// Average sale velocities with EMN data
			/*saleVelocities.nqSaleVelocity =
				(saleVelocities.nqSaleVelocity + emnData.turnoverPerDayNQ) / 2;
			saleVelocities.hqSaleVelocity =
				(saleVelocities.hqSaleVelocity + emnData.turnoverPerDayHQ) / 2;*/

			data.items[i] = R.pipe(
				item,
				R.merge(saleVelocities),
				R.merge(calculateAveragePrices(item.listings, nqItems, hqItems, item.recentHistory, nqItemsHistory, hqItemsHistory)),
				R.merge(makeStackSizeHistograms(item.listings, nqItems, hqItems)),
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
	regularSeries: MarketBoardItemListing[],
	nqSeries: MarketBoardItemListing[],
	hqSeries: MarketBoardItemListing[],
	regularSeriesHistory: MarketBoardHistoryEntry[],
	nqSeriesHistory: MarketBoardHistoryEntry[],
	hqSeriesHistory: MarketBoardHistoryEntry[],
): Stats {
	const ppu = regularSeries.map((entry) => entry.pricePerUnit);
	const nqPpu = nqSeries.map((entry) => entry.pricePerUnit);
	const hqPpu = hqSeries.map((entry) => entry.pricePerUnit);

	const historicalPpu = regularSeriesHistory.map((entry) => entry.pricePerUnit);
	const nqHistoricalPpu = nqSeriesHistory.map((entry) => entry.pricePerUnit);
	const hqHistoricalPpu = hqSeriesHistory.map((entry) => entry.pricePerUnit);

	const stats = calcTrimmedStats(calcStandardDeviation(...historicalPpu), ...historicalPpu);
	const statsNQ = calcTrimmedStats(calcStandardDeviation(...nqHistoricalPpu), ...nqHistoricalPpu);
	const statsHQ = calcTrimmedStats(calcStandardDeviation(...hqHistoricalPpu), ...hqHistoricalPpu);

	return {
		averagePrice: stats.mean,
		averagePriceNQ: statsNQ.mean,
		averagePriceHQ: statsHQ.mean,
		minPrice: Math.min(...ppu),
		minPriceNQ: Math.min(...nqPpu),
		minPriceHQ: Math.min(...hqPpu),
		maxPrice: Math.max(...ppu),
		maxPriceNQ: Math.max(...nqPpu),
		maxPriceHQ: Math.max(...hqPpu),
	};
}

function calculateCurrentAveragePrices(
	regularSeries: MarketBoardItemListing[],
	nqSeries: MarketBoardItemListing[],
	hqSeries: MarketBoardItemListing[],
)/*: CurrentStats */ {
	const ppu = regularSeries.map((listing) => listing.pricePerUnit);
	const nqPpu = nqSeries.map((listing) => listing.pricePerUnit);
	const hqPpu = hqSeries.map((listing) => listing.pricePerUnit);
	const stats = calcTrimmedStats(calcStandardDeviation(...ppu), ...ppu);
	const statsNQ = calcTrimmedStats(calcStandardDeviation(...nqPpu), ...nqPpu);
	const statsHQ = calcTrimmedStats(calcStandardDeviation(...hqPpu), ...hqPpu);
	return {
		currentAveragePrice: stats.mean,
		currentAveragePriceNQ: statsNQ.mean,
		currentAveragePriceHQ: statsHQ.mean,
	};
}

function makeStackSizeHistograms(
	regularSeries: MarketBoardItemListing[],
	nqSeries: MarketBoardItemListing[],
	hqSeries: MarketBoardItemListing[],
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
