import { MarketBoardHistoryEntry } from "./MarketBoardHistoryEntry";
import { MarketBoardItemListing } from "./MarketBoardItemListing";

export interface MarketBoardListingsEndpoint {
    itemID: number;
    lastUploadTime: number;
    listings?: MarketBoardItemListing[];
    recentHistory?: MarketBoardHistoryEntry[];
    worldID: number;

    averagePrice?: number;
    averagePriceNQ?: number;
    averagePriceHQ?: number;

    saleVelocity?: number;
    saleVelocityNQ?: number;
    saleVelocityHQ?: number;
    saleVelocityUnits?: "per day";

    stackSizeHistogram?: { [key: number]: number };
}
