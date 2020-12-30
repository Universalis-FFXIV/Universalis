import { MarketBoardHistoryEntry } from "./MarketBoardHistoryEntry";
import { MarketBoardItemListing } from "./MarketBoardItemListing";

export interface MarketBoardListingsEndpoint {
	itemID: number;
	lastUploadTime: number;
	listings?: MarketBoardItemListing[];
	recentHistory?: MarketBoardHistoryEntry[];
	worldID?: number;
	dcName?: string;

	averagePrice?: number;
	averagePriceNQ?: number;
	averagePriceHQ?: number;

	currentAveragePrice?: number;
	currentAveragePriceNQ?: number;
	currentAveragePriceHQ?: number;

	saleVelocity?: number;
	saleVelocityNQ?: number;
	saleVelocityHQ?: number;
	saleVelocityUnits?: "per day";

	stackSizeHistogram?: { [key: number]: number };
	stackSizeHistogramNQ?: { [key: number]: number };
	stackSizeHistogramHQ?: { [key: number]: number };
}
