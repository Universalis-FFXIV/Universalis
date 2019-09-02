import { MarketBoardDCHistoryEntry } from "./MarketBoardDCHistoryEntry";
import { MarketBoardDCItemListing } from "./MarketBoardDCItemListing";

export interface MarketInfoDCLocalData {
    dcName: string;
    itemID: number;
    listings?: MarketBoardDCItemListing[];
    recentHistory?: MarketBoardDCHistoryEntry[];
}
