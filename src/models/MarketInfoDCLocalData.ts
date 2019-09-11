import { MarketBoardDCHistoryEntry } from "./MarketBoardDCHistoryEntry";
import { MarketBoardDCItemListing } from "./MarketBoardDCItemListing";

export interface MarketInfoDCLocalData {
    dcName: string;
    itemID: number;
    lastUploadTime: number
    listings?: MarketBoardDCItemListing[];
    recentHistory?: MarketBoardDCHistoryEntry[];
}
