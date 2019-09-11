import { MarketBoardHistoryEntry } from "./MarketBoardHistoryEntry";
import { MarketBoardItemListing } from "./MarketBoardItemListing";

export interface MarketInfoLocalData {
    worldID: number;
    itemID: number;
    lastUploadTime: number;
    listings?: MarketBoardItemListing[];
    recentHistory?: MarketBoardHistoryEntry[];
    uploaderID: string;
}
