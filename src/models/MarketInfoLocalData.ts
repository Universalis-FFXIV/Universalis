import { MarketBoardHistoryEntry } from "./MarketBoardHistoryEntry";
import { MarketBoardItemListing } from "./MarketBoardItemListing";

export interface MarketInfoLocalData {
    listings?: MarketBoardItemListing[];
    recentHistory?: MarketBoardHistoryEntry[];
}
